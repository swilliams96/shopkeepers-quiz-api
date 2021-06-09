using MongoDB.Driver;
using ShopkeepersQuiz.Api.Models.Messages;
using ShopkeepersQuiz.Api.Models.Queues;
using ShopkeepersQuiz.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.QueueEntries
{
	public class MongoDbQueueEntryRepository : IQueueEntryRepository
	{
		const string DatabaseCollection = "queue-entries";

		private readonly IMongoCollection<QueueEntry> _queueEntries;
		private readonly DateTimeProvider _dateTimeProvider;

		public MongoDbQueueEntryRepository(IMongoDatabase mongoDatabase, DateTimeProvider dateTimeProvider)
		{
			_queueEntries = mongoDatabase.GetCollection<QueueEntry>(DatabaseCollection);
			_dateTimeProvider = dateTimeProvider;
		}

		public async Task<IEnumerable<QueueEntry>> GetUpcomingQueueEntries()
		{
			return await _queueEntries.Find(x => x.StartTimeUtc >= _dateTimeProvider.GetUtcNow()).ToListAsync();
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
