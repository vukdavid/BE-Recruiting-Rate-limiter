using System;

namespace RateLimiter.Matching
{
    /// <summary>
    /// Default implementation of the endpoint matcher.
    /// </summary>
    public class DefaultEndpointMatcher : IEndpointMatcher
    {
        /// <inheritdoc />
        public bool IsMatch(string requestPath, string configuredPath)
        {
            if (string.IsNullOrEmpty(requestPath) || string.IsNullOrEmpty(configuredPath))
            {
                return false;
            }

            return string.Equals(requestPath, configuredPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
