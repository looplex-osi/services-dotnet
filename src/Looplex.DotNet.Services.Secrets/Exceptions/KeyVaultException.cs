namespace Looplex.DotNet.Services.Secrets.Exceptions
{
    internal class KeyVaultException : Exception
    {
        public KeyVaultException() : base() 
        {
            
        }
        public KeyVaultException(string message) : base(message)
        {

        }

        public KeyVaultException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
