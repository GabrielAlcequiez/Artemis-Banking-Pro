using ABP.Application.Common;
using ABP.Application.Features.Commerce.DTOs;
using MediatR;

namespace ABP.Application.Features.Commerce.Commands.UpdateCommerce;

public sealed record UpdateCommerceCommand(
    UpdateCommerceRequest Request) : IRequest<OperationResult>;
