namespace RateLimiter.Configuration
{
    /// <summary>
    /// Rate limiting configuration for a specific endpoint.
    /// </summary>
    public class EndpointLimitOptions
    {
        /// <summary>
        /// The exact endpoint path to apply rate limiting to (e.g. "/api/products/books").
        /// Case-insensitive matching is used.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Time window in milliseconds for rate limiting for this specific endpoint.
        /// </summary>
        public int RequestLimitMs { get; set; }

        /// <summary>
        /// Maximum number of requests allowed within the specified time window for this endpoint.
        /// </summary>
        public int RequestLimitCount { get; set; }
    }
}
