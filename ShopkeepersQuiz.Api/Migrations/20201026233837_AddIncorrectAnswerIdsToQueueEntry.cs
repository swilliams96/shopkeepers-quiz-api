using Microsoft.EntityFrameworkCore.Migrations;

namespace ShopkeepersQuiz.Api.Migrations
{
    public partial class AddIncorrectAnswerIdsToQueueEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IncorrectAnswerIds",
                table: "QueueEntries",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncorrectAnswerIds",
                table: "QueueEntries");
        }
    }
}
