using System;

namespace RateLimiter.Matching
{
    /// <summary>
    /// Default implementation of the endpoint matcher using simple string comparison that is case-insensitive.
    /// </summary>
    public class SimpleEndpointMatcher : IEndpointMatcher
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
