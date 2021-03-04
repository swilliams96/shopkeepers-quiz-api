using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Questions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ShopkeepersQuiz.Api.Models.Queues
{
	public class QueueEntry
	{
		const int IncorrectAnswerCount = 3;

		public QueueEntry(Question question, DateTime startTimeUtc, DateTime endTimeUtc)
		{
			Id = Guid.NewGuid();
			Question = question;
			QuestionId = question.Id;
			StartTimeUtc = startTimeUtc;
			EndTimeUtc = endTimeUtc;

			ChooseNewIncorrectAnswers();
		}

		[JsonConstructor]
		private QueueEntry()
		{
			// Necessary for EF Core and Newtonsoft
		}

		[BsonId]
		public Guid Id { get; set; }

		public Guid QuestionId { get; set; }

		public DateTime StartTimeUtc { get; set; }

		public DateTime EndTimeUtc { get; set; }

		public Guid[] IncorrectAnswerIds { get; set; }

		public Question Question { get; set; }

		// Calculated Properties

		[NotMapped]
		[JsonIgnore]
		[BsonIgnore]
		public Answer CorrectAnswer => Question?.Answers?.SingleOrDefault(x => x.Correct);

		[NotMapped]
		[JsonIgnore]
		[BsonIgnore]
		public IEnumerable<Answer> IncorrectAnswers => Question?.Answers?.Where(x => IncorrectAnswerIds?.Contains(x.Id) ?? false) ?? Enumerable.Empty<Answer>();

		[NotMapped]
		[JsonIgnore]
		[BsonIgnore]
		public IEnumerable<Answer> AllAnswers => IncorrectAnswers.Append(CorrectAnswer);

		/// <summary>
		/// Picks out some incorrect answers from the supplied <see cref="Question"/> to use as the incorrect options.
		/// </summary>
		private void ChooseNewIncorrectAnswers()
		{
			int incorrectAnswerCount = Question.Answers?.Where(x => !x.Correct).Count() ?? 0;
			if (incorrectAnswerCount < IncorrectAnswerCount)
			{
				throw new InvalidOperationException(
					$"The Question supplied contains {incorrectAnswerCount} incorrect Answers but a minimum of {IncorrectAnswerCount} incorrect Answers were expected.");
			}

			IncorrectAnswerIds = Question.Answers
				.Where(x => !x.Correct)
				.OrderBy(x => Guid.NewGuid())
				.Take(IncorrectAnswerCount)
				.Select(x => x.Id)
				.ToArray();
		}
	}
}
