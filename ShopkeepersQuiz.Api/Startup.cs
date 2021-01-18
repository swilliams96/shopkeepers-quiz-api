using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Repositories.Context;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Repositories.Queues;
using ShopkeepersQuiz.Api.Services.Questions;
using ShopkeepersQuiz.Api.Services.Questions.Generation;
using ShopkeepersQuiz.Api.Services.Scrapers;
using ShopkeepersQuiz.Api.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ShopkeepersQuiz.Api
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();

			services.AddRouting(config => config.LowercaseUrls = true);

			services.AddDbContext<ApplicationDbContext>();

			services.AddMemoryCache();

			services.AddSwaggerGen(config =>
			{
				var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
				config.IncludeXmlComments(xmlPath);
			});

			services.AddSingleton(Log.Logger);

			RegisterDependencyInjection(services);
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();

				Log.Logger.Debug("Environment variables:" 
					+ Environment.NewLine 
					+ string.Join(Environment.NewLine + "\t", Configuration.AsEnumerable().Select(x => x.Key + ": " + x.Value))
					+ Environment.NewLine);
			}

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

			app.UseSwagger();
			app.UseSwaggerUI(config =>
			{
				config.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Program.ApplicationName} v{Program.ApplicationVersion}");
				config.RoutePrefix = string.Empty;
			});


			app.UseSerilogRequestLogging();

			EnsureDatabaseMigrated(app);
		}

		/// <summary>
		/// Ensures that the database is migrated fully using EF Core.
		/// </summary>
		private static void EnsureDatabaseMigrated(IApplicationBuilder app)
		{
			using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
			using var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			context.Database.Migrate();
		}

		/// <summary>
		/// Adds the application's services to the DI container.
		/// </summary>
		private void RegisterDependencyInjection(IServiceCollection services)
		{
			// Configuration
			services.Configure<ConnectionStrings>(options => Configuration.GetSection(nameof(ConnectionStrings)).Bind(options));
			services.Configure<QuestionSettings>(options => Configuration.GetSection(nameof(QuestionSettings)).Bind(options));
			services.Configure<ScraperSettings>(options => Configuration.GetSection(nameof(ScraperSettings)).Bind(options));

			// App Services
			services.AddScoped<IQuestionService, QuestionService>();

			// App Repositories
			services.AddTransient<IQuestionRepository, SqlServerQuestionRepository>();
			services.AddTransient<IQueueRepository, SqlServerQueueRepository>();

			// App Utilities
			services.AddSingleton<RandomHelper>();

			// Scrapers
			services.AddTransient<IScraper, GamepediaScraper>();

			// Question Generators
			services.AddScoped<IQuestionGenerator, CooldownQuestionGenerator>();
			services.AddScoped<IQuestionGenerator, ManaCostQuestionGenerator>();
		}
	}
}
