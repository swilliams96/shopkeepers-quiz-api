using ShopkeepersQuiz.Api.Models.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Queues
{
	public interface IQueueRepository
	{
		/// <summary>
		/// Retrieves all the <see cref="QueueEntry"/> entities that have a <see cref="QueueEntry.StartTimeUtc"/> in the future.
		/// </summary>
		public Task<IEnumerable<QueueEntry>> GetUpcomingQueueEntries();

		/// <summary>
		/// Adds the given new <see cref="QueueEntry"/> to the database.
		/// </summary>
		public Task<QueueEntry> CreateQueueEntry(QueueEntry queueEntry);
	}
}
