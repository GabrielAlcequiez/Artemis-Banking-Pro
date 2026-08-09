using System.Reflection;
using ABP.Application.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.HermesPay.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.Commerce.Services.Interfaces;
using ABP.Application.Features.HermesPay.Services.Interfaces;
using ABP.TestDoubles;
using Xunit;

namespace ABP.Application.UnitTests.Contracts
{
    public class P4ContractTests
    {
        [Fact]
        public async Task FakeCardDebtReaderService_ShouldReturnConfiguredAndDefaultDebt()
        {
            // Arrange
            var fakeDebtReader = new FakeCardDebtReaderService { DefaultDebt = 100m };
            fakeDebtReader.SetDebtForClient("CLIENT-123", 500.50m);

            // Act
            var configuredDebt = await fakeDebtReader.GetActiveCardDebtByClientIdAsync("CLIENT-123");
            var defaultDebt = await fakeDebtReader.GetActiveCardDebtByClientIdAsync("UNKNOWN-CLIENT");

            // Assert
            Assert.Equal(500.50m, configuredDebt);
            Assert.Equal(100m, defaultDebt);
            Assert.IsAssignableFrom<ICardDebtReaderService>(fakeDebtReader);
        }

        [Fact]
        public async Task FakeCommerceAuthorizationResolverService_ShouldReturnRequestedCommerceIdByDefault()
        {
            // Arrange
            var fakeResolver = new FakeCommerceAuthorizationResolverService();
            var requestedCommerceId = Guid.NewGuid();

            // Act
            var result = await fakeResolver.ResolveAuthorizedCommerceIdAsync(requestedCommerceId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(requestedCommerceId, result.Value);
            Assert.IsAssignableFrom<ICommerceAuthorizationResolverService>(fakeResolver);
        }

        [Fact]
        public async Task FakeCommerceAuthorizationResolverService_ShouldReturnConfiguredResultWhenSet()
        {
            // Arrange
            var fakeResolver = new FakeCommerceAuthorizationResolverService();
            var requestedCommerceId = Guid.NewGuid();
            var overriddenCommerceId = Guid.NewGuid();
            fakeResolver.SetResultForCommerce(requestedCommerceId, OperationResult<Guid>.Success(overriddenCommerceId));

            // Act
            var result = await fakeResolver.ResolveAuthorizedCommerceIdAsync(requestedCommerceId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(overriddenCommerceId, result.Value);
        }

        [Fact]
        public void FinancialRequests_ShouldContainOperationId()
        {
            // Arrange & Act
            var operationId = Guid.NewGuid();

            var cardPayment = new CreditCardPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 150m, operationId);
            var cashAdvance = new CashAdvanceRequest(Guid.NewGuid(), Guid.NewGuid(), 200m, operationId);
            var hermesPayment = new ProcessHermesPaymentRequest(Guid.NewGuid(), "4000123456789010", 12, 2030, "123", 99.99m, operationId);

            // Assert
            Assert.Equal(operationId, cardPayment.OperationId);
            Assert.Equal(operationId, cashAdvance.OperationId);
            Assert.Equal(operationId, hermesPayment.OperationId);
        }

        [Fact]
        public void ProcessHermesPaymentRequest_ToString_MustRedactCardNumberAndCvc()
        {
            // Arrange
            var rawPan = "4532012345678901";
            var rawCvc = "999";
            var hermesRequest = new ProcessHermesPaymentRequest(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                rawPan,
                11,
                2028,
                rawCvc,
                250m,
                Guid.Parse("22222222-2222-2222-2222-222222222222"));

            // Act
            var str = hermesRequest.ToString();

            // Assert
            Assert.DoesNotContain(rawPan, str);
            Assert.DoesNotContain(rawCvc, str);
            Assert.Contains("****8901", str);
        }

        [Fact]
        public void ServiceInterfaces_MustHaveExactMethodSignatures()
        {
            // ICardPaymentService
            var paymentMethod = typeof(ICardPaymentService).GetMethod("ProcessPaymentAsync");
            Assert.NotNull(paymentMethod);
            Assert.Equal(typeof(Task<OperationResult<FinancialOperationReceipt>>), paymentMethod.ReturnType);
            var paymentParams = paymentMethod.GetParameters();
            Assert.Equal(2, paymentParams.Length);
            Assert.Equal(typeof(CreditCardPaymentRequest), paymentParams[0].ParameterType);
            Assert.Equal(typeof(CancellationToken), paymentParams[1].ParameterType);

            // ICashAdvanceService
            var advanceMethod = typeof(ICashAdvanceService).GetMethod("ProcessCashAdvanceAsync");
            Assert.NotNull(advanceMethod);
            Assert.Equal(typeof(Task<OperationResult<FinancialOperationReceipt>>), advanceMethod.ReturnType);
            var advanceParams = advanceMethod.GetParameters();
            Assert.Equal(2, advanceParams.Length);
            Assert.Equal(typeof(CashAdvanceRequest), advanceParams[0].ParameterType);
            Assert.Equal(typeof(CancellationToken), advanceParams[1].ParameterType);

            // IHermesPaymentService
            var hermesMethod = typeof(IHermesPaymentService).GetMethod("ProcessHermesPaymentAsync");
            Assert.NotNull(hermesMethod);
            Assert.Equal(typeof(Task<OperationResult<FinancialOperationReceipt>>), hermesMethod.ReturnType);
            var hermesParams = hermesMethod.GetParameters();
            Assert.Equal(2, hermesParams.Length);
            Assert.Equal(typeof(ProcessHermesPaymentRequest), hermesParams[0].ParameterType);
            Assert.Equal(typeof(CancellationToken), hermesParams[1].ParameterType);
        }

        [Fact]
        public void Requests_MustHaveExactFrozenPropertiesAndTypes()
        {
            // CreateCreditCardRequest (ClientId: string, CreditLimit: decimal)
            AssertProperty<CreateCreditCardRequest>("ClientId", typeof(string));
            AssertProperty<CreateCreditCardRequest>("CreditLimit", typeof(decimal));

            // UpdateCreditLimitRequest (CreditCardId: Guid, CreditLimit: decimal)
            AssertProperty<UpdateCreditLimitRequest>("CreditCardId", typeof(Guid));
            AssertProperty<UpdateCreditLimitRequest>("CreditLimit", typeof(decimal));

            // CancelCreditCardRequest (CreditCardId: Guid)
            AssertProperty<CancelCreditCardRequest>("CreditCardId", typeof(Guid));

            // CreditCardPaymentRequest (CreditCardId: Guid, SourceAccountId: Guid, Amount: decimal, OperationId: Guid)
            AssertProperty<CreditCardPaymentRequest>("CreditCardId", typeof(Guid));
            AssertProperty<CreditCardPaymentRequest>("SourceAccountId", typeof(Guid));
            AssertProperty<CreditCardPaymentRequest>("Amount", typeof(decimal));
            AssertProperty<CreditCardPaymentRequest>("OperationId", typeof(Guid));

            // CashAdvanceRequest (CreditCardId: Guid, TargetAccountId: Guid, Amount: decimal, OperationId: Guid)
            AssertProperty<CashAdvanceRequest>("CreditCardId", typeof(Guid));
            AssertProperty<CashAdvanceRequest>("TargetAccountId", typeof(Guid));
            AssertProperty<CashAdvanceRequest>("Amount", typeof(decimal));
            AssertProperty<CashAdvanceRequest>("OperationId", typeof(Guid));

            // CreateCommerceRequest (Name: string, Description: string?, Email: string, PhoneNumber: string, Rnc: string)
            AssertProperty<CreateCommerceRequest>("Name", typeof(string));
            AssertProperty<CreateCommerceRequest>("Description", typeof(string));
            AssertProperty<CreateCommerceRequest>("Email", typeof(string));
            AssertProperty<CreateCommerceRequest>("PhoneNumber", typeof(string));
            AssertProperty<CreateCommerceRequest>("Rnc", typeof(string));

            // UpdateCommerceRequest (CommerceId: Guid, Name: string, Description: string?, Email: string, PhoneNumber: string, Rnc: string)
            AssertProperty<UpdateCommerceRequest>("CommerceId", typeof(Guid));
            AssertProperty<UpdateCommerceRequest>("Name", typeof(string));
            AssertProperty<UpdateCommerceRequest>("Description", typeof(string));
            AssertProperty<UpdateCommerceRequest>("Email", typeof(string));
            AssertProperty<UpdateCommerceRequest>("PhoneNumber", typeof(string));
            AssertProperty<UpdateCommerceRequest>("Rnc", typeof(string));

            // ChangeCommerceStatusRequest (CommerceId: Guid, IsActive: bool)
            AssertProperty<ChangeCommerceStatusRequest>("CommerceId", typeof(Guid));
            AssertProperty<ChangeCommerceStatusRequest>("IsActive", typeof(bool));

            // ProcessHermesPaymentRequest
            AssertProperty<ProcessHermesPaymentRequest>("RequestedCommerceId", typeof(Guid));
            AssertProperty<ProcessHermesPaymentRequest>("CardNumber", typeof(string));
            AssertProperty<ProcessHermesPaymentRequest>("ExpirationMonth", typeof(int));
            AssertProperty<ProcessHermesPaymentRequest>("ExpirationYear", typeof(int));
            AssertProperty<ProcessHermesPaymentRequest>("Cvc", typeof(string));
            AssertProperty<ProcessHermesPaymentRequest>("TransactionAmount", typeof(decimal));
            AssertProperty<ProcessHermesPaymentRequest>("OperationId", typeof(Guid));

            // GetUserDto (CommerceId: Guid?)
            AssertProperty<GetUserDto>("CommerceId", typeof(Guid?));

            // CreateUserDto (CommerceId: Guid?)
            AssertProperty<CreateUserDto>("CommerceId", typeof(Guid?));
        }

        private static void AssertProperty<T>(string propertyName, Type expectedType)
        {
            var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(prop);
            Assert.Equal(expectedType, prop.PropertyType);
            Assert.True(prop.CanRead);
        }
    }
}
