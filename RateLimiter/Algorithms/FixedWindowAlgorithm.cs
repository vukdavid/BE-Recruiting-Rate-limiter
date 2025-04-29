using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RateLimiter.Storage;

namespace RateLimiter.Algorithms
{
    /// <summary>
    /// Implementation of a fixed window rate limiting algorithm.
    /// Counts requests within fixed time windows.
    /// </summary>
    public class FixedWindowAlgorithm : IRateLimitAlgorithm
    {
        private readonly IRequestStore _requestStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedWindowAlgorithm"/> class.
        /// </summary>
        /// <param name="requestStore">The request storage provider.</param>
        public FixedWindowAlgorithm(IRequestStore requestStore)
        {
            _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
        }

        /// <inheritdoc />
        public bool ShouldLimitRequest(
            HttpContext context,
            string ipAddress,
            string path,
            int requestLimitMs,
            int requestLimitCount)
        {
            // Current time window
            var timestamp = DateTimeOffset.UtcNow;
            var windowId = timestamp.ToUnixTimeMilliseconds() / requestLimitMs;
            
            // Generate a unique key for this client, endpoint and window
            string key = $"{ipAddress}:{path}:{windowId}";
            
            // Increment the counter for this request
            long currentCount = _requestStore.IncrementRequestCount(key);

            // Clean up old entries periodically
            if (currentCount == 1)
            {
                // Only do cleanup when creating a new entry to avoid doing it too often
                _ = Task.Run(() => _requestStore.Cleanup(requestLimitMs * 2));
            }

            // Determine if the request should be limited
            return currentCount > requestLimitCount;
        }
    }
}
