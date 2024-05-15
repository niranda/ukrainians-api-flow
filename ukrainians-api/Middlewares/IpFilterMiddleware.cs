using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using Ukrainians.UtilityServices.Models.IP;

namespace Ukrainians.WebAPI.Middlewares
{
    public class IpFilterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpFilterMiddleware> _logger;
        private readonly HashSet<string> _allowedCountries;
        private readonly HttpClient _httpClient;

        public IpFilterMiddleware(RequestDelegate next, ILogger<IpFilterMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;

            var allowedCountries = configuration.GetSection("AllowedCountries").Get<List<string>>();
            _allowedCountries = new HashSet<string>(allowedCountries ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            _httpClient = new HttpClient();
        }


        public async Task InvokeAsync(HttpContext context)
        {
            context = context ?? throw new ArgumentNullException(nameof(context));

            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(clientIp))
            {
                _logger.LogWarning("Client IP address is null or empty");
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Access denied");
                return;
            }

            var country = await GetCountryFromIpAsync(clientIp);
            //if (
            //    string.IsNullOrWhiteSpace(country) || 
            //    !_allowedCountries.Contains(country))
            //{
            //    _logger.LogWarning($"Blocked IP: {clientIp} from {country}");
            //    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            //    await context.Response.WriteAsync("Access denied");
            //    return;
            //}

            await _next(context);
        }

        private async Task<string?> GetCountryFromIpAsync(string ip)
        {
            var url = $"http://ipinfo.io/{ip}";
            using var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                IPInfo? ipInfo = JsonConvert.DeserializeObject<IPInfo>(json);
                if (ipInfo != null && !string.IsNullOrWhiteSpace(ipInfo.Country))
                {
                    return new RegionInfo(ipInfo.Country).EnglishName;
                }
            }

            return null;
        }
    }
}
