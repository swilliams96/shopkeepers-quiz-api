using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ShopkeepersQuiz.Api.Extensions;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Repositories.Heroes;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Repositories.QueueEntries;
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

			services.AddMongoDb(Configuration);

			//services.AddDbContext<ApplicationDbContext>();

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
			services.AddSingleton<IQuestionService, QuestionService>();

			// App Repositories
			services.AddSingleton<IQuestionRepository, MongoDbQuestionRepository>();
			services.AddSingleton<IQueueEntryRepository, MongoDbQueueEntryRepository>();
			services.AddSingleton<IHeroRepository, MongoDbHeroRepository>();

			// App Utilities
			services.AddSingleton<RandomHelper>();

			// Scrapers
			services.AddTransient<IScraper, FandomScraper>();

			// Question Generators
			services.AddTransient<IQuestionGenerator, CooldownQuestionGenerator>();
			services.AddTransient<IQuestionGenerator, ManaCostQuestionGenerator>();
		}
	}
}
