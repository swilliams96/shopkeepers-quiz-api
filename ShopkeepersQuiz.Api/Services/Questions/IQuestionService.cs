using OneOf;
using OneOf.Types;
using ShopkeepersQuiz.Api.Dtos;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Queues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Questions
{
	public interface IQuestionService
	{
		/// <summary>
		/// Gets the next few questions in the queue.
		/// </summary>
		public Task<IEnumerable<QueueEntry>> GetQuestionQueue();

		/// <summary>
		/// Gets the correct <see cref="AnswerDto"/> for a previous <see cref="QueueEntry"/>.
		/// </summary>
		/// <param name="queueEntryId">The ID of the <see cref="QueueEntry"/>.</param>
		public ValueTask<OneOf<AnswerDto, NotFound, AnswerNotAvailableYet>> GetPreviousQueueEntryAnswer(Guid queueEntryId);
	}
}
