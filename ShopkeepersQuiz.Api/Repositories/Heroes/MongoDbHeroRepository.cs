using MongoDB.Driver;
using ShopkeepersQuiz.Api.Models.GameEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Heroes
{
	public class MongoDbHeroRepository : IHeroRepository
	{
		const string DatabaseCollection = "heroes";

		private readonly IMongoCollection<Hero> _heroes;

		public MongoDbHeroRepository(IMongoDatabase mongoDatabase)
		{
			_heroes = mongoDatabase.GetCollection<Hero>(DatabaseCollection);
		}

		public async Task<IEnumerable<Hero>> GetAllHeroes()
		{
			return await _heroes.Find(x => true).ToListAsync();
		}

		public async Task<IEnumerable<Hero>> CreateHeroes(IEnumerable<Hero> heroes)
		{
			await _heroes.InsertManyAsync(heroes, new InsertManyOptions() { IsOrdered = true });
			return heroes;
		}

		public async Task DeleteHeroes(IEnumerable<string> heroIds)
		{
			var filter = Builders<Hero>.Filter.In(nameof(Hero.Id), heroIds);
			await _heroes.DeleteManyAsync(filter);
		}

		public async Task<Hero> UpdateHero(Hero hero)
		{
			await _heroes.ReplaceOneAsync(x => x.Id == hero.Id, hero);
			return hero;
		}
	}
}
