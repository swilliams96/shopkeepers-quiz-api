using Flurl;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ShopkeepersQuiz.Api.Models.GameEntities;
using ShopkeepersQuiz.Api.Repositories.Heroes;
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
	public class FandomScraper : IScraper
	{
		private const string BaseUrl = @"https://dota2.fandom.com/";
		private const string HeroesListUrl = @"https://dota2.fandom.com/wiki/Heroes";

		private const string HeroAncorLinksXPath = "//div[@id='content']//tbody[1]//tr[td]/td/div//a";
		private const string AbilityDetailsXPath = "//div[@class='ability-background']/div[1]";
		private const string AbilityImageXPath = ".//div[contains(@class, 'ico_')]/a/img[1]";
		private const string AbilityManaCostXPath = ".//a[@href='/wiki/Mana']/../parent::div";
		private const string AbilityCooldownXPath = ".//a[@href='/wiki/Cooldown']/../parent::div";

		private readonly IHeroRepository _heroRepository;
		private readonly ILogger _logger;

		private readonly HtmlWeb _web = new HtmlWeb();

		/// <summary>
		/// The abilities that shouldn't be stored as they are not "real" abilities that we want to generate questions for.
		/// </summary>
		private readonly Dictionary<string, string[]> AbilityExceptions = new Dictionary<string, string[]>()
		{
			{ "Ancient Apparition", new string[] { "Release" } },
			{ "Alchemist", new string[] { "Unstable Concoction Throw", "Aghanim's Scepter Synth" } },
			{ "Bane", new string[] { "Nightmare End" } },
			{ "Elder Titan", new string[] { "Return Astral Spirit" } },
			{ "Hoodwink", new string[] { "End Sharpshooter" } },
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

		public FandomScraper(IHeroRepository heroRepository, ILogger logger)
		{
			_heroRepository = heroRepository;
			_logger = logger.ForContext<FandomScraper>();

			_web = new HtmlWeb();
			_web.UserAgent = "Shopkeeper's Quiz";
		}

		public async Task RunScraper(IServiceScope scope)
		{
			IEnumerable<Hero> scrapedHeroes = ScrapeHeroes();
			await SaveHeroData(scrapedHeroes);

			IEnumerable<Ability> scrapedAbilities = await ScrapeAbilities();
			await SaveAbilityData(scrapedAbilities);
		}

		/// <summary>
		/// Scrapes the list of heroes from the wiki.
		/// </summary>
		private IEnumerable<Hero> ScrapeHeroes()
		{
			HtmlDocument heroesListHtml = _web.Load(HeroesListUrl);

			HtmlNodeCollection heroAnchorLinkNodes = heroesListHtml.DocumentNode.SelectNodes(HeroAncorLinksXPath);
			if (!(heroAnchorLinkNodes?.Any() ?? false))
			{
				throw new NodeNotFoundException("Failed to find any heroes on the hero list at: " + HeroesListUrl);
			}

			return heroAnchorLinkNodes
				.Select(heroLink => new Hero(
					name: heroLink.Attributes["title"].DeEntitizeValue,
					wikiPageUrl: Url.Combine(BaseUrl, heroLink.Attributes["href"].Value)))
				.OrderBy(hero => hero.Name);
		}

		/// <summary>
		/// Scrapes all of the abilities for each of the heroes provided.
		/// </summary>
		private async Task<IEnumerable<Ability>> ScrapeAbilities()
		{
			var abilities = new List<Ability>();

			IEnumerable<Hero> heroes = await _heroRepository.GetAllHeroes();

			foreach (Hero hero in heroes)
			{
				HtmlDocument heroDetailsHtml = _web.Load(hero.WikiPageUrl);

				HtmlNodeCollection heroAbilityNodes = heroDetailsHtml.DocumentNode.SelectNodes(AbilityDetailsXPath);
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

					abilities.Add(new Ability(
						name: abilityName,
						heroId: hero.Id,
						imageUrl: GetImageUrlForAbility(heroAbilityNode),
						manaCost: GetManaCostForAbility(heroAbilityNode),
						cooldown: GetCooldownForAbility(heroAbilityNode)));
				}

				_logger.Debug("Found {AbilitiesCount} abilities for {HeroName}", abilities.Count, hero.Name);


				// Add a delay after each request so we don't accidentally run a DoS attack!
				Thread.Sleep(400);
			}

			return abilities;
		}

		/// <summary>
		/// Gets the image URL for an ability.
		/// </summary>
		/// <param name="abilityNode">The <see cref="HtmlNode"/> containing the ability data.</param>
		private string GetImageUrlForAbility(HtmlNode abilityNode)
		{
			return abilityNode.SelectSingleNode(AbilityImageXPath)?.Attributes["src"].Value;
		}

		/// <summary>
		/// Gets the mana cost of an ability.
		/// </summary>
		/// <param name="abilityNode">The <see cref="HtmlNode"/> containing the ability data.</param>
		private string GetManaCostForAbility(HtmlNode abilityNode)
		{
			string manaCost = HttpUtility.HtmlDecode(
				abilityNode.SelectSingleNode(AbilityManaCostXPath)?.GetDirectInnerText());

			return string.IsNullOrWhiteSpace(manaCost) ? 0.ToString() : manaCost.Trim();
		}

		/// <summary>
		/// Gets the cooldown of an ability.
		/// </summary>
		/// <param name="abilityNode">The <see cref="HtmlNode"/> containing the ability data.</param>
		private string GetCooldownForAbility(HtmlNode abilityNode)
		{
			string cooldown = HttpUtility.HtmlDecode(
				abilityNode.SelectSingleNode(AbilityCooldownXPath)?.GetDirectInnerText());

			int bracketIndex = cooldown?.IndexOf("(") ?? -1;
			if (bracketIndex != -1)
			{
				cooldown = cooldown?.Substring(0, bracketIndex);
			}

			return string.IsNullOrWhiteSpace(cooldown) ? 0.ToString() : cooldown.Trim();
		}

		/// <summary>
		/// Saves the given <see cref="Hero"/> entities in the database if they don't already exist or have been 
		/// modified since the last scrape. This means that pregenerated questions and answers for entities that have
		/// not changed can be persisted changed to allow for better data collection on question difficulty.
		/// </summary>
		/// <param name="heroes">The scraped <see cref="Hero"/> entities to save.</param>
		/// <returns>The final list of all <see cref="Hero"/> entities after the save operation is complete.</returns>
		private async Task SaveHeroData(IEnumerable<Hero> heroes)
		{
			var existingHeroes = await _heroRepository.GetAllHeroes();

			var unsavedHeroes = heroes
				.Where(hero => !existingHeroes.Any(x => x.Name.Equals(hero.Name, StringComparison.InvariantCultureIgnoreCase)))
				.OrderBy(x => x.Name)
				.ToList();
			var notFoundHeroIds = existingHeroes
				.Where(existingHero => !heroes.Any(x => x.Name.Equals(existingHero.Name, StringComparison.InvariantCultureIgnoreCase)))
				.Select(x => x.Id)
				.ToList();

			// Remove missing heroes
			await _heroRepository.DeleteHeroes(notFoundHeroIds);

			// Add new heroes
			await _heroRepository.CreateHeroes(unsavedHeroes);

			_logger.Information(
				"Hero data saved - {NewCount} new, {RemovedCount} removed, {UpdatedHeroCount} updated",
				unsavedHeroes.Count,
				notFoundHeroIds.Count,
				heroes.Count() - unsavedHeroes.Count);
		}

		/// <summary>
		/// Saves the given <see cref="Ability"/> entities in the database if they don't already exist or have been 
		/// modified since the last scrape. This means that pregenerated questions and answers for entities that have
		/// not changed can be persisted changed to allow for better data collection on question difficulty.
		/// </summary>
		/// <param name="scrapedAbilities">The scraped <see cref="Ability"/> entities to save.</param>
		private async Task SaveAbilityData(IEnumerable<Ability> scrapedAbilities)
		{
			IEnumerable<Hero> heroes = await _heroRepository.GetAllHeroes();

			foreach (Hero hero in heroes)
			{
				IEnumerable<Ability> scrapedHeroAbilities = scrapedAbilities.Where(x => x.HeroId == hero.Id);

				IEnumerable<Ability> removedHeroAbilities = Enumerable.Empty<Ability>();

				foreach (Ability ability in hero.Abilities ??= new List<Ability>())
				{
					Ability matchingScrapedAbility = scrapedHeroAbilities.SingleOrDefault(x => x.Name == ability.Name);
					if (matchingScrapedAbility != null)
					{
						// Update existing ability data on the hero
						ability.ImageUrl = matchingScrapedAbility.ImageUrl;
						ability.ManaCost = matchingScrapedAbility.ManaCost;
						ability.Cooldown = matchingScrapedAbility.Cooldown;
					}
					else
					{
						// Remove missing abilities from the hero
						removedHeroAbilities.Append(ability);
					}
				}

				foreach (Ability removedAbility in removedHeroAbilities)
				{
					hero.Abilities.Remove(removedAbility);
				}

				foreach (Ability scrapedAbility in scrapedHeroAbilities)
				{
					if (!hero.Abilities.Any(x => x.Name == scrapedAbility.Name))
					{
						// Add missing abilities to the hero
						hero.Abilities.Add(scrapedAbility);
					}
				}

				await _heroRepository.UpdateHero(hero);
				await _heroRepository.UpdateHero(hero);
			}
		}
	}
}
