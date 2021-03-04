using MongoDB.Driver;
using MongoDB.Driver.Linq;
using ShopkeepersQuiz.Api.Models.Questions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Questions
{
	public class MongoDbQuestionRepository : IQuestionRepository
	{
		const string DatabaseCollection = "questions";

		private readonly IMongoCollection<Question> _questions;

		public MongoDbQuestionRepository(IMongoDatabase mongoDatabase)
		{
			_questions = mongoDatabase.GetCollection<Question>(DatabaseCollection);
		}

		public async Task<IEnumerable<Question>> GetRandomQuestionsWithAnswers(int count)
		{
			return await _questions.AsQueryable()
				.Sample(count)
				.ToListAsync();
		}

		public async Task<IEnumerable<Question>> GetQuestionsForAbility(Guid abilityId)
		{
			return await _questions.Find(x => x.AbilityId == abilityId).ToListAsync();
		}
	}
}
