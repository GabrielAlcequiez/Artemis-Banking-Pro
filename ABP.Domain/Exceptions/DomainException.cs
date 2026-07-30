using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABP.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        protected DomainException(string code, string message):base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
