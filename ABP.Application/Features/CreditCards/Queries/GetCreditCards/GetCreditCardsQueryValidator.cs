using ABP.Application.Features.CreditCards.DTOs;
using FluentValidation;

namespace ABP.Application.Features.CreditCards.Queries.GetCreditCards;

public sealed class GetCreditCardsQueryValidator : AbstractValidator<GetCreditCardsQuery>
{
    public GetCreditCardsQueryValidator(
        IValidator<CreditCardListRequest> requestValidator)
    {
        RuleFor(query => query.Request)
            .NotNull()
            .SetValidator(requestValidator);
    }
}
