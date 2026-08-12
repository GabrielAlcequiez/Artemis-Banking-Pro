using ABP.Application.Common;
using ABP.Application.Features.Commerce.DTOs;
using MediatR;

namespace ABP.Application.Features.Commerce.Commands.ChangeCommerceStatus;

public sealed record ChangeCommerceStatusCommand(
    ChangeCommerceStatusRequest Request) : IRequest<OperationResult>;
