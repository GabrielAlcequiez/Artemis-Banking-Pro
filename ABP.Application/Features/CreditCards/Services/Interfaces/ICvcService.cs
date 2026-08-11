namespace ABP.Application.Features.CreditCards.Services.Interfaces
{
    public interface ICvcService
    {
        string Generate();
        string Hash(string cvc);
        bool Verify(string cvc, string cvcHash);
    }
}