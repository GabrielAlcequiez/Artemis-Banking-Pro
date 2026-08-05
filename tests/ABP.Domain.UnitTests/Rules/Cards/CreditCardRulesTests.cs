using ABP.Domain.Enums;
using ABP.Domain.Rules.Cards;

namespace ABP.Domain.UnitTests.Rules.Cards
{
    public sealed class CreditCardRulesTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IsCreditLimitValid_WhenCreditLimitIsNotPositive_ReturnsFalse(
            int invalidCreditLimit)
        {
            var result = CreditCardRules.IsCreditLimitValid(invalidCreditLimit);

            Assert.False(result);
        }

        [Fact]
        public void IsCreditLimitValid_WhenCreditLimitIsPositive_ReturnsTrue()
        {
            var result = CreditCardRules.IsCreditLimitValid(0.01m);

            Assert.True(result);
        }

        [Fact]
        public void CalculateExpirationDate_WhenDateIsInAugust_ReturnsLastDayAfterThreeYears()
        {
            var result = CreditCardRules.CalculateExpirationDate(
                new DateOnly(2026, 8, 3));

            Assert.Equal(
                new DateOnly(2029, 8, 31),
                result);
        }

        [Fact]
        public void CalculateExpirationDate_WhenDateIsLeapDay_ReturnsLastDayOfFebruary()
        {
            var result = CreditCardRules.CalculateExpirationDate(
                new DateOnly(2024, 2, 29));

            Assert.Equal(
                new DateOnly(2027, 2, 28),
                result);
        }

        [Fact]
        public void IsExpired_WhenBankingDateEqualsExpirationDate_ReturnsFalse()
        {
            var expirationDate = new DateOnly(2029, 8, 31);

            var result = CreditCardRules.IsExpired(
                expirationDate,
                expirationDate);

            Assert.False(result);
        }

        [Fact]
        public void IsExpired_WhenBankingDateIsNextDay_ReturnsTrue()
        {
            var expirationDate = new DateOnly(2029, 8, 31);

            var result = CreditCardRules.IsExpired(
                expirationDate,
                expirationDate.AddDays(1));

            Assert.True(result);
        }

        [Fact]
        public void CanChangeLimit_WhenNewLimitEqualsDebt_ReturnsTrue()
        {
            var result = CreditCardRules.CanChangeLimit(
                CreditCardStatus.Active,
                500m,
                500m);

            Assert.True(result);
        }

        [Fact]
        public void CanChangeLimit_WhenNewLimitIsBelowDebt_ReturnsFalse()
        {
            var result = CreditCardRules.CanChangeLimit(
                CreditCardStatus.Active,
                500m,
                499.99m);

            Assert.False(result);
        }

        [Fact]
        public void CanChangeLimit_WhenNewLimitIsNotPositive_ReturnsFalse()
        {
            var result = CreditCardRules.CanChangeLimit(
                CreditCardStatus.Active,
                500m,
                0m);

            Assert.False(result);
        }

        [Fact]
        public void CanChangeLimit_WhenCardIsCancelled_ReturnsFalse()
        {
            var result = CreditCardRules.CanChangeLimit(
                CreditCardStatus.Cancelled,
                0m,
                1000m);

            Assert.False(result);
        }

        [Fact]
        public void CanCancel_WhenCardIsActiveAndDebtIsZero_ReturnsTrue()
        {
            var result = CreditCardRules.CanCancel(
                CreditCardStatus.Active,
                0m);

            Assert.True(result);
        }

        [Fact]
        public void CanCancel_WhenCardHasDebt_ReturnsFalse()
        {
            var result = CreditCardRules.CanCancel(
                CreditCardStatus.Active,
                0.01m);

            Assert.False(result);
        }

        [Fact]
        public void CanCancel_WhenCardIsAlreadyCancelled_ReturnsFalse()
        {
            var result = CreditCardRules.CanCancel(
                CreditCardStatus.Cancelled,
                0m);

            Assert.False(result);
        }
    }
}
