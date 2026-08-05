using ABP.Domain.Enums;

namespace ABP.Application.Interfaces.Services;


public interface IFinancialIdentifierGenerator
{
    Task<string> GenerateNineDigitIdentifierAsync( FinancialIdentifierType type, CancellationToken cancellationToken = default);
}
