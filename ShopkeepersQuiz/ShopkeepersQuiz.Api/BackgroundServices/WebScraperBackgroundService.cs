using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ShopkeepersQuiz.Api.Models.Configuration;
using System;
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

		private Timer _timer;
		private CronExpression _cronExpression;
		private DateTime _nextRunTimeUtc = DateTime.MinValue;
		private readonly object _isRunningLock = new object();

		public WebScraperBackgroundService(IOptions<ScraperSettings> scraperSettings)
		{
			_scraperSettings = scraperSettings.Value;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_cronExpression = CronExpression.Parse(_scraperSettings.RunEvery);

			// Every 60 seconds check if the scrapers should run again and run them if necessary.
			_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(20));

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
		/// Callback for the timer 
		/// </summary>
		private void DoWork(object state)
		{
			if (_nextRunTimeUtc <= DateTime.UtcNow)
			{
				RunScrapers().ConfigureAwait(false);

				_nextRunTimeUtc = _cronExpression.GetNextOccurrence(DateTime.UtcNow) ?? DateTime.MaxValue;
			}
		}

		/// <summary>
		/// Runs the scrapers.
		/// </summary>
		private async Task RunScrapers()
		{
			lock (_isRunningLock)
			{
				Console.WriteLine("===== Running Scrapers =====");

				// do work

				Console.WriteLine("===== Scrapers Complete =====");
			}
		}
	}
}
