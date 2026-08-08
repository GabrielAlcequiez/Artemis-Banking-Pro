namespace ABP.Application.Features.CreditCards.DTOs;

public enum CreditCardSearchStatus
{
    NoSearch = 0,
    ClientNotFound = 1,
    ClientWithoutCards = 2,
    ResultsFound = 3,
    NoMatchingCards = 4
}
