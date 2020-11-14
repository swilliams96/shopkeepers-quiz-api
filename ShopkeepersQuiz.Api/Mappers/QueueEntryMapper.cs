using ShopkeepersQuiz.Api.Dtos.Queues;
using ShopkeepersQuiz.Api.Models.Queues;
using System;
using System.Linq;

namespace ShopkeepersQuiz.Api.Mappers
{
	public static class QueueEntryMapper
	{
		private const string NoQuestionProvidedMessage = "No question was provided on this queue entry.";

		public static QueueEntryDto MapToDto(this QueueEntry entity)
		{
			if (entity == null)
			{
				return null;
			}

			if (entity.Question == null)
			{
				throw new ArgumentException(NoQuestionProvidedMessage);
			}

			return new QueueEntryDto()
			{
				Id = entity.Id,
				StartTimeUtc = entity.StartTimeUtc,
				EndTimeUtc = entity.EndTimeUtc,
				Key = entity.Question.Key,
				Type = entity.Question.Type,
				Question = entity.Question.Text,
				Answers = entity.AllAnswers.OrderBy(x => Guid.NewGuid()).Select(x => x.MapToDto()),
				ImageUrl = entity.Question.Ability?.ImageUrl
			};
		}
	}
}
