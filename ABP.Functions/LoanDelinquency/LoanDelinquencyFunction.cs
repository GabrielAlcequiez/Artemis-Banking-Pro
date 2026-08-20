using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Loans.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.LoanDelinquency;

public sealed class LoanDelinquencyFunction(
    ILoanDelinquencyService delinquencyService,
    IClock clock,
    ILogger<LoanDelinquencyFunction> logger)
{
    [Function(nameof(LoanDelinquencyFunction))]
    public async Task RunAsync(
        [TimerTrigger("%LoanDelinquencySchedule%")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = clock.UtcNow;
        var bankingDate = clock.Today;

        try
        {
            if (timer.IsPastDue)
            {
                logger.LogWarning(
                    "La ejecución programada de mora inició con retraso para la fecha bancaria {BankingDate}.",
                    bankingDate);
            }

            logger.LogInformation(
                "Iniciando el proceso diario de mora para la fecha bancaria {BankingDate}.",
                bankingDate);

            var updatedInstallments = await delinquencyService
                .UpdateDelinquencyAsync(
                    bankingDate,
                    cancellationToken);

            logger.LogMetric(
                "LoanDelinquencyUpdatedInstallments",
                updatedInstallments);
            logger.LogInformation(
                "Proceso diario de mora completado para {BankingDate}. Cuotas actualizadas: {UpdatedInstallments}.",
                bankingDate,
                updatedInstallments);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "El proceso diario de mora fue cancelado para la fecha bancaria {BankingDate}.",
                bankingDate);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Falló el proceso diario de mora para la fecha bancaria {BankingDate}.",
                bankingDate);
            throw;
        }
        finally
        {
            var elapsedMilliseconds =
                (clock.UtcNow - startedAtUtc).TotalMilliseconds;

            logger.LogInformation(
                "Finalizó la ejecución de mora para {BankingDate} en {ElapsedMilliseconds} ms.",
                bankingDate,
                elapsedMilliseconds);
        }
    }
}
