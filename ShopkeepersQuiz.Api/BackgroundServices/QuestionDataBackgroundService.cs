using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Services.Questions.Generation;
using ShopkeepersQuiz.Api.Services.Scrapers;
using ShopkeepersQuiz.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.BackgroundServices
{
	/// <summary>
	/// Hosted background service for running the webscrapers on a configured time interval.
	/// </summary>
	public class QuestionDataBackgroundService : IHostedService, IDisposable
	{
		private readonly ScraperSettings _scraperSettings;
		private readonly IEnumerable<IScraper> _scrapers;
		private readonly ILogger _logger;
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly DateTimeProvider _dateTimeProvider;

		/// <summary>
		/// The <see cref="Timer"/> used to check if the scrapers should be run.
		/// </summary>
		private Timer _timer;

		/// <summary>
		/// The <see cref="CronExpression"/> parsed from the <see cref="ScraperSettings"/> of the appsettings.json.
		/// </summary>
		private CronExpression _cronExpression;

		/// <summary>
		/// The calculated time of the next scheduled run according to the <see cref="_cronExpression"/>.
		/// </summary>
		private DateTime _nextRunTimeUtc = DateTime.MinValue;

		/// <summary>
		/// Whether the scrapers are currently running. This can be used to ensure the tasks don't overlap.
		/// </summary>
		private bool _currentlyRunning = false;

		public QuestionDataBackgroundService(
			IOptions<ScraperSettings> scraperSettings,
			IEnumerable<IScraper> scrapers,
			ILogger logger,
			IServiceScopeFactory serviceScopeFactory,
			DateTimeProvider dateTimeProvider)
		{
			_scraperSettings = scraperSettings.Value;
			_scrapers = scrapers;
			_logger = logger.ForContext<QuestionDataBackgroundService>();
			_serviceScopeFactory = serviceScopeFactory;
			_dateTimeProvider = dateTimeProvider;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_cronExpression = CronExpression.Parse(_scraperSettings.RunEvery);

			// Every 10 seconds check if the scrapers should run again and run them if necessary.
			_timer = new Timer(DoWorkIfScheduled, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		/// <summary>
		/// Callback for the <see cref="Timer"/> to run the scrapers if the <see cref="_nextRunTimeUtc"/> has passed.
		/// </summary>
		private void DoWorkIfScheduled(object state)
		{
			if (_nextRunTimeUtc <= _dateTimeProvider.GetUtcNow())
			{
				if (_currentlyRunning)
				{
					_logger.Debug("Scrapers are already running! Skipping this run...");
				}
				else
				{
					RunScrapers().ConfigureAwait(false);
				}

				_nextRunTimeUtc = _cronExpression.GetNextOccurrence(_dateTimeProvider.GetUtcNow()) ?? DateTime.MaxValue;
			}
		}

		/// <summary>
		/// Runs the scrapers.
		/// </summary>
		private async Task RunScrapers()
		{
			_currentlyRunning = true;
			_logger.Information("Starting background service run...");

			using var scope = _serviceScopeFactory.CreateScope();

			try
			{
				foreach (var scraper in _scrapers)
				{
					_logger.Information($"Running {scraper.GetType().Name}...");
					await scraper.RunScraper(scope);
				}

				IEnumerable<IQuestionGenerator> questionGenerators = scope.ServiceProvider.GetServices<IQuestionGenerator>();
				foreach (var generator in questionGenerators)
				{
					_logger.Information($"Generating questions using {generator.GetType().Name}...");
					await generator.GenerateQuestions();
				}

			}
			catch (Exception ex)
			{
				_logger.Error(ex, $"Unhandled {ex.GetType().Name} thrown in background service.");
			}
			finally
			{
				_currentlyRunning = false;
				_logger.Information("Background task complete!");
			}
		}
	}
}
