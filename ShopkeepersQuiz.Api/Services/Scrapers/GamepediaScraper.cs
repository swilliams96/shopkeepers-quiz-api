using Flurl;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopkeepersQuiz.Api.Models.GameEntities;
using ShopkeepersQuiz.Api.Repositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ShopkeepersQuiz.Api.Services.Scrapers
{
	/// <summary>
	/// Retrieves Hero and Ability data from the Dota 2 Gamepedia wiki (https://dota2.gamepedia.com/), which allows use of its 
	/// data under the CC BY-NC-SA 3.0 licence (https://creativecommons.org/licenses/by-sa/3.0/).
	/// </summary>
	public class GamepediaScraper : IScraper
	{
		const string BaseUrl = @"https://dota2.gamepedia.com";
		const string HeroesListUrl = @"https://dota2.gamepedia.com/Heroes";

		private readonly HtmlWeb _web = new HtmlWeb();

		private ApplicationDbContext _context;

		/// <summary>
		/// The abilities that shouldn't be stored as they are not "real" abilities that we want to generate questions for.
		/// </summary>
		private readonly Dictionary<string, string[]> AbilityExceptions = new Dictionary<string, string[]>()
		{
			{ "Ancient Apparition", new string[] { "Release" } },
			{ "Alchemist", new string[] { "Unstable Concoction Throw", "Aghanim's Scepter Synth" } },
			{ "Bane", new string[] { "Nightmare End" } },
			{ "Elder Titan", new string[] { "Return Astral Spirit" } },
			{ "Invoker", new string[] { "Melting Strike" } },
			{ "Io", new string[] { "Break Tether" } },
			{ "Keeper of the Light", new string[] { "Release Illuminate", "Illuminate (Aghanim's Scepter)", "Release Illuminate (Aghanim's Scepter)" } },
			{ "Morphling", new string[] { "Morph Replicate" } },
			{ "Naga Siren", new string[] { "Song of the Siren End" } },
			{ "Pangolier", new string[] { "Stop Rolling" } },
			{ "Phoenix", new string[] { "Stop Sun Ray", "Toggle Movement", "Launch Fire Spirit", "Stop Icarus Dive" } },
			{ "Timbersaw", new string[] { "Return Chakram", "Return Chakram (Aghanim's Scepter)" } },
			{ "Tusk", new string[] { "Launch Snowball" } },
			{ "Underlord", new string[] { "Cancel Dark Rift" } },
			{ "Undying", new string[] { "Spell Immunity" } },
			{ "Visage", new string[] { "Stone Form" } },
			{ "Warlock", new string[] { "Flaming Fists", "Permanent Immolation" } },
			{ "Weaver", new string[] { "Mana Break" } }
		};

		public async Task RunScraper(IServiceScope scope)
		{
			_context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			_web.UserAgent = "Shopkeeper's Quiz";

			Console.WriteLine("Running Gamepedia scraper...");

			IEnumerable<Hero> scrapedHeroes = ScrapeHeroes();
			await SaveHeroData(scrapedHeroes);

			IEnumerable<Ability> scrapedAbilities = ScrapeAbilities();
			await SaveAbilityData(scrapedAbilities);

			//IEnumerable<Item> scrapedItems = ScrapeItems();
			//await SaveItemData(scrapedItems);

			Console.WriteLine("Finished Gamepedia scraper!");
		}

		/// <summary>
		/// Scrapes the list of heroes from the wiki.
		/// </summary>
		private IEnumerable<Hero> ScrapeHeroes()
		{
			Console.WriteLine($"Scraping heroes...");

			HtmlDocument heroesListHtml = _web.Load(HeroesListUrl);

			HtmlNodeCollection heroAnchorLinkNodes = heroesListHtml.DocumentNode.SelectNodes("//div[@id='bodyContent']//tbody[1]//tr[td]/td/div//a");
			if (!(heroAnchorLinkNodes?.Any() ?? false))
			{
				throw new NodeNotFoundException("Failed to find any heroes on the hero list at: " + HeroesListUrl);
			}

			return heroAnchorLinkNodes
				.Select(heroLink => new Hero()
				{
					Name = heroLink.Attributes["title"].DeEntitizeValue,
					WikiPageUrl = Url.Combine(BaseUrl, heroLink.Attributes["href"].Value)
				})
				.OrderBy(hero => hero.Name);
		}

		/// <summary>
		/// Scrapes all of the abilities for each of the heroes provided.
		/// </summary>
		private IEnumerable<Ability> ScrapeAbilities()
		{
			var abilities = new List<Ability>();

			Console.WriteLine($"Scraping abilities...");

			IEnumerable<Hero> heroes = _context.Heroes.ToList();

			foreach (Hero hero in heroes)
			{
				HtmlDocument heroDetailsHtml = _web.Load(hero.WikiPageUrl);

				Console.WriteLine($"    - {hero.Name}");

				HtmlNodeCollection heroAbilityNodes = heroDetailsHtml.DocumentNode.SelectNodes("//div[@class='ability-background']/div[1]");
				if (!(heroAbilityNodes?.Any() ?? false))
				{
					throw new NodeNotFoundException($"Failed to find any abilities for {hero.Name} at: {hero.WikiPageUrl}");
				}

				foreach (HtmlNode heroAbilityNode in heroAbilityNodes)
				{
					string abilityName = heroAbilityNode.ChildNodes.FindFirst("div")?.GetDirectInnerText().Trim();

					if (string.IsNullOrWhiteSpace(abilityName))
					{
						throw new NodeNotFoundException("Failed to determine the ability name for one of the abilities of " + hero.Name);
					}

					if (AbilityExceptions.ContainsKey(hero.Name) && AbilityExceptions[hero.Name].Contains(abilityName))
					{
						continue;
					}

					if (abilities.Where(x => x.HeroId == hero.Id && x.Name == abilityName).Any())
					{
						continue;
					}

					abilities.Add(new Ability()
					{
						Name = abilityName,
						HeroId = hero.Id,
						ImageUrl = GetImageUrlForAbility(heroAbilityNode),
						ManaCost = GetManaCostForAbility(heroAbilityNode),
						Cooldown = GetCooldownForAbility(heroAbilityNode),
					});
				}

				// Add a delay after each request so we don't accidentally run a denial-of-service attack!
				Thread.Sleep(400);
			}

			return abilities;
		}

		/// <summary>
		/// Gets the mana cost of an ability.
		/// </summary>
		/// <param name="abilityNode">The <see cref="HtmlNode"/> containing the ability data.</param>
		private string GetManaCostForAbility(HtmlNode abilityNode)
		{
			string manaCost = HttpUtility.HtmlDecode(
				abilityNode.SelectSingleNode(".//a[@href='/Mana']/../parent::div")?.GetDirectInnerText());

			return manaCost?.Trim() ?? "0";
		}

		/// <summary>
		/// Gets the cooldown of an ability.
		/// </summary>
		/// <param name="abilityNode">The <see cref="HtmlNode"/> containing the ability data.</param>
		private string GetCooldownForAbility(HtmlNode abilityNode)
		{
			string cooldown = HttpUtility.HtmlDecode(
				abilityNode.SelectSingleNode(".//a[@href='/Cooldown']/../parent::div")?.GetDirectInnerText());

			int bracketIndex = cooldown?.IndexOf("(") ?? -1;
			if (bracketIndex != -1)
			{
				cooldown = cooldown?.Substring(0, bracketIndex);
			}

			return cooldown?.Trim() ?? "0";
		}

		/// <summary>
		/// Gets the image URL for an ability.
		/// </summary>
		/// <param name="abilityNode">The <see cref="HtmlNode"/> containing the ability data.</param>
		private string GetImageUrlForAbility(HtmlNode abilityNode)
		{
			return abilityNode.SelectSingleNode(".//div[contains(@class, 'ico_')]/a/img[1]")?.Attributes["src"].Value;
		}

		/// <summary>
		/// Saves the given <see cref="Hero"/> entities in the database if they don't already exist or have been 
		/// modified since the last scrape. This means that pregenerated questions and answers for entities that have
		/// not changed can be persisted changed to allow for better data collection on question difficulty.
		/// </summary>
		/// <param name="heroes">The scraped <see cref="Hero"/> entities to save.</param>
		/// <returns>The final list of all <see cref="Hero"/> entities after the save operation is complete.</returns>
		private async Task<IEnumerable<Hero>> SaveHeroData(IEnumerable<Hero> heroes)
		{
			IEnumerable<Hero> existingHeroes = await _context.Heroes.ToListAsync();

			IEnumerable<Hero> unsavedHeroes = heroes.Where(hero =>
				!existingHeroes.Any(x => x.Name.Equals(hero.Name, StringComparison.InvariantCultureIgnoreCase)));
			IEnumerable<Hero> notFoundHeroes = existingHeroes.Where(existingHero =>
				!heroes.Any(x => x.Name.Equals(existingHero.Name, StringComparison.InvariantCultureIgnoreCase)));

			// Remove missing heroes
			_context.Heroes.RemoveRange(notFoundHeroes);

			// Add new heroes
			_context.Heroes.AddRange(unsavedHeroes.OrderBy(x => x.Name));

			await _context.SaveChangesAsync();

			return await _context.Heroes
				.Include(x => x.Abilities)
				.ToListAsync();
		}

		/// <summary>
		/// Saves the given <see cref="Ability"/> entities in the database if they don't already exist or have been 
		/// modified since the last scrape. This means that pregenerated questions and answers for entities that have
		/// not changed can be persisted changed to allow for better data collection on question difficulty.
		/// </summary>
		/// <param name="scrapedAbilities">The scraped <see cref="Ability"/> entities to save.</param>
		/// <param name="heroes">The <see cref="Hero"/> entities that are in the database.</param>
		private async Task SaveAbilityData(IEnumerable<Ability> scrapedAbilities)
		{
			IEnumerable<Hero> heroes = _context.Heroes.Include(x => x.Abilities).ToList();
			IEnumerable<Ability> existingAbilities = _context.Abilities.ToList();

			foreach (Hero hero in heroes)
			{
				IEnumerable<Ability> scrapedHeroAbilities = scrapedAbilities.Where(x => x.HeroId == hero.Id);

				foreach (Ability ability in hero.Abilities)
				{
					Ability matchingScrapedAbility = scrapedHeroAbilities.SingleOrDefault(x => x.Name == ability.Name);
					if (matchingScrapedAbility != null)
					{
						// Update existing ability on the hero
						ability.ImageUrl = matchingScrapedAbility.ImageUrl;
						ability.ManaCost = matchingScrapedAbility.ManaCost;
						ability.Cooldown = matchingScrapedAbility.Cooldown;
					}
					else
					{
						// Remove missing abilities from the hero
						_context.Remove(ability);
					}
				}

				foreach (Ability scrapedAbility in scrapedHeroAbilities)
				{
					if (!hero.Abilities.Any(x => x.Name == scrapedAbility.Name))
					{
						// Add missing abilities to the hero
						hero.Abilities.Add(scrapedAbility);
					}
				}
			}

			await _context.SaveChangesAsync();
		}
	}
}
