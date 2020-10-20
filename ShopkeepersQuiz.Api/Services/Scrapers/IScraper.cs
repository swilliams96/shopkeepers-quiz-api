using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Scrapers
{
	/// <summary>
	/// Interface for all scrapers that should be run to obtain the data needed to generate questions from.
	/// </summary>
	public interface IScraper
	{
		/// <summary>
		/// Runs the given scraper asynchronously.
		/// </summary>
		public Task RunScraper(IServiceScope scope);
	}
}
