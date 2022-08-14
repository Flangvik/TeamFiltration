using System;

namespace KoenZomers.OneDrive.Api.Exceptions
{
    /// <summary>
    /// Exception thrown when failing to retrieve a valid token to connect to OneDrive
    /// </summary>
    public class TokenRetrievalFailedException : Exception
    {
        /// <summary>
        /// Exception to indicate it wasn't possible to retrieve a valid token to connect to OneDrive
        /// </summary>
        /// <param name="message">Message providing details on why it wasn't possible to retrieve the token (optional)</param>
        /// <param name="errorDetails">Details returned by the OneDrive API about why the request failed (optional)</param>
        /// <param name="innerException">Inner exception that was thrown to indicate failure of the operation (optional)</param>
        public TokenRetrievalFailedException(string message = null, Entities.OneDriveError errorDetails = null, Exception innerException = null) : base("Failed to retrieve OneDrive access token." + (string.IsNullOrEmpty(message) ? string.Empty : " Additional information: " + message), innerException)
        {

        }
    }
}
