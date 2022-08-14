using System;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Event arguments belonging to an event triggered to indicate progress in an upload to OneDrive
    /// </summary>
    public class OneDriveUploadProgressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Amount of bytes already sent
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// Amount of total bytes to transmit
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Progress of the upload in a percentage to indicate how much of the upload is done already
        /// </summary>
        public int ProgressPercentage => (int) decimal.Multiply(decimal.Divide(BytesSent, TotalBytes), 100);

        /// <summary>
        /// Initiates a new instance of upload progress
        /// </summary>
        /// <param name="bytesSent">Amount of bytes already sent</param>
        /// <param name="totalBytes">Amount of total bytes to transmit</param>
        public OneDriveUploadProgressChangedEventArgs(long bytesSent, long totalBytes)
        {
            BytesSent = bytesSent;
            TotalBytes = totalBytes;
        }
    }
}
