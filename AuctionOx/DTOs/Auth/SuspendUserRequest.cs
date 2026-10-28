namespace AuctionOx.DTOs.Auth
{
    public class SuspendUserRequest
    {
        /// <summary>
        /// Duration in days for suspension. Default is 7 days.
        /// </summary>
        public int Days { get; set; } = 7;

        /// <summary>
        /// Optional reason for moderation logs or alerts.
        /// </summary>
        public string? Reason { get; set; }
    }
}
