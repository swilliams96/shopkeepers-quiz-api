using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopkeepersQuiz.Api.Models.Answers
{
	public class Answer
	{
		public Answer(string text, bool correct)
		{
			Id = Guid.NewGuid();
			Text = text;
			Correct = correct;
		}

		[BsonId]
		public Guid Id { get; set; }

		[Required]
		public string Text { get; set; }

		public bool Correct { get; set; }
	}
}
