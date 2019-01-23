using System;
namespace lapi.domain.Exceptions
{
    public class SSLRequiredException: Exception
    {
        public SSLRequiredException() : base() { }
        public SSLRequiredException(string message): base(message) { }

    }
}
