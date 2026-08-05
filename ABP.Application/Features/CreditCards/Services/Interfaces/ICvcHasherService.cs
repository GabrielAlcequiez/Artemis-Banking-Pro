namespace ABP.Application.Interfaces.Services
{
    public interface ICvcHasherService
    {
        string Hash(string cvc);

        bool Verify(string cvc, string cvcHash);
    }
}