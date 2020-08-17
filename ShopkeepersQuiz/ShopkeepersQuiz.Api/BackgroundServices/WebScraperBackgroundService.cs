using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Services.Scrapers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.BackgroundServices
{
	/// <summary>
	/// Hosted background service for running the webscrapers on a configured time interval.
	/// </summary>
	public class WebScraperBackgroundService : IHostedService, IDisposable
	{
		private readonly ScraperSettings _scraperSettings;
		private readonly IEnumerable<IScraper> _scrapers;
		private readonly IServiceScopeFactory _serviceScopeFactory;

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

		public WebScraperBackgroundService(
			IOptions<ScraperSettings> scraperSettings,
			IEnumerable<IScraper> scrapers,
			IServiceScopeFactory serviceScopeFactory)
		{
			_scraperSettings = scraperSettings.Value;
			_scrapers = scrapers;
			_serviceScopeFactory = serviceScopeFactory;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_cronExpression = CronExpression.Parse(_scraperSettings.RunEvery);

			// Every 60 seconds check if the scrapers should run again and run them if necessary.
			_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

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
		private void DoWork(object state)
		{
				if (_nextRunTimeUtc <= DateTime.UtcNow)
				{
					if (_currentlyRunning)
					{
						Console.WriteLine("Scrapers are already running! Skipping this run...");
					}
					else
					{
						RunScrapers().ConfigureAwait(false);
					}

					_nextRunTimeUtc = _cronExpression.GetNextOccurrence(DateTime.UtcNow) ?? DateTime.MaxValue;
				}
		}

		/// <summary>
		/// Runs the scrapers.
		/// </summary>
		private async Task RunScrapers()
		{
			_currentlyRunning = true;

			using var scope = _serviceScopeFactory.CreateScope();

			try
			{
				foreach (var scraper in _scrapers)
				{
					Console.WriteLine($"Running {scraper.GetType().Name}...");
					await scraper.RunScraper(scope);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unhandled {ex.GetType().Name} thrown in scraper: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
			}
			finally
			{
				_currentlyRunning = false;
			}
		}
	}
}
