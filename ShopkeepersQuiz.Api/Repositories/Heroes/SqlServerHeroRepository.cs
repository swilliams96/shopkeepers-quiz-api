using Microsoft.EntityFrameworkCore;
using ShopkeepersQuiz.Api.Models.GameEntities;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Repositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Heroes
{
	public class SqlServerHeroRepository : IHeroRepository
	{
		private ApplicationDbContext _context;

		public SqlServerHeroRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Hero>> GetAllHeroes()
		{
			return await _context.Heroes.ToListAsync();
		}

		public async Task<IEnumerable<Hero>> CreateHeroes(IEnumerable<Hero> heroes)
		{
			_context.Heroes.AddRange(heroes);

			await _context.SaveChangesAsync();

			return heroes;
		}

		public async Task DeleteHeroes(IEnumerable<string> heroIds)
		{
			var heroesToDelete = await _context.Heroes
				.Where(x => heroIds.Contains(x.Id.ToString()))
				.Include(x => x.Abilities)
				.ToListAsync();

			var abilitiesToDelete = heroesToDelete.SelectMany(x => x.Abilities);

			var questionsToDelete = await _context.Questions
				.Where(x => abilitiesToDelete.Any(a => a.Id == x.AbilityId))
				.ToListAsync();

			var queueEntriesToDelete = await _context.QueueEntries
				.Where(x => questionsToDelete.Any(q => q.Id == x.QuestionId))
				.ToListAsync();

			_context.RemoveRange(queueEntriesToDelete);
			_context.RemoveRange(questionsToDelete);
			_context.RemoveRange(abilitiesToDelete);
			_context.RemoveRange(heroesToDelete);

			await _context.SaveChangesAsync();
		}

		public async Task<Hero> UpdateHero(Hero hero)
		{
			_context.Update(hero);

			await _context.SaveChangesAsync();

			return hero;
		}
	}
}
