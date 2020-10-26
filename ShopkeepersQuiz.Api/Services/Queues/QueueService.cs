using Microsoft.EntityFrameworkCore.Internal;
using ShopkeepersQuiz.Api.Models.Queue;
using ShopkeepersQuiz.Api.Repositories.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Queues
{
	public class QueueService : IQueueService
	{
		private readonly IQueueRepository _queueRepository;

		public QueueService(IQueueRepository queueRepository)
		{
			_queueRepository = queueRepository;
		}

		public Task<IEnumerable<QueueEntry>> GetUpcomingQueueEntries() => _queueRepository.GetUpcomingQueueEntries();

		public async Task UpdateQueue(IEnumerable<QueueEntry> questionQueue)
		{
			if (questionQueue == null || !questionQueue.Any())
			{
				throw new ArgumentNullException(nameof(questionQueue));
			}

			IEnumerable<QueueEntry> existingQueueEntries = await _queueRepository.GetUpcomingQueueEntries();

			IEnumerable<QueueEntry> queueEntriesToAdd = questionQueue
				.Where(x => x.StartTimeUtc >= DateTime.UtcNow)
				.Where(x => !existingQueueEntries.Any(existing => x.Id == existing.Id));

			foreach (var entry in queueEntriesToAdd)
			{
				await _queueRepository.CreateQueueEntry(entry);
			}
		}
	}
}
