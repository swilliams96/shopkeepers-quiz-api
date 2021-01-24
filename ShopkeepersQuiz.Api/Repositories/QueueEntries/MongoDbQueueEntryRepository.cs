using MongoDB.Driver;
using ShopkeepersQuiz.Api.Models.Messages;
using ShopkeepersQuiz.Api.Models.Queues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.QueueEntries
{
	public class MongoDbQueueEntryRepository : IQueueEntryRepository
	{
		const string DatabaseCollection = "queue-entries";

		private readonly IMongoCollection<QueueEntry> _queueEntries;
		public MongoDbQueueEntryRepository(IMongoDatabase mongoDatabase)
		{
			_queueEntries = mongoDatabase.GetCollection<QueueEntry>(DatabaseCollection);
		}

		public async Task<IEnumerable<QueueEntry>> GetUpcomingQueueEntries()
		{
			return await _queueEntries.Find(x => x.StartTimeUtc >= DateTime.UtcNow).ToListAsync();
		}

		public async Task<QueueEntry> CreateQueueEntry(QueueEntry queueEntry)
		{
			if (await ConflictsWithExistingQueueEntry(queueEntry))
			{
				throw new InvalidOperationException(ResponseMessages.Errors.GenericInvalidOperation);
			}

			await _queueEntries.InsertOneAsync(queueEntry);

			return queueEntry;
		}

		/// <summary>
		/// Checks if the timing of the given <see cref="QueueEntry"/> will overlap with any other one in the database.
		/// </summary>
		private async Task<bool> ConflictsWithExistingQueueEntry(QueueEntry queueEntry)
		{
			DateTime start = queueEntry.StartTimeUtc;
			DateTime end = queueEntry.EndTimeUtc;

			return await _queueEntries.Find(x => 
				(x.StartTimeUtc <= start && x.EndTimeUtc > start) ||
				(x.StartTimeUtc < end && x.EndTimeUtc >= end))
				.AnyAsync();
		}
	}
}
