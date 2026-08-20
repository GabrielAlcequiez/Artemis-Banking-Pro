using System.Transactions;
using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Notifications;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanOriginationService(
    ILoanRepository loanRepository,
    IUserRepository userRepository,
    ISavingsAccountRepository savingsAccountRepository,
    IFinancialIdentifierGenerator identifierGenerator,
    IAccountBalanceService accountBalanceService,
    IAccountLedger accountLedger,
    ILoanRiskService loanRiskService,
    IAmortizationCalculator amortizationCalculator,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IClock clock,
    IValidator<CreateLoanRequest> requestValidator,
    IMapper mapper,
    IEmailService emailService,
    ILogger<LoanOriginationService> logger)
    : ILoanOriginationService
{
    public async Task<OperationResult<HighRiskAssessmentDto>> AssessRiskAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await requestValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var eligibilityError = await ValidateClientEligibilityAsync(
            request.ClientId,
            cancellationToken);

        if (eligibilityError != Error.None)
        {
            return OperationResult<HighRiskAssessmentDto>.Failure(
                eligibilityError);
        }

        var assessment = await loanRiskService.AssessAsync(
            request,
            cancellationToken);

        return OperationResult<HighRiskAssessmentDto>.Success(assessment);
    }

    public async Task<LoanOperationResult<LoanDetailDto>> CreateAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await requestValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var assignedByUserId = GetAdministratorUserId();

        if (assignedByUserId is null)
        {
            logger.LogWarning(
                "Intento de originación de préstamo sin un administrador autenticado.");

            return WithoutNotification(
                OperationResult<LoanDetailDto>.Failure(
                    LoanErrors.AdministratorRequired));
        }

        var client = await userRepository.GetByIdAsync(
            request.ClientId,
            cancellationToken);

        if (client is null || client.Role != Roles.Client)
        {
            return WithoutNotification(
                OperationResult<LoanDetailDto>.Failure(
                    LoanErrors.ClientNotFound));
        }

        if (!client.IsActive)
        {
            return WithoutNotification(
                OperationResult<LoanDetailDto>.Failure(
                    LoanErrors.ClientInactive));
        }

        if (await loanRepository.HasActiveLoanAsync(
                request.ClientId,
                cancellationToken))
        {
            return WithoutNotification(
                OperationResult<LoanDetailDto>.Failure(
                    LoanErrors.ActiveLoanExists));
        }

        var riskAssessment = await loanRiskService.AssessAsync(
            request,
            cancellationToken);

        if (riskAssessment.RequiresConfirmation)
        {
            logger.LogWarning(
                "La originación para el cliente {ClientId} requiere confirmación por riesgo {RiskType}.",
                request.ClientId,
                riskAssessment.RiskType);

            return WithoutNotification(
                OperationResult<LoanDetailDto>.Failure(
                    LoanErrors.HighRiskConfirmationRequired));
        }

        var principalAccount = await savingsAccountRepository
            .GetPrincipalAccountAsync(
                request.ClientId,
                cancellationToken);

        if (principalAccount is null ||
            principalAccount.Status != SavingsAccountStatus.Active)
        {
            return WithoutNotification(
                OperationResult<LoanDetailDto>.Failure(
                    LoanErrors.PrincipalAccountNotFound));
        }

        var amortization = amortizationCalculator.Calculate(
            request.CapitalAmount,
            request.AnnualInterestRate,
            request.TermInMonths,
            clock.Today);
        Loan loan;

        using (var transaction = new TransactionScope(
                   TransactionScopeOption.Required,
                   TransactionScopeAsyncFlowOption.Enabled))
        {
            string loanNumber;

            try
            {
                loanNumber = await identifierGenerator
                    .GenerateNineDigitIdentifierAsync(
                        FinancialIdentifierType.Loan,
                        cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                logger.LogError(
                    exception,
                    "No fue posible generar un número único para el préstamo del cliente {ClientId}.",
                    request.ClientId);

                return WithoutNotification(
                    OperationResult<LoanDetailDto>.Failure(
                        LoanErrors.NumberGenerationFailed));
            }

            loan = CreateLoan(
                request,
                assignedByUserId,
                loanNumber,
                amortization);

            await loanRepository.AddAsync(loan, cancellationToken);

            var disbursement = await accountBalanceService.CreditAsync(
                principalAccount.Id,
                request.CapitalAmount,
                cancellationToken);

            if (disbursement.IsFailure)
            {
                logger.LogWarning(
                    "Falló el desembolso del préstamo para el cliente {ClientId}: {ErrorCode}.",
                    request.ClientId,
                    disbursement.Error.Code);

                return WithoutNotification(
                    OperationResult<LoanDetailDto>.Failure(
                        disbursement.Error));
            }

            await accountLedger.RecordApprovedAsync(
                Guid.NewGuid(),
                principalAccount.Id,
                request.CapitalAmount,
                TransactionDirection.Credit,
                FinancialOperationType.LoanDisbursement,
                loanNumber,
                principalAccount.AccountNumber,
                assignedByUserId,
                Roles.Administrator.ToString(),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            transaction.Complete();
        }

        loan.Client = client;

        logger.LogInformation(
            "Préstamo {LoanId} originado para el cliente {ClientId} por {AdministratorId}.",
            loan.Id,
            request.ClientId,
            assignedByUserId);

        var detail = mapper.Map<LoanDetailDto>(loan);
        var recipient = ToRecipient(client);
        var notificationSent = await LoanNotificationEmails.SendBestEffortAsync(
            emailService,
            logger,
            LoanNotificationEmails.LoanApproved(
                recipient,
                loan.LoanNumber,
                loan.Capital,
                loan.TermInMonths,
                loan.AnnualInterestRate,
                detail.MonthlyInstallment),
            "aprobación de préstamo",
            loan.Id.ToString("N"));

        return new LoanOperationResult<LoanDetailDto>(
            OperationResult<LoanDetailDto>.Success(detail),
            !notificationSent);
    }

    private async Task<Error> ValidateClientEligibilityAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        var client = await userRepository.GetByIdAsync(
            clientId,
            cancellationToken);

        if (client is null || client.Role != Roles.Client)
        {
            return LoanErrors.ClientNotFound;
        }

        if (!client.IsActive)
        {
            return LoanErrors.ClientInactive;
        }

        return await loanRepository.HasActiveLoanAsync(
            clientId,
            cancellationToken)
            ? LoanErrors.ActiveLoanExists
            : Error.None;
    }

    private string? GetAdministratorUserId() =>
        currentUser.IsAuthenticated &&
        currentUser.IsInRole(Roles.Administrator.ToString()) &&
        !string.IsNullOrWhiteSpace(currentUser.UserId)
            ? currentUser.UserId
            : null;

    private static Loan CreateLoan(
        CreateLoanRequest request,
        string assignedByUserId,
        string loanNumber,
        AmortizationResult amortization)
    {
        var loan = new Loan
        {
            ClientId = request.ClientId,
            LoanNumber = loanNumber,
            Capital = request.CapitalAmount,
            PendingAmount = amortization.TotalAmountToPay,
            AnnualInterestRate = request.AnnualInterestRate,
            TermInMonths = request.TermInMonths,
            Status = LoanStatus.Active,
            AssignedByUserId = assignedByUserId
        };

        loan.Installments = amortization.Installments
            .Select(installment => new LoanInstallment
            {
                Loan = loan,
                Number = installment.InstallmentNumber,
                DueDate = installment.DueDate,
                InstallmentAmount = installment.InstallmentAmount,
                InterestAmount = installment.InterestAmount,
                CapitalAmount = installment.CapitalAmount,
                PendingAmount = installment.PendingInstallmentAmount,
                PaymentStatus = InstallmentPaymentStatus.Pending,
                IsLate = false
            })
            .ToArray();

        return loan;
    }

    private static LoanNotificationRecipient ToRecipient(
        Domain.Entities.User client) =>
        new(
            client.Id,
            client.Email ?? string.Empty,
            $"{client.Name} {client.LastName}".Trim());

    private static LoanOperationResult<TValue> WithoutNotification<TValue>(
        OperationResult<TValue> result) =>
        new(result, false);
}
