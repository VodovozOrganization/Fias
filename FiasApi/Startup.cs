using System.IO;
using System.Linq;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Driver;
using NLog.Web;
using Npgsql;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FiasApi.HealthChecks;
using FiasApi.MiddleWare;
using GeoCoder.Factories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FiasApi
{
	public class Startup
	{
		private ILogger<Startup> _logger;
		private readonly IConfiguration _configuration;
		public Startup(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();
			services.AddHttpClient();

			services.AddLogging(
				logging =>
				{
					logging.ClearProviders();
					logging.AddNLogWeb();
				});

			_logger = new Logger<Startup>(LoggerFactory.Create(logging =>
				logging.AddNLogWeb(NLogBuilder.ConfigureNLog("NLog.config").Configuration)));

			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.RequireHttpsMetadata = false;
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = false,
						ValidateAudience = false,
						ValidateLifetime = false,
						ValidateIssuerSigningKey = true,
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
							_configuration.GetValue<string>("Security:Token:SecurityKey")
						)),
					};
				});

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "FiasAPI", Version = "v1" });
			});

			services.AddScoped<ISessionFactory>((sp) => ConfigureFiasConnection());
			services.AddSingleton<IYandexGeoCoderModelFactory, YandexGeoCoderModelFactory>();

			services.AddHealthChecks().AddCheck<FiasHelthCheck>(nameof(FiasHelthCheck));
		}

		private ISessionFactory ConfigureFiasConnection()
		{
			_logger.LogInformation("Connect to FIAS...");
			var connectionBuilder = new NpgsqlConnectionStringBuilder();
			var fiasSection = _configuration.GetSection("ConnectionStrings:FiasConnection");

			connectionBuilder.Host = fiasSection.GetValue<string>( "Host");
			connectionBuilder.Port = fiasSection.GetValue<int>("Port"); ;
			connectionBuilder.Database = fiasSection.GetValue<string>("Database");
			connectionBuilder.Username = fiasSection.GetValue<string>("Username");
			connectionBuilder.Password = fiasSection.GetValue<string>("Password");
			connectionBuilder.SslMode = SslMode.Disable;

			var databaseConfig = PostgreSQLConfiguration.Standard
					.Dialect<PostgreSQL83Dialect>()
					.AdoNetBatchSize(100)
					.ConnectionString(connectionBuilder.ConnectionString)
					.Driver<NpgsqlDriver>();

			var fluenConfig = Fluently.Configure().Database(databaseConfig);

			fluenConfig.Mappings(m =>
			{
				m.FluentMappings.AddFromAssembly(Assembly.GetAssembly(typeof(Fias.Search.DTO.ApartmentDTO)));
				m.FluentMappings.AddFromAssembly(Assembly.GetAssembly(typeof(Fias.Search.DTO.CityDTO)));
				m.FluentMappings.AddFromAssembly(Assembly.GetAssembly(typeof(Fias.Search.DTO.HouseDTO)));
				m.FluentMappings.AddFromAssembly(Assembly.GetAssembly(typeof(Fias.Search.DTO.StreetDTO)));
			});

			return fluenConfig.BuildSessionFactory();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if(env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseSwagger();
			app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FiasAPI v1"));

			app.UseHttpsRedirection();
			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.UseMiddleware<RequestResponseLoggingMiddleware>();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

			var options = new HealthCheckOptions
			{
				ResponseWriter = WriteResponse,
				AllowCachingResponses = false
			};

			app.UseHealthChecks("/health", options);
		}

		private Task WriteResponse(HttpContext context, HealthReport healthReport)
		{
			context.Response.ContentType = "application/json; charset=utf-8";

			var options = new JsonWriterOptions { Indented = true };

			using var memoryStream = new MemoryStream();
			using(var jsonWriter = new Utf8JsonWriter(memoryStream, options))
			{
				jsonWriter.WriteStartObject();
				jsonWriter.WriteString("status", healthReport.Status.ToString());
				jsonWriter.WriteString("description", healthReport.Entries.FirstOrDefault().Value.Description);

				foreach(var healthReportEntry in healthReport.Entries)
				{
					jsonWriter.WriteStartObject("data");

					foreach(var item in healthReportEntry.Value.Data)
					{
						jsonWriter.WritePropertyName(item.Key);

						JsonSerializer.Serialize(jsonWriter, item.Value, item.Value?.GetType());
					}

					if(healthReportEntry.Value.Exception is { } exception)
					{
						jsonWriter.WritePropertyName("exception");
						jsonWriter.WriteStringValue(exception.ToString());
					}

					jsonWriter.WriteEndObject();
				}

				jsonWriter.WriteEndObject();
			}

			return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
		}
	}
}
