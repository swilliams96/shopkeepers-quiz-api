﻿using System;

namespace ShopkeepersQuiz.Api.Models.Cache
{
	/// <summary>
	/// Constants for storing and retrieving data from the memory cache.
	/// </summary>
	public static class CacheKeys
	{
		public static string QuestionQueue = nameof(QuestionQueue);
		public static string PreviousQueueEntry(Guid queueId) => $"{nameof(PreviousQueueEntry)}__{queueId:N}";
	}
}
