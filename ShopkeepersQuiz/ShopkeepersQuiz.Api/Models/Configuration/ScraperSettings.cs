namespace ShopkeepersQuiz.Api.Models.Configuration
{
	/// <summary>
	/// Configuration settings for the web scraper background service.
	/// </summary>
	public class ScraperSettings
	{
		/// <summary>
		/// Cron expression to determine how frequently the scrapers should be run.
		/// See https://github.com/HangfireIO/Cronos#cron-format for more information.
		/// </summary>
		public string RunEvery { get; set; }
	}
}
