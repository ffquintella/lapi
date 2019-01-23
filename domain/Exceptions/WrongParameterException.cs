using System;
namespace lapi.domain.Exceptions
{
    public class WrongParameterException: Exception
    {
        public WrongParameterException() : base() { }
        public WrongParameterException(string message): base(message) { }

    }
}
