using ShopkeepersQuiz.Api.Models.GameEntities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Heroes
{
	public interface IHeroRepository
	{
		/// <summary>
		/// Gets all of the <see cref="Hero"/>es stored in the database.
		/// </summary>
		public Task<IEnumerable<Hero>> GetAllHeroes();

		/// <summary>
		/// Adds the given list of <see cref="Hero"/>es to the database.
		/// </summary>
		/// <param name="heroes">The <see cref="Hero"/>es to create.</param>
		public Task<IEnumerable<Hero>> CreateHeroes(IEnumerable<Hero> heroes);

		/// <summary>
		/// Deletes all of the given <see cref="Hero"/>es.
		/// </summary>
		/// <param name="heroIds">The IDs of the <see cref="Hero"/>es to delete.</param>
		public Task DeleteHeroes(IEnumerable<Guid> heroIds);

		/// <summary>
		/// Updates a single <see cref="Hero"/> with a matching ID.
		/// </summary>
		/// <param name="hero">The <see cref="Hero"/> to update.</param>
		public Task<Hero> UpdateHero(Hero hero);
	}
}
