using System.Globalization;

namespace ABP.Application.Exceptions
{
    public class ApiException : Exception
    {
        public int StatusCode { get; set; }

        public string? ErrorCode { get; set; }

        public ApiException() : base() { }

        public ApiException(string message) : base(message) { }

        public ApiException(string message, int statuCode) : base(message) {
            StatusCode = statuCode;
        }

        public ApiException(string message, int statuCode, string errorCode) : base(message) {
            StatusCode = statuCode;
            ErrorCode = errorCode;
        }
        public ApiException(string message, params object[] args) 
            : base(String.Format(CultureInfo.CurrentCulture,message,args))
        {            
        }
    }
}