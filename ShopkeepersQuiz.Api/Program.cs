using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ShopkeepersQuiz.Api.BackgroundServices;
using System;
using System.IO;
using System.Reflection;

namespace ShopkeepersQuiz.Api
{
	public class Program
	{
		public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }.json", optional: true)
			.AddEnvironmentVariables("ShopkeepersQuizApi_")
			.Build();

		public static readonly string ApplicationName = Configuration.GetValue("Name", "Shopkeeper's Quiz API");

		public static readonly string ApplicationVersion = Configuration.GetValue("Version", "0.0.0");

		public static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(Configuration)
				.Enrich.FromLogContext()
				.Enrich.WithProperty(nameof(ApplicationVersion), ApplicationVersion)
				.CreateLogger();

			try
			{
				Log.Information($"Starting {ApplicationName} v{ApplicationVersion}...");

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
				.UseSerilog()
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				})
				.ConfigureServices(services =>
				{
					services.AddHostedService<WebScraperBackgroundService>();
				});
		}
}
