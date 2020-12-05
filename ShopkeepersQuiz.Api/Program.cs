using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ShopkeepersQuiz.Api.BackgroundServices;
using System;
using System.Reflection;

namespace ShopkeepersQuiz.Api
{
	public class Program
	{
		public static string ApplicationName { get; private set; }

		public static string ApplicationVersion => Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion ?? "LOCAL";

		public static void Main(string[] args)
		{
			string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

			IConfiguration loggerConfig = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			ApplicationName = loggerConfig.GetValue<string>("ApplicationName") ?? "Shopkeeper's Quiz API";

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(loggerConfig)
				.Enrich.FromLogContext()
				.Enrich.WithProperty("ApplicationVersion", ApplicationVersion)
				.CreateLogger();

			try
			{
				Log.Information($"Starting {ApplicationName} v{ApplicationVersion}");

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
					services.AddHostedService<QuestionDataBackgroundService>();
				})
				.UseSerilog();
	}
}
