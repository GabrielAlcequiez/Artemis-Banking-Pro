using ABP.Application.GeneralDto;

namespace ABP.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(EmailRequestDto emailRequestDto);    
}
