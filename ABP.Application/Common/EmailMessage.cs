namespace ABP.Application.Common;

public sealed record EmailMessage(
    string RecipientEmail,
    string Subject,
    string HtmlBody);
