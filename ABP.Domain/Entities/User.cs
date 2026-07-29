using ABP.Domain.Common;
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


        // Acá irán los navigations y relaciones, como prestamos, cuenta de ahorro y tarjeta de credito
        // PENDIENTE
    }
}
