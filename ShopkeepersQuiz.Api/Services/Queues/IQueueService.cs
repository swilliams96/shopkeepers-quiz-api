using ShopkeepersQuiz.Api.Models.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Queues
{
	public interface IQueueService
	{
		/// <summary>
		/// Stores any untracked queue entries in the provided queue to the database.
		/// </summary>
		public Task UpdateQueue(IEnumerable<QueueEntry> questionQueue);
	}
}
