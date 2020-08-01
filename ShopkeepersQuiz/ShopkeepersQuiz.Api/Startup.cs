using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Repositories.Context;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Services;

namespace ShopkeepersQuiz.Api
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();

			services.AddDbContext<ApplicationDbContext>();

			RegisterDependencyInjection(services);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
		}

		/// <summary>
		/// Adds the application's services to the DI container.
		/// </summary>
		private void RegisterDependencyInjection(IServiceCollection services)
		{
			// AConfiguration
			services.Configure<ConnectionStrings>(options => Configuration.GetSection(nameof(ConnectionStrings)).Bind(options));
			services.Configure<QuestionSettings>(options => Configuration.GetSection(nameof(QuestionSettings)).Bind(options));

			// App Services
			services.AddScoped<IQuestionService, QuestionService>();

			// App Repositories
			services.AddTransient<IQuestionRepository, QuestionRepository>();
		}
	}
}
