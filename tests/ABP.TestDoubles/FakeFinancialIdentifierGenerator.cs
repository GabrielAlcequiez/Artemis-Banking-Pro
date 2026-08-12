using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;

namespace ABP.TestDoubles
{
    public class FakeFinancialIdentifierGenerator : IFinancialIdentifierGenerator
    {
        public string? NextValue { get; set; }

        private int _sequence = 100_000_000;

        public Task<string> GenerateNineDigitIdentifierAsync(
            FinancialIdentifierType type, CancellationToken cancellationToken = default)
        {
            if (NextValue is not null)
            {
                return Task.FromResult(NextValue);
            }

            _sequence++;
            return Task.FromResult(_sequence.ToString());
        }
    }
}
