using FluentValidation;

namespace ABP.Application.Features.Accounts.Queries.GetSavingsAccounts;

public sealed class GetSavingsAccountsQueryValidator : AbstractValidator<GetSavingsAccountsQuery>
{
    public GetSavingsAccountsQueryValidator()
    {
        RuleFor(query => query.PagedRequest)
            .NotNull();

        RuleFor(query => query.PagedRequest.Page)
            .GreaterThanOrEqualTo(1)
            .When(query => query.PagedRequest is not null)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(query => query.PagedRequest.PageSize)
            .GreaterThan(0)
            .When(query => query.PagedRequest is not null)
            .WithMessage("PageSize must be greater than zero.");
    }
}
