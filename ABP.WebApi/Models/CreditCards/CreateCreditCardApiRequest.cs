namespace ABP.WebApi.Models.CreditCards;

public sealed record CreateCreditCardApiRequest(
    string ClientId,
    decimal CreditLimit);
