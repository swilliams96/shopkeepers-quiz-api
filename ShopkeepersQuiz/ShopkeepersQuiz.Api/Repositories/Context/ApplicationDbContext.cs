﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.Questions;

namespace ShopkeepersQuiz.Api.Repositories.Context
{
	public class ApplicationDbContext : DbContext
	{
		private readonly ConnectionStrings _connectionStrings;

		public ApplicationDbContext(IOptions<ConnectionStrings> connectionStrings)
		{
			_connectionStrings = connectionStrings.Value;
		}

		public DbSet<Question> Questions { get; set; }
		public DbSet<Answer> Answers { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			options.UseSqlServer(_connectionStrings.ApplicationDatabase);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Question>(question =>
			{
				question.HasMany(x => x.Answers)
					.WithOne()
					.HasForeignKey(x => x.QuestionId);

				question.HasIndex(x => x.Key)
					.IsUnique();
			});
		}
	}
}
