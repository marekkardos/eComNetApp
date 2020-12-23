using System;

namespace Core.Ex
{
    public class CreateOrderException : Exception
    {
        public CreateOrderException(string errorMessage) : base(errorMessage)
        {
        }

        public CreateOrderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}