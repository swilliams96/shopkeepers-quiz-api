using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Repositories.Context;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Repositories.Queues;
using ShopkeepersQuiz.Api.Services.Questions;
using ShopkeepersQuiz.Api.Services.Questions.Generation;
using ShopkeepersQuiz.Api.Services.Scrapers;

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

			services.AddDbContext<ApplicationDbContext>();

			services.AddMemoryCache();

			RegisterDependencyInjection(services);
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

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
			// AConfiguration
			services.Configure<ConnectionStrings>(options => Configuration.GetSection(nameof(ConnectionStrings)).Bind(options));
			services.Configure<QuestionSettings>(options => Configuration.GetSection(nameof(QuestionSettings)).Bind(options));
			services.Configure<ScraperSettings>(options => Configuration.GetSection(nameof(ScraperSettings)).Bind(options));

			// App Services
			services.AddScoped<IQuestionService, QuestionService>();
			services.AddScoped<IQuestionGenerationService, QuestionGenerationService>();

			// App Repositories
			services.AddTransient<IQuestionRepository, QuestionRepository>();
			services.AddTransient<IQueueRepository, QueueRepository>();

			// Scrapers
			services.AddTransient<IScraper, GamepediaScraper>();
		}
	}
}
