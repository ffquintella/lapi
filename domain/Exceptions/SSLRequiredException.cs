using System;
namespace lapi.domain.Exceptions
{
    public class NullException: Exception
    {
        public NullException() : base() { }
        public NullException(string message): base(message) { }

    }
}
