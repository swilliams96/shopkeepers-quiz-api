using Newtonsoft.Json;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Questions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

		[Key]
		public Guid Id { get; set; }

		public int QuestionId { get; set; }

		public DateTime StartTimeUtc { get; set; }

		public DateTime EndTimeUtc { get; set; }

		public int[] IncorrectAnswerIds { get; set; }

		// Navigation Properties

		public Question Question { get; set; }

		// Calculated Properties

		[NotMapped]
		[JsonIgnore]
		public Answer CorrectAnswer => Question?.Answers?.SingleOrDefault(x => x.Correct);

		[NotMapped]
		[JsonIgnore]
		public IEnumerable<Answer> IncorrectAnswers => Question?.Answers?.Where(x => IncorrectAnswerIds.Contains(x.Id)) ?? Enumerable.Empty<Answer>();

		[NotMapped]
		[JsonIgnore]
		public IEnumerable<Answer> AllAnswers => IncorrectAnswers.Append(CorrectAnswer);

		/// <summary>
		/// Picks out some incorrect answers from the supplied <see cref="Question"/> to use as the incorrect options.
		/// </summary>
		private void ChooseNewIncorrectAnswers()
		{
			int answerCount = Question.Answers?.Count ?? 0;
			if (answerCount < IncorrectAnswerCount)
			{
				throw new InvalidOperationException($"The Question supplied contains {answerCount} Answers but a minimum of {IncorrectAnswerCount} Answers is expected.");
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
