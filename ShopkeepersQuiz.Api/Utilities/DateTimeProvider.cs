using System;

namespace ShopkeepersQuiz.Api.Utilities
{
	public class DateTimeProvider
	{
		public virtual DateTime GetUtcNow() => DateTime.UtcNow;
	}
}
