using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RateLimiter.Algorithms
{
    /// <summary>
    /// Interface for rate limiting algorithms.
    /// </summary>
    public interface IRateLimitAlgorithm
    {
        /// <summary>
        /// Determines whether a request should be limited based on the rate limiting rules.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="ipAddress">The IP address of the client making the request.</param>
        /// <param name="path">The request path (e.g., "__default__" or a specific endpoint path).</param>
        /// <param name="requestLimitMs">Time window in milliseconds for rate limiting.</param>
        /// <param name="requestLimitCount">Maximum number of requests allowed in the time window.</param>
        /// <returns>True if the request should be limited, false otherwise.</returns>
        bool ShouldLimitRequest(
            HttpContext context,
            string ipAddress,
            string path,
            int requestLimitMs,
            int requestLimitCount);
    }
}
