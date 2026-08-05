using ABP.Domain.Enums;

namespace ABP.Application.Features.Accounts.Services.Interfaces;


public interface IFinancialIdentifierGenerator
{
    Task<string> GenerateNineDigitIdentifierAsync( FinancialIdentifierType type, CancellationToken cancellationToken = default);
}
