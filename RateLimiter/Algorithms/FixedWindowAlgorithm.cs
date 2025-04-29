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
            var timestamp = DateTimeOffset.UtcNow;
            var windowStart = timestamp.ToUnixTimeMilliseconds() / requestLimitMs;
            
            string key = $"{ipAddress}:{path}:{windowStart}";
            
            long currentCount = _requestStore.IncrementRequestCount(key);

            // clean up old entries periodically (runs once per new window/bucket)
            if (currentCount == 1)
            {
                _ = Task.Run(() => _requestStore.Cleanup(requestLimitMs * 2)); // keep two windows behind
            }

            return currentCount > requestLimitCount;
        }
    }
}
