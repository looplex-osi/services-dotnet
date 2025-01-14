namespace Looplex.DotNet.Services.Secrets.Exceptions
{
    public class SecretValidationException : Exception
    {
        public SecretValidationException() : base()
        {

        }
        public SecretValidationException(string message) : base(message)
        {

        }
        public SecretValidationException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
