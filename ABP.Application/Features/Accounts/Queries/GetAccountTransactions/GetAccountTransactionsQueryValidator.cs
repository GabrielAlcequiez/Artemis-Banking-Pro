using FluentValidation;

namespace ABP.Application.Features.Accounts.Queries.GetAccountTransactions;

public sealed class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
{
    public GetAccountTransactionsQueryValidator()
    {
        RuleFor(query => query.AccountId)
            .NotEmpty();

        RuleFor(query => query.PagedRequest)
            .NotNull();

        RuleFor(query => query.PagedRequest.Page)
            .GreaterThanOrEqualTo(1)
            .When(query => query.PagedRequest is not null);

        RuleFor(query => query.PagedRequest.PageSize)
            .GreaterThan(0)
            .When(query => query.PagedRequest is not null);
    }
}
