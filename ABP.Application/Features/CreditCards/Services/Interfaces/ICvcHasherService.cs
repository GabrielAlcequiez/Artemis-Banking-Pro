namespace ABP.Application.Features.CreditCards.Services.Interfaces
{
    public interface ICvcHasherService
    {
        string Hash(string cvc);

        bool Verify(string cvc, string cvcHash);
    }
}