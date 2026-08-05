using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;

namespace ABP.Domain.Entities
{
    public class User : AuditableEntity<string>
    {
        public User()
        {
        }

        public User(string id) : base(id)
        {
        }

        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;


        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public string Identification { get; set; } = string.Empty;
        public Roles Role { get; set; }

        public bool IsActive { get; set; }

        public Guid? CommerceId { get; set; }

        // Propiedades de navegación (Relaciones)
        public ICollection<SavingsAccount> SavingsAccounts { get; set; } = new List<SavingsAccount>();
        public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
    }
}
