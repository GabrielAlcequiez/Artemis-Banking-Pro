using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities
{
    public class FinancialIdentifier : BaseEntity<Guid>
    {
        public FinancialIdentifier()
        {
        }

        public FinancialIdentifier(Guid id) : base(id)
        {
        }

        public string Value { get; set; } = string.Empty;
        public FinancialIdentifierType Type { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
