using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FiasApi.HealthChecks
{
	public class FiasHelthCheck : IHealthCheck
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;

		public FiasHelthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
			CancellationToken cancellationToken = new CancellationToken())
		{
			var healthSection = _configuration.GetSection("Health");
			var baseAddress = healthSection.GetValue<string>("BaseAddress");
			var fiasApiToken = healthSection.GetValue<string>("Token");

			var httpClient = _httpClientFactory.CreateClient();
			httpClient.BaseAddress = new Uri(baseAddress);
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fiasApiToken);
			httpClient.DefaultRequestHeaders.Accept.Clear();
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			httpClient.Timeout = TimeSpan.FromSeconds(5);

			try
			{
				var response = await httpClient.GetStringAsync("api/GetAddressByGeoCoder?latitude=59.90297&longitude=30.398280");

				if(response.Contains("Седова"))
				{
					return new HealthCheckResult(HealthStatus.Healthy);
				}

				return new HealthCheckResult(HealthStatus.Unhealthy);
			}
			catch(Exception e)
			{
				return new HealthCheckResult(HealthStatus.Unhealthy, "Исключение в процессе проверки здоровья", e);
			}
		}
	}
}
