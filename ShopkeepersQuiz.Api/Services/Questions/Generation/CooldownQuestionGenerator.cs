using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.GameEntities;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Repositories.Heroes;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Questions.Generation
{
	public class CooldownQuestionGenerator : IQuestionGenerator
	{
		private readonly IQuestionRepository _questionRepository;
		private readonly IHeroRepository _heroRepository;
		private readonly QuestionSettings _questionSettings;
		private readonly RandomHelper _randomHelper;
		private readonly ILogger _logger;

		private readonly Random _random = new Random();

		public CooldownQuestionGenerator(
			IQuestionRepository questionRepository,
			IHeroRepository heroRepository,
			IOptions<QuestionSettings> questionSettings,
			RandomHelper randomHelper,
			ILogger logger)
		{
			_questionRepository = questionRepository;
			_heroRepository = heroRepository;
			_questionSettings = questionSettings.Value;
			_randomHelper = randomHelper;
			_logger = logger.ForContext<CooldownQuestionGenerator>();
		}

		public async Task GenerateQuestions()
		{
			IEnumerable<Hero> heroes = await _heroRepository.GetAllHeroes();

			foreach (Ability ability in heroes.SelectMany(x => x.Abilities))
			{
				IEnumerable<Question> abilityQuestions = await _questionRepository.GetQuestionsForAbility(ability.Id);
				IEnumerable<Question> cooldownQuestionsForAbility = abilityQuestions.Where(x => x.Type == QuestionType.AbilityCooldown);

				if (cooldownQuestionsForAbility.Any())
				{
					foreach (Question question in cooldownQuestionsForAbility)
					{
						Answer correctAnswer = question.Answers.SingleOrDefault(x => x.Correct);
						if (correctAnswer == null)
						{
							await _questionRepository.DeleteQuestion(question.Id);
							continue;
						}

						RebuildCooldownQuestionIfOutdated(question, ability);

						if (question.Answers.Any(x => string.IsNullOrWhiteSpace(x.Text)))
						{
							await _questionRepository.DeleteQuestion(question.Id);
							continue;
						}

						await _questionRepository.UpdateQuestion(question);
					}
				}
				else
				{
					Question question = BuildNewCooldownQuestion(ability);

					if (question.Answers.Any(x => string.IsNullOrWhiteSpace(x?.Text)))
					{
						continue;
					}

					await _questionRepository.CreateQuestion(question);
				}
			}
		}

		/// <summary>
		/// Regenerates the <see cref="Question"/> and the <see cref="Answer"/>s if the correct <see cref="Answer"/> is no longer the same.
		/// </summary>
		private void RebuildCooldownQuestionIfOutdated(Question question, Ability ability)
		{
			Answer correctAnswer = question.Answers.Single(x => x.Correct);
			if (correctAnswer.Text == ability.Cooldown)
			{
				// Ability hasn't changed the cooldown so no need to regenerate the question
				return;
			}

			question.Answers = new List<Answer>();

			question.Answers.Add(new Answer()
			{
				Correct = true,
				QuestionId = question.Id,
				Text = ability.Cooldown
			});

			int slashes = ability.Cooldown.Count(x => x == '/');

			// Generate one of each type of incorrect answer (x, x/x/x, x/x/x/x)
			question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, 0));
			question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, 2));
			question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, 3));

			// Generate extra incorrect answers of the same type as the correct answer to increase the difficulty
			int incorrectAnswersOfSameType = Math.Max(_questionSettings.IncorrectAnswersGenerated, 3) - 3;
			for (int i = 0; i < incorrectAnswersOfSameType; i++)
			{
				question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, slashes));
			}

			question.Text = $"What is the cooldown of {ability.Name}?";
		}

		/// <summary>
		/// Generates a new <see cref="Question"/> and a variety of answers about the cooldown of the given <see cref="Ability"/>.
		/// </summary>
		/// <param name="ability"></param>
		/// <returns></returns>
		private Question BuildNewCooldownQuestion(Ability ability)
		{
			var regex = new Regex(@"[\s'\-,.!?\(\)]+");
			string abilityKey = $"{regex.Replace(ability.Hero.Name, string.Empty)}-{regex.Replace(ability.Name, string.Empty)}-cooldown".ToLowerInvariant();

			Question question = new Question()
			{
				Type = QuestionType.AbilityCooldown,
				Text = $"What is the cooldown of {ability.Name}?",
				Ability = ability,
				Key = abilityKey,
				Answers = new List<Answer>()
			};

			question.Answers.Add(new Answer()
			{
				Correct = true,
				Text = ability.Cooldown
			});

			question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, 0));
			question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, 2));
			question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, 3));
			
			int slashes = ability.Cooldown.Count(x => x == '/');

			// Generate extra incorrect answers of the same type as the correct answer to increase the difficulty
			int incorrectAnswersOfSameType = Math.Max(_questionSettings.IncorrectAnswersGenerated, 3) - 3;
			for (int i = 0; i < incorrectAnswersOfSameType; i++)
			{
				question.Answers.Add(BuildIncorrectCooldownAnswer(ability, question, slashes));
			}

			return question;
		}

		/// <summary>
		/// Generates a new incorrect answer for the cooldown of the given <see cref="Ability"/>.
		/// </summary>
		/// <param name="ability">The <see cref="Ability"/> to generate an incorrect cooldown for.</param>
		/// <param name="question">The <see cref="Question"/> that the answer will belong to.</param>
		/// <param name="slashes">The number of forward slashes the incorrect cooldown should contain.</param>
		private Answer BuildIncorrectCooldownAnswer(Ability ability, Question question, int slashes)
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

				int startingValueOffset = (int)minimumValue switch
				{
					int val when minimumValue <= 5 => _randomHelper.ChooseRandomNumberBetween(-1 * val + 1, 6),
					_ when minimumValue <= 12 => _randomHelper.ChooseRandomNumberBetween(-3, 8),
					_ when minimumValue % 5 == 0 && minimumValue <= 30 => _randomHelper.ChooseRandomNumberBetween(-2, 2) * 5,
					_ when minimumValue % 5 == 0 && minimumValue <= 50 => _randomHelper.ChooseRandomNumberBetween(-4, 3) * 5,
					_ when minimumValue % 5 == 0 && minimumValue <= 80 => _randomHelper.ChooseRandomNumberBetween(-6, 4) * 5,
					_ when minimumValue % 2 == 0 && minimumValue <= 30 => _randomHelper.ChooseRandomNumberBetween(-4, 7) * 2,
					_ when minimumValue % 2 == 0 && minimumValue <= 60 => _randomHelper.ChooseRandomNumberBetween(-5, 10) * 2,
					_ when minimumValue <= 20 => _randomHelper.ChooseRandomNumberBetween(-3, 5) * 2,
					_ when minimumValue <= 40 => _randomHelper.ChooseRandomNumberBetween(-5, 7) * 2,
					_ => _randomHelper.ChooseRandomNumberBetween(-10, 10) * 2
				};

				decimal startingValue = minimumValue + startingValueOffset;
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
						_ when minimumValue <= 5 => _randomHelper.ChooseRandomOption(1, 1, 1, 2, 2, 3, 4, 5),
						_ when minimumValue <= 15 => _randomHelper.ChooseRandomOption(1, 1, 2, 2, 3, 4, 4, 5, 8, 10),
						_ when minimumValue <= 30 => _randomHelper.ChooseRandomOption(1, 2, 3, 4, 5, 6, 7, 8, 10, 12),
						_ when minimumValue <= 60 && (slashes == 2 || valueDifference > 25) => _randomHelper.ChooseRandomOption(8, 10, 12, 15, 20, 25, 30, 35, 40, 45),
						_ when minimumValue <= 60 => _randomHelper.ChooseRandomOption(2, 4, 5, 6, 8, 10, 12, 15, 20, 25, 30),
						_ when minimumValue % 5 != 0 || maximumValue % 5 != 0 => _randomHelper.ChooseRandomOption(2, 4, 5, 6, 8, 10, 10, 12, 15, 16, 20, 20, 25, 30, 40),
						_ when valueDifference < slashes * 10 => _randomHelper.ChooseRandomOption(5, 5, 10, 10, 15, 15, 20, 25, 30),
						_ => _randomHelper.ChooseRandomOption(5, 10, 10, 10, 15, 15, 20, 20, 20, 25, 25, 30, 30, 40, 50)
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
	}
}
