namespace RateLimiter.Configuration
{
    /// <summary>
    /// Rate limiting configuration for a specific endpoint.
    /// </summary>
    public class EndpointLimitOptions
    {
        /// <summary>
        /// The endpoint path to apply rate limiting to.
        /// Supports route templates (e.g. "/api/products/{id}").
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
