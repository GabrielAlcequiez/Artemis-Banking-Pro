using FluentValidation;

namespace ABP.Application.Features.HermesPay.Queries.GetHermesTransactions;

public sealed class GetHermesTransactionsQueryValidator
    : AbstractValidator<GetHermesTransactionsQuery>
{
    public GetHermesTransactionsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0)
            .WithMessage("La página debe ser mayor que cero.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 20)
            .WithMessage("La cantidad de registros por página debe estar entre 1 y 20.");
    }
}
