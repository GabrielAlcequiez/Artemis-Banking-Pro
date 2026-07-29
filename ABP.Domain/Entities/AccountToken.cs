using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities;

public class AccountToken : BaseEntity<Guid>
{
    public AccountToken()
    {
    }

    public AccountToken(Guid id)
        : base(id)
    {
    }

    public string UserId { get; set; } = string.Empty;

    public AccountTokenPurpose Purpose { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? UsedAtUtc { get; set; }
}
