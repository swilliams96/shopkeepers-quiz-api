using Microsoft.EntityFrameworkCore;
using ShopkeepersQuiz.Api.Models.Messages;
using ShopkeepersQuiz.Api.Models.Queues;
using ShopkeepersQuiz.Api.Repositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Queues
{
	public class QueueRepository : IQueueRepository
	{
		private readonly ApplicationDbContext _context;

		public QueueRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<QueueEntry>> GetUpcomingQueueEntries()
		{
			return await _context.QueueEntries
				.Include(q => q.Question)
					.ThenInclude(q => q.Ability)
				.Include(q => q.Question)
					.ThenInclude(q => q.Answers)
				.Where(x => x.StartTimeUtc >= DateTime.UtcNow)
				.ToListAsync();
		}

		public async Task<QueueEntry> CreateQueueEntry(QueueEntry queueEntry)
		{
			if (queueEntry == null)
			{
				throw new ArgumentNullException(nameof(queueEntry));
			}

			if (await ConflictsWithExistingQueueEntry(queueEntry))
			{
				throw new InvalidOperationException(ResponseMessages.Errors.GenericInvalidOperation);
			}

			await _context.QueueEntries.AddAsync(queueEntry);
			await _context.SaveChangesAsync();

			return queueEntry;
		}

		/// <summary>
		/// Checks if the timing of the given <see cref="QueueEntry"/> will overlap with any other one in the database.
		/// </summary>
		private async Task<bool> ConflictsWithExistingQueueEntry(QueueEntry queueEntry)
		{
			DateTime start = queueEntry.StartTimeUtc;
			DateTime end = queueEntry.EndTimeUtc;

			return await _context.QueueEntries
				.Where(x => (x.StartTimeUtc <= start && x.EndTimeUtc > start)
					|| (x.StartTimeUtc < end && x.EndTimeUtc >= end))
				.AnyAsync();
		}
	}
}
