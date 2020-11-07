using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ShopkeepersQuiz.Api.BackgroundServices;
using System;

namespace ShopkeepersQuiz.Api
{
	public class Program
	{
		public static void Main(string[] args)
		{
			string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

			IConfiguration loggerConfig = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			string appName = loggerConfig.GetValue<string>("ApplicationName") ?? "Shopkeeper's Quiz";
			string appVersion = loggerConfig.GetValue<string>("ApplicationVersion") ?? "0.0.0";

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(loggerConfig)
				.Enrich.FromLogContext()
				.Enrich.WithProperty("ApplicationVersion", appVersion)
				.CreateLogger();

			try
			{
				Log.Information($"Starting {appName} v{appVersion}");

				CreateHostBuilder(args).Build().Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Program terminated unexpectedly!");

				throw;
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				})
				.ConfigureServices(services =>
				{
					services.AddHostedService<WebScraperBackgroundService>();
				})
				.UseSerilog();
	}
}
