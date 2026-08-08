using ABP.Application.Common.DTOs;

namespace ABP.Application.Common.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(EmailRequestDto emailRequestDto);    
}
