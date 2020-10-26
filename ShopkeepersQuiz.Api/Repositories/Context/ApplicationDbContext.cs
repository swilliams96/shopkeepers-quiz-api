using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.GameEntities;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Models.Queue;
using System;
using System.Linq;

namespace ShopkeepersQuiz.Api.Repositories.Context
{
	public class ApplicationDbContext : DbContext
	{
		private readonly ConnectionStrings _connectionStrings;

		private readonly ValueConverter<int[], string> _intArrayConverter = new ValueConverter<int[], string>(
				array => string.Join(",", array),
				str => str.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(value => int.Parse(value)).ToArray());

		public ApplicationDbContext(IOptions<ConnectionStrings> connectionStrings)
		{
			_connectionStrings = connectionStrings.Value;
		}

		public DbSet<QueueEntry> QueueEntries { get; set; }
		public DbSet<Question> Questions { get; set; }
		public DbSet<Answer> Answers { get; set; }
		public DbSet<Hero> Heroes { get; set; }
		public DbSet<Ability> Abilities { get; set; }
		public DbSet<Item> Items{ get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			options.UseSqlServer(_connectionStrings.ApplicationDatabase);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<QueueEntry>(entry =>
			{
				entry.HasOne(x => x.Question)
					.WithMany()
					.HasForeignKey(x => x.QuestionId);

				entry.Property(x => x.IncorrectAnswerIds)
					.HasConversion(_intArrayConverter);
			});

			modelBuilder.Entity<Question>(question =>
			{
				question.HasMany(x => x.Answers)
					.WithOne()
					.HasForeignKey(x => x.QuestionId);

				question.HasIndex(x => x.Key)
					.IsUnique();
			});

			modelBuilder.Entity<Hero>()
				.HasMany(x => x.Abilities)
				.WithOne(x => x.Hero)
				.HasForeignKey(x => x.HeroId);
		}
	}
}
