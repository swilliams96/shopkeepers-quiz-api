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

		public async Task<Question> CreateQuestion(Question question)
		{
			if (question == null)
			{
				throw new ArgumentNullException(nameof(question));
			}

			question.Id = Guid.NewGuid();

			await _questions.InsertOneAsync(question);

			return question;
		}

		public async Task DeleteQuestion(Guid questionId)
		{
			await _questions.DeleteOneAsync(x => x.Id == questionId);
		}

		public async Task<Question> UpdateQuestion(Question question)
		{
			if (question?.Id == null || question?.Id == default(Guid))
			{
				throw new ArgumentException("Question is null or does not have an assigned ID");
			}

			await _questions.ReplaceOneAsync(x => x.Id == question.Id, question);
			return question;
		}
	}
}
