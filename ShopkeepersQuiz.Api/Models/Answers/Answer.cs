using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopkeepersQuiz.Api.Models.Answers
{
	public class Answer
	{
		[BsonId]
		public Guid Id { get; set; }

		public Guid QuestionId { get; set; }

		[Required]
		public string Text { get; set; }

		public bool Correct { get; set; }
	}
}
