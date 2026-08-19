using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers
{
    [AllowAnonymous]
    public sealed class AccountController(IAccountServiceForWebApi accountService) : BaseApiController
    {
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthenticationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            var response = await accountService.LoginAsync(dto);
            return Ok(response);
        }

        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Confirm([FromBody] ConfirmAccountRequestDto request, CancellationToken cancellationToken)
        {
            await accountService.ConfirmAccountAsync(request);
            return NoContent();
        }

        [HttpPost("get-reset-token")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetResetToken([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken)
        {
            await accountService.GetResetTokenAsync(dto);
            return NoContent();
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken)
        {
            await accountService.ResetPasswordAsync(dto);
            return NoContent();
        }
    }
}