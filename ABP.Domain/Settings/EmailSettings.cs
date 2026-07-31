using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABP.Domain.Settings
{
   public class EmailSettings
    {
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;

        public bool UseSsl { get; set; } = true;
         public bool RequiresAuthentication { get; set; } = true;
    }
}