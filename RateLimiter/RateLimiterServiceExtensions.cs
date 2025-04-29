using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RateLimiter.Algorithms;
using RateLimiter.Configuration;
using RateLimiter.Matching;
using RateLimiter.Storage;

namespace RateLimiter
{
    /// <summary>
    /// Extension methods for configuring rate limiter options.
    /// </summary>
    public static class RateLimiterServiceExtensions
    {
        /// <summary>
        /// Configures <see cref="RateLimiterOptions"/> from the specified <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configuration">The configuration under section "RateLimiter". See <see cref="RateLimiterOptions"/> for details.
        /// For endpoint-specific limits, you can set <see cref="EndpointLimitOptions.MatchHttpMethod"/> to true to apply rate limiting per HTTP method.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddIpRateLimiter(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.Configure<RateLimiterOptions>(configuration.GetSection("RateLimiter"));
            RegisterServices(services);

            return services;
        }

        /// <summary>
        /// Configures <see cref="RateLimiterOptions"/> using the provided action.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The action used to configure the options.
        /// For endpoint-specific limits, you can set <see cref="EndpointLimitOptions.MatchHttpMethod"/> to true to apply rate limiting per HTTP method.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddIpRateLimiter(
            this IServiceCollection services,
            Action<RateLimiterOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureOptions);

            services.Configure(configureOptions);
            RegisterServices(services);

            return services;
        }

        /// <summary>
        /// Adds the rate limiter middleware to the application's request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
        public static IApplicationBuilder UseIpRateLimiter(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.UseMiddleware<RateLimiterMiddleware>();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.TryAddSingleton<IRateLimitAlgorithm, FixedWindowAlgorithm>();
            services.TryAddSingleton<IRequestStore, InMemoryRequestStore>();
            services.TryAddSingleton<IEndpointMatcher, SimpleEndpointMatcher>();
        }
    }
}
