﻿using MongoDB.Bson.Serialization.Attributes;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.GameEntities;
using System;
using System.Collections.Generic;

namespace ShopkeepersQuiz.Api.Models.Questions
{
	public class Question
	{
		public Question(string key, string text, QuestionType type, Ability ability)
		{
			Id = Guid.NewGuid();
			Key = key;
			Text = text;
			Type = type;
			Ability = ability;
		}

		[BsonId]
		public Guid Id { get; set; }

		public string Key { get; set; }

		public string Text { get; set; }

		public QuestionType Type { get; set; }

		public ICollection<Answer> Answers { get; set; } = new List<Answer>();

		public Ability Ability { get; set; }
	}
}
