using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.GameEntities;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Repositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Questions.Generation
{
	public class QuestionGenerationService : IQuestionGenerationService
	{
		private readonly ApplicationDbContext _context;
		private readonly QuestionSettings _questionSettings;
		private readonly Random _random = new Random();

		public QuestionGenerationService(ApplicationDbContext context, IOptions<QuestionSettings> questionSettings)
		{
			_context = context;
			_questionSettings = questionSettings.Value;
		}

		public async Task GenerateQuestions()
		{
			await GenerateAbilityQuestions();
		}

		/// <summary>
		/// Generates the <see cref="Ability"/>-based questions for the app.
		/// </summary>
		private async Task GenerateAbilityQuestions()
		{
			IEnumerable<Ability> abilities = await _context.Abilities.Include(x => x.Hero).ToListAsync();

			foreach (Ability ability in abilities)
			{
				IEnumerable<Question> abilityQuestions = await GetQuestionsForAbility(ability.Id);

				if (abilityQuestions.Any())
				{
					foreach (Question question in abilityQuestions)
					{
						Answer correctAnswer = question.Answers.SingleOrDefault(x => x.Correct);
						if (correctAnswer == null)
						{
							_context.Remove(question);
							continue;
						}

						switch (question.Type)
						{
							case QuestionType.AbilityCooldown:
								RegenerateCooldownQuestionIfOutdated(question, ability);
								break;

							default:
								Console.WriteLine($"Unexpected question type of '{question.Type}' linked to ability {ability.Name} ({ability.Id}).");
								break;
						}
					}
				}
				else
				{
					_context.Questions.Add(GenerateNewCooldownQuestion(ability));
				}
			}

			await _context.SaveChangesAsync();
		}

		/// <summary>
		/// Regenerates the <see cref="Question"/> and the <see cref="Answer"/>s if the correct <see cref="Answer"/> is
		/// no longer the same.
		/// </summary>
		private void RegenerateCooldownQuestionIfOutdated(Question question, Ability ability)
		{
			Answer correctAnswer = question.Answers.Single(x => x.Correct);
			if (correctAnswer.Text == ability.Cooldown)
			{
				// Ability hasn't changed the cooldown so no need to regenerate the question
				return;
			}

			_context.RemoveRange(question.Answers);

			question.Answers.Add(new Answer()
			{
				Correct = true,
				QuestionId = question.Id,
				Text = ability.Cooldown
			});

			int slashes = ability.Cooldown.Count(x => x == '/');

			// Generate one of each type of incorrect answer (x, x/x/x, x/x/x/x)
			question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, 0));
			question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, 2));
			question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, 3));

			// Generate extra incorrect answers of the same type as the correct answer to increase the difficulty
			int incorrectAnswersOfSameType = Math.Max(_questionSettings.IncorrectAnswersGenerated, 3) - 3;
			for (int i = 0; i < incorrectAnswersOfSameType; i++)
			{
				question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, slashes));
			}

			question.Text = $"What is the cooldown of {ability.Name}?";
		}

		/// <summary>
		/// Generates a new <see cref="Question"/> and a variety of answers about the cooldown of the given <see cref="Ability"/>.
		/// </summary>
		/// <param name="ability"></param>
		/// <returns></returns>
		private Question GenerateNewCooldownQuestion(Ability ability)
		{
			var regex = new Regex(@"[\s'\-,.!?\(\)]+");
			string abilityKey = $"{regex.Replace(ability.Hero.Name, string.Empty)}-{regex.Replace(ability.Name, string.Empty)}-cooldown".ToLowerInvariant();

			Question question = new Question()
			{
				Type = QuestionType.AbilityCooldown,
				Text = $"What is the cooldown of {ability.Name}?",
				AbilityId = ability.Id,
				Key = abilityKey,
				Answers = new List<Answer>()
			};

			question.Answers.Add(new Answer()
			{
				Correct = true,
				Text = ability.Cooldown
			});

			question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, 0));
			question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, 2));
			question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, 3));
			
			int slashes = ability.Cooldown.Count(x => x == '/');

			// Generate extra incorrect answers of the same type as the correct answer to increase the difficulty
			int incorrectAnswersOfSameType = Math.Max(_questionSettings.IncorrectAnswersGenerated, 3) - 3;
			for (int i = 0; i < incorrectAnswersOfSameType; i++)
			{
				question.Answers.Add(GenerateIncorrectCooldownAnswer(ability, question, slashes));
			}

			return question;
		}

		/// <summary>
		/// Generates a new incorrect answer for the cooldown of the given <see cref="Ability"/>.
		/// </summary>
		/// <param name="ability">The <see cref="Ability"/> to generate an incorrect cooldown for.</param>
		/// <param name="slashes">The number of forward slashes the incorrect cooldown should contain.</param>
		private Answer GenerateIncorrectCooldownAnswer(Ability ability, Question question, int slashes)
		{
			const int MaximumAttempts = 10;
			for (int attemptNumber = 0; attemptNumber < MaximumAttempts; attemptNumber++)
			{
				decimal minimumValue, maximumValue;

				if (ability.Cooldown.Contains('/'))
				{
					var cooldownParts = ability.Cooldown.Split('/').Select(x => x.Trim());
					if (!decimal.TryParse(cooldownParts.First(), out decimal firstValue) || !decimal.TryParse(cooldownParts.Last(), out decimal lastValue))
					{
						return null;
					}

					minimumValue = Math.Min(firstValue, lastValue);
					maximumValue = Math.Max(firstValue, lastValue);
				}
				else
				{
					if (!decimal.TryParse(ability.Cooldown.Trim(), out decimal value))
					{
						return null;
					}

					minimumValue = value;
					maximumValue = value;
				}

				bool isWholeNumber = (minimumValue % 1) == 0;
				if (!isWholeNumber)
				{
					// Generate the values as if the cooldown is 10x larger to better handle decimals
					minimumValue *= 10;
					maximumValue *= 10;
				}

				int startingValueJitter = (int)minimumValue switch
				{
					int val when minimumValue <= 5 => ChooseRandomNumberBetween(-1 * val, 6),
					_ when minimumValue <= 12 => ChooseRandomNumberBetween(-3, 8),
					_ when minimumValue <= 20 => ChooseRandomNumberBetween(-7, 10),
					_ when minimumValue <= 40 => ChooseRandomNumberBetween(-10, 12),
					_ when minimumValue <= 80 => ChooseRandomNumberBetween(-15, 20),
					_ => ChooseRandomNumberBetween(-20, 25)
				};

				decimal startingValue = minimumValue + startingValueJitter;
				string answerText;

				if (slashes == 0)
				{
					answerText = isWholeNumber ? startingValue.ToString() : (startingValue / 10).ToString();
				}
				else
				{
					decimal valueDifference = maximumValue - minimumValue;

					int interval = startingValue switch
					{
						_ when minimumValue <= 5 => ChooseRandomOption(1, 1, 1, 2, 2, 3, 4, 5),
						_ when minimumValue <= 15 => ChooseRandomOption(1, 1, 2, 2, 3, 4, 4, 5, 8, 10),
						_ when minimumValue <= 30 => ChooseRandomOption(1, 2, 2, 3, 4, 5, 6, 8, 10, 12),
						_ when minimumValue <= 60 && (slashes == 2 || valueDifference > 25) => ChooseRandomOption(8, 10, 12, 15, 20, 25, 30, 35, 40, 45),
						_ when minimumValue <= 60 => ChooseRandomOption(2, 4, 5, 6, 8, 10, 12, 15, 20, 25, 30),
						_ when minimumValue <= 60 => ChooseRandomOption(1, 5),
						_ when minimumValue <= 60 => ChooseRandomOption(1, 5),
						_ when minimumValue <= 60 => ChooseRandomOption(1, 5),
						_ when minimumValue <= 60 => ChooseRandomOption(1, 5),
						_ when minimumValue % 5 != 0 || maximumValue % 5 != 0 => ChooseRandomOption(2, 4, 5, 6, 8, 10, 10, 12, 15, 16, 20, 20, 25, 30, 40),
						_ when valueDifference < slashes * 10 => ChooseRandomOption(5, 5, 10, 10, 15, 15, 20, 25, 30),
						_ => ChooseRandomOption(5, 10, 10, 10, 15, 15, 20, 20, 20, 25, 25, 30, 30, 40, 50)
					};

					var values = new List<decimal>();
					for (int i = 0; i < slashes + 1; i++)
					{
						values.Add(startingValue + interval * (i + 1));
					}

					if (_random.NextDouble() < 0.9)
					{
						// Has a 90% chance to reverse, so that the cooldown would decrease as you level up
						values.Reverse();
					}

					if (!isWholeNumber)
					{
						values = values.Select(x => x / 10).ToList();
					}

					answerText = string.Join('/', values);
				}

				if (answerText == ability.Cooldown)
				{
					continue;
				}

				return new Answer()
				{
					Correct = false,
					QuestionId = question.Id,
					Text = answerText
				};
			}

			throw new InvalidOperationException($"Failed to generate an incorrect cooldown for ability '{ability.Name}' within {MaximumAttempts} attempts.");
		}

		/// <summary>
		/// Gets all the questions that relate to the ability with the given ID.
		/// </summary>
		private async Task<IEnumerable<Question>> GetQuestionsForAbility(int abilityId)
		{
			return await _context.Questions
				.Include(x => x.Answers)
				.Where(x => x.AbilityId == abilityId)
				.ToListAsync();
		}

		/// <summary>
		/// Chooses a random option from the given set of options and returns it.
		/// </summary>
		/// <param name="options">The options to choose from.</param>
		/// <returns>One option at random.</returns>
		private T ChooseRandomOption<T>(params T[] options)
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
		/// <param name="options">The options to choose from.</param>
		/// <returns>One option at random.</returns>
		private int ChooseRandomNumberBetween(int min, int max)
		{
			if (min > max)
			{
				throw new ArgumentException("The minimum vale cannot be greater than the maximum value.");
			}

			return ChooseRandomOption(Enumerable.Range(min, max - min).ToArray());
		}
	}
}
