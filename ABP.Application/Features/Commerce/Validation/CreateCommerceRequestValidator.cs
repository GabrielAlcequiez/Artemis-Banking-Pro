using ABP.Application.Features.Commerce.DTOs;

namespace ABP.Application.Features.Commerce.Validation;

public sealed class CreateCommerceRequestValidator : CommerceDataValidator<CreateCommerceRequest>
{
    public CreateCommerceRequestValidator()
    {
        AddCommerceDataRules(
            request => request.Name,
            request => request.Description,
            request => request.Email,
            request => request.PhoneNumber,
            request => request.Rnc);
    }
}
