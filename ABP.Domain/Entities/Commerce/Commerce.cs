using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities.Commerce
{
    public sealed class Commerce : AuditableEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public CommerceStatus Status { get; set; } = CommerceStatus.Active;
        public byte[] RowVersion { get; set; } = [];
    }
}