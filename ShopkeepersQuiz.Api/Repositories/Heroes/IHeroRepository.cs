using ShopkeepersQuiz.Api.Models.GameEntities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Heroes
{
	public interface IHeroRepository
	{
		public Task<IEnumerable<Hero>> GetAllHeroes();

		public Task<IEnumerable<Hero>> CreateHeroes(IEnumerable<Hero> heroes);

		public Task DeleteHeroes(IEnumerable<Guid> heroIds);

		public Task<Hero> UpdateHero(Hero hero);
	}
}
