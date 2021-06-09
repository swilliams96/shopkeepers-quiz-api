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
	public class ManaCostQuestionGenerator : IQuestionGenerator
	{
		private readonly IQuestionRepository _questionRepository;
		private readonly IHeroRepository _heroRepository;
		private readonly QuestionSettings _questionSettings;
		private readonly RandomHelper _randomHelper;
		private readonly ILogger _logger;

		private readonly Random _random = new Random();

		public ManaCostQuestionGenerator(
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
			_logger = logger.ForContext<ManaCostQuestionGenerator>();
		}

		public async Task GenerateQuestions()
		{
			IEnumerable<Hero> heroes = await _heroRepository.GetAllHeroes();

			foreach (Hero hero in heroes)
			{
				foreach (Ability ability in hero.Abilities)
				{
					IEnumerable<Question> abilityQuestions = await _questionRepository.GetQuestionsForAbility(ability.Id);
					IEnumerable<Question> manaCostQuestionsForAbility = abilityQuestions.Where(x => x.Type == QuestionType.AbilityManaCost);

					if (manaCostQuestionsForAbility.Any())
					{
						foreach (Question question in manaCostQuestionsForAbility)
						{
							Answer correctAnswer = question.Answers.SingleOrDefault(x => x.Correct);
							if (correctAnswer == null)
							{
								await _questionRepository.DeleteQuestion(question.Id);
								continue;
							}

							RebuildManaCostQuestionIfOutdated(question, ability);

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
						Question question = BuildNewManaCostQuestion(ability, hero.Name);

						if (question.Answers.Any(x => string.IsNullOrWhiteSpace(x?.Text)))
						{
							continue;
						}

						await _questionRepository.CreateQuestion(question);
					}
				}
			}
		}

		/// <summary>
		/// Regenerates the <see cref="Question"/> and the <see cref="Answer"/>s if the correct <see cref="Answer"/> is no longer the same.
		/// </summary>
		private void RebuildManaCostQuestionIfOutdated(Question question, Ability ability)
		{
			Answer correctAnswer = question.Answers.SingleOrDefault(x => x.Correct);
			if (correctAnswer?.Text == ability.ManaCost)
			{
				// Ability hasn't changed the mana cost so no need to regenerate the question
				return;
			}

			question.Answers = new List<Answer>()
			{
				new Answer(ability.ManaCost, true)
			};

			// Generate one of each type of incorrect answer (x, x/x/x, x/x/x/x)
			question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, 0));
			question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, 2));
			question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, 3));

			// Generate extra incorrect answers of the same type as the correct answer to increase the difficulty
			int slashes = ability.ManaCost.Count(x => x == '/');
			int incorrectAnswersOfSameType = Math.Max(_questionSettings.IncorrectAnswersGenerated, 3) - 3;
			for (int i = 0; i < incorrectAnswersOfSameType; i++)
			{
				question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, slashes));
			}

			question.Text = $"What is the mana cost of {ability.Name}?";
		}

		/// <summary>
		/// Generates a new <see cref="Question"/> and a variety of answers about the mana cost of the given <see cref="Ability"/>.
		/// </summary>
		private Question BuildNewManaCostQuestion(Ability ability, string heroName)
		{
			var regex = new Regex(@"[\s'\-,.!?\(\)]+");
			string questionKey = $"{regex.Replace(heroName, string.Empty)}-{regex.Replace(ability.Name, string.Empty)}-manacost".ToLowerInvariant();

			Question question = new Question(
				type: QuestionType.AbilityManaCost,
				text: $"What is the mana cost of {ability.Name}?",
				ability: ability,
				key: questionKey);

			question.Answers.Add(new Answer(ability.ManaCost, true));

			// Generate one of each type of incorrect answer (x, x/x/x, x/x/x/x)
			question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, 0));
			question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, 2));
			question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, 3));
			
			// Generate extra incorrect answers of the same type as the correct answer to increase the difficulty
			int slashes = ability.ManaCost.Count(x => x == '/');
			int incorrectAnswersOfSameType = Math.Max(_questionSettings.IncorrectAnswersGenerated, 3) - 3;
			for (int i = 0; i < incorrectAnswersOfSameType; i++)
			{
				question.Answers.Add(BuildIncorrectManaCostAnswer(ability, question, slashes));
			}

			return question;
		}

		/// <summary>
		/// Generates a new incorrect answer for the mana cost of the given <see cref="Ability"/>.
		/// </summary>
		/// <param name="ability">The <see cref="Ability"/> to generate an incorrect mana cost for.</param>
		/// <param name="question">The <see cref="Question"/> that the answer will belong to.</param>
		/// <param name="slashes">The number of forward slashes the incorrect mana cost should contain.</param>
		private Answer BuildIncorrectManaCostAnswer(Ability ability, Question question, int slashes)
		{
			const int MaximumAttempts = 10;
			for (int attemptNumber = 0; attemptNumber < MaximumAttempts; attemptNumber++)
			{
				decimal minimumValue, maximumValue;
				int manaCostPartsCount = 1;

				if (ability.ManaCost.Contains('/'))
				{
					var manaCostParts = ability.ManaCost.Split('/').Select(x => x.Trim());
					manaCostPartsCount = manaCostParts.Count();

					if (!decimal.TryParse(manaCostParts.First(), out decimal firstValue) || !decimal.TryParse(manaCostParts.Last(), out decimal lastValue))
					{
						return null;
					}

					minimumValue = Math.Min(firstValue, lastValue);
					maximumValue = Math.Max(firstValue, lastValue);
				}
				else
				{
					if (!decimal.TryParse(ability.ManaCost.Trim(), out decimal value))
					{
						return null;
					}

					minimumValue = value;
					maximumValue = value;
				}

				int startingValueOffset = (int)minimumValue switch
				{
					int val when minimumValue <= 10 => _randomHelper.ChooseRandomNumberBetween(-1 * val, 10),
					int val when minimumValue <= 20 => _randomHelper.ChooseRandomNumberBetween(-5, 10) * 2,
					int valBy5 when minimumValue % 5 == 0 => valBy5 switch
					{
						_ when minimumValue <= 50 => _randomHelper.ChooseRandomNumberBetween(-4, 6) * 5,
						_ when minimumValue <= 90 => _randomHelper.ChooseRandomNumberBetween(-8, 12) * 5,
						_ when minimumValue <= 150 => _randomHelper.ChooseRandomNumberBetween(-12, 10) * 5,
						int valBy25 when minimumValue % 25 == 0 => valBy25 switch
						{
							_ when minimumValue <= 550 => _randomHelper.ChooseRandomNumberBetween(-6, 6) * 25,
							_ => _randomHelper.ChooseRandomNumberBetween(5, 5) * 50,
						},
						_ when minimumValue <= 250 => _randomHelper.ChooseRandomNumberBetween(-18, 12) * 5,
						int valBy10 when minimumValue % 10 == 0 => valBy10 switch
						{
							_ when minimumValue <= 550 => _randomHelper.ChooseRandomNumberBetween(-15, 10) * 10,
							_ => _randomHelper.ChooseRandomNumberBetween(-25, 20) * 10,
						},
						_ => _randomHelper.ChooseRandomNumberBetween(-25, 25) * 5
					},
					_ when minimumValue <= 60 => _randomHelper.ChooseRandomNumberBetween(-7, 20) * 2,
					_ when minimumValue <= 150 => _randomHelper.ChooseRandomNumberBetween(-5, 5) * 10,
					_ => _randomHelper.ChooseRandomNumberBetween(-10, 15) * 10
				};

				decimal startingValue = minimumValue + startingValueOffset;
				string answerText;

				if (slashes == 0)
				{
					answerText = startingValue.ToString();
				}
				else
				{
					decimal correctAnswerMinMaxDifference = maximumValue - minimumValue;
					decimal correctAnswerInterval = correctAnswerMinMaxDifference / manaCostPartsCount;

					int interval = startingValue switch
					{
						_ when minimumValue <= 20 => _randomHelper.ChooseRandomOption(4, 5, 5, 5),
						_ when minimumValue <= 90 => _randomHelper.ChooseRandomOption(5, 5, 5, 5, 10, 10, 10, 10, 15, 15, 15, 20, 20, 25, 25, 30, 50, 60, 75),
						_ when minimumValue <= 180 && (correctAnswerInterval <= 40) => _randomHelper.ChooseRandomOption(5, 5, 10, 10, 15, 15, 20, 20, 25, 25, 30, 30, 35, 40),
						_ when minimumValue <= 180 => _randomHelper.ChooseRandomOption(50, 50, 60, 60, 75, 75, 100, 125, 150, 200),
						_ => _randomHelper.ChooseRandomOption(50, 75, 100, 100, 100, 110, 125, 125, 150, 150, 150, 175, 200, 220, 225, 250)
					};

					var values = new List<decimal>();
					for (int i = 0; i < slashes + 1; i++)
					{
						values.Add(startingValue + interval * (i + 1));
					}

					if (_random.NextDouble() < 0.05)
					{
						// Has a 5% chance to reverse, so that the manacost would decrease as you level up
						values.Reverse();
					}

					answerText = string.Join('/', values);
				}

				if (answerText == ability.ManaCost)
				{
					continue;
				}

				return new Answer(answerText, false);
			}

			_logger.Warning("Failed to generate an incorrect cooldown for ability '{ability.Name}' within {MaximumAttempts} attempts.");
			return null;
		}
	}
}
