using ShopkeepersQuiz.Api.Models.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Queues
{
	public interface IQueueService
	{
		/// <summary>
		/// Retrieves all the <see cref="QueueEntry"/> entities that have a <see cref="QueueEntry.StartTimeUtc"/> in the future.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<QueueEntry>> GetUpcomingQueueEntries();

		/// <summary>
		/// Stores any untracked queue entries in the provided queue to the database.
		/// </summary>
		public Task UpdateQueue(IEnumerable<QueueEntry> questionQueue);
	}
}
