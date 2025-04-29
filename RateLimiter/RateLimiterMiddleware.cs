using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimiter.Algorithms;
using RateLimiter.Configuration;
using RateLimiter.Helpers;
using RateLimiter.Matching;

namespace RateLimiter
{
    /// <summary>
    /// Middleware for rate limiting HTTP requests based on IP address.
    /// </summary>
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimiterMiddleware> _logger;
        private readonly IRateLimitAlgorithm _algorithm;
        private readonly IEndpointMatcher _endpointMatcher;
        private readonly RateLimiterOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimiterMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="algorithm">The rate limiting algorithm.</param>
        /// <param name="endpointMatcher">The endpoint matcher.</param>
        /// <param name="options">The rate limiter options.</param>
        public RateLimiterMiddleware(
            RequestDelegate next,
            ILogger<RateLimiterMiddleware> logger,
            IRateLimitAlgorithm algorithm,
            IEndpointMatcher endpointMatcher,
            IOptions<RateLimiterOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            _endpointMatcher = endpointMatcher ?? throw new ArgumentNullException(nameof(endpointMatcher));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Process an HTTP request.
        /// </summary>
        /// <param name="context">The HTTP context for the request.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.RequestLimiterEnabled)
            {
                await _next(context);
                return;
            }

            string ipAddress = IpAddressHelper.GetClientIpAddress(context);
            string path = context.Request.Path.Value;
            string method = context.Request.Method;

            var endpointLimit = FindEndpointLimitOptions(path, method);

            bool shouldLimit = _algorithm.ShouldLimitRequest(
                    context,
                    ipAddress,
                    path,
                    endpointLimit?.RequestLimitMs ?? _options.DefaultRequestLimitMs,
                    endpointLimit?.RequestLimitCount ?? _options.DefaultRequestLimitCount);

            if (shouldLimit)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}, Path: {Path}", ipAddress, path);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            await _next(context);
        }

        private EndpointLimitOptions FindEndpointLimitOptions(string path, string method)
        {
            return _options.EndpointLimits.FirstOrDefault(limitOptions => _endpointMatcher.IsMatch(path, limitOptions.Endpoint));
        }
    }
}
