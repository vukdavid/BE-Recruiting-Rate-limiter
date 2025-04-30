using System.Collections.Generic;

namespace RateLimiter.Configuration
{
    /// <summary>
    /// Configuration options for the rate limiter middleware.
    /// </summary>
    public class RateLimiterOptions
    {
        /// <summary>
        /// Enables or disables the rate limiter functionality.
        /// </summary>
        public bool RequestLimiterEnabled { get; set; }

        /// <summary>
        /// Default time window in milliseconds for rate limiting across all endpoints.
        /// </summary>
        public int DefaultRequestLimitMs { get; set; }

        /// <summary>
        /// Default maximum number of requests allowed within the specified time window across all endpoints.
        /// </summary>
        public int DefaultRequestLimitCount { get; set; }

        /// <summary>
        /// List of endpoint-specific rate limiting rules.
        /// </summary>
        public List<EndpointLimitOptions> EndpointLimits { get; set; } = [];
    }
}
