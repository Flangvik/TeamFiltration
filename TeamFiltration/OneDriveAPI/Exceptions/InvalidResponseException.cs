using System;

namespace KoenZomers.OneDrive.Api.Exceptions
{
    /// <summary>
    /// Exception thrown when failing to parse a response returned by OneDrive
    /// </summary>
    public class InvalidResponseException : Exception
    {
        /// <summary>
        /// Response received by the OneDrive service
        /// </summary>
        public string Response { get; private set; }

        /// <summary>
        /// Exception to indicate it wasn't possible to parse the response returned by OneDrive
        /// </summary>
        /// <param name="response">Response received by the OneDrive service</param>
        /// <param name="innerException">Inner exception that was thrown to indicate failure of the operation (optional)</param>
        public InvalidResponseException(string response, Exception innerException) : base("Invalid response returned by OneDrive: " + response, innerException)
        {
            Response = response;
        }
    }
}
