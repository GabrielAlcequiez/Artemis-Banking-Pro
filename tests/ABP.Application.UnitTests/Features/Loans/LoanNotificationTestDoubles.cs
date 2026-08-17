using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;

namespace ABP.Application.UnitTests.Features.Loans;

internal sealed class RecordingLoanEmailService : IEmailService
{
    public List<EmailRequestDto> SentEmails { get; } = [];

    public int SendAttempts { get; private set; }

    public bool ThrowOnSend { get; set; }

    public Func<bool>? IsOperationCommitted { get; set; }

    public bool WasCalledBeforeCommit { get; private set; }

    public Task SendAsync(EmailRequestDto emailRequestDto)
    {
        SendAttempts++;
        WasCalledBeforeCommit |= IsOperationCommitted is not null
            && !IsOperationCommitted();

        if (ThrowOnSend)
        {
            throw new InvalidOperationException("Fallo SMTP simulado.");
        }

        SentEmails.Add(emailRequestDto);
        return Task.CompletedTask;
    }
}
