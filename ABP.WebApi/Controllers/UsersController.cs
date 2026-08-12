using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;
using ABP.WebApi.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers
{
    [Authorize(Roles = nameof(Roles.Administrator))]
    public sealed class UsersController(
        IAccountServiceForWebApi accountService,
        ICurrentUserService currentUser) : BaseApiController
    {
        private string CurrentUserId => currentUser.UserId ?? string.Empty;

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? role = null,
            CancellationToken cancellationToken = default)
        {
            var filter = new UserQueryFilterDto { Page = page, PageSize = pageSize, Role = role };
            var pageResult = await accountService.GetUsersPagedAsync(filter);
            return Ok(pageResult.ToApiPage());
        }

        [HttpGet("commerce")]
        public async Task<ActionResult> GetCommerce(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var filter = new UserQueryFilterDto { Page = page, PageSize = pageSize, IsCommerceOnly = true };
            var pageResult = await accountService.GetUsersPagedAsync(filter);
            return Ok(pageResult.ToApiPage());
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
        {
            var result = await accountService.RegisterUserAsync(dto, origin: null, isApi: true);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, UserApiPresenter.ToCreated(dto, result));
        }

        [HttpPost("commerce/{commerceId:guid}")]
        public async Task<ActionResult> CreateCommerce(Guid commerceId, [FromBody] CreateCommerceUserRequestDto dto, CancellationToken cancellationToken)
        {
            var result = await accountService.RegisterCommerceUserAsync(dto, commerceId, origin: null);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, UserApiPresenter.ToCreated(dto, result, commerceId));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] EditUserDto dto, CancellationToken cancellationToken)
        {
            dto.Id = id;
            await accountService.EditUserAsync(dto, CurrentUserId);
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeUserStatusRequestDto request, CancellationToken cancellationToken)
        {
            await accountService.ChangeUserStatusAsync(id, request, CurrentUserId);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var detail = await accountService.GetUserDetailAsync(id);
            return Ok(detail!.ToApiDetail());
        }
    }
}