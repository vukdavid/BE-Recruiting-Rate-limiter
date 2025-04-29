using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace RateLimiter.Helpers
{
    internal static class IpAddressHelper
    {
        public static string GetClientIpAddress(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            string ip = context.Connection?.RemoteIpAddress?.ToString();

            if (!string.IsNullOrWhiteSpace(ip) && !IsPrivateIpAddress(ip))
            {
                return ip;
            }

            // try X-Forwarded-For header (common for proxies)
            if (TryExtractFromForwardedHeader(context, out var extractedAddress))
            {
                return extractedAddress;
            }

            return ip ?? "unknown";
        }

        private static bool IsPrivateIpAddress(string ipAddress)
        {
            ipAddress = ipAddress.Trim();

            if (ipAddress == "::1" || ipAddress == "127.0.0.1")
                return true;

            if (IPAddress.TryParse(ipAddress, out var address))
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    byte[] bytes = address.GetAddressBytes();
                    return
                        // 10.0.0.0/8
                        bytes[0] == 10 ||
                        // 172.16.0.0/12
                        (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                        // 192.168.0.0/16
                        (bytes[0] == 192 && bytes[1] == 168);
                }

                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
                }
            }

            return false;
        }

        private static bool TryExtractFromForwardedHeader(HttpContext context, out string extractedAddress)
        {
            extractedAddress = null;

            string forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(forwarded))
            {
                return false;
            }

            // The X-Forwarded-For header can contain multiple IP addresses
            // The first non-private IP address is typically the client's real address
            string[] addresses = forwarded.Split(',');
            foreach (var address in addresses)
            {
                if (!string.IsNullOrWhiteSpace(address) && !IsPrivateIpAddress(address))
                {
                    extractedAddress = address.Trim();
                    return true;
                }
            }

            return false;
        }
    }
}
