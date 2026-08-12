using ABP.Application.Common;
using ABP.Application.Features.Commerce.DTOs;
using MediatR;

namespace ABP.Application.Features.Commerce.Commands.CreateCommerce;

public sealed record CreateCommerceCommand(
    CreateCommerceRequest Request) : IRequest<OperationResult<Guid>>;
