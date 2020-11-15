using System;
using System.Linq;

namespace ShopkeepersQuiz.Api.Utilities
{
	public class RandomHelper
	{
		/// <summary>
		/// Chooses a random option from the given set of options and returns it.
		/// </summary>
		/// <param name="options">The options to choose from.</param>
		/// <returns>One option at random.</returns>
		public T ChooseRandomOption<T>(params T[] options)
		{
			if (!options?.Any() ?? true)
			{
				throw new ArgumentNullException(nameof(options), "At least one option must be provided.");
			}

			return options.OrderBy(x => Guid.NewGuid()).First();
		}

		/// <summary>
		/// Chooses a random number between the given min and max numbers.
		/// </summary>
		public int ChooseRandomNumberBetween(int min, int max)
		{
			if (min > max)
			{
				throw new ArgumentException("The minimum value cannot be greater than the maximum value.");
			}

			return ChooseRandomOption(Enumerable.Range(min, max - min).ToArray());
		}
	}
}
