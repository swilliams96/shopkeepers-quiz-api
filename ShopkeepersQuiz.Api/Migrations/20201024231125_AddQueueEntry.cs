using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ShopkeepersQuiz.Api.Migrations
{
    public partial class AddQueueEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueueEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    QuestionId = table.Column<int>(nullable: false),
                    StartTimeUtc = table.Column<DateTime>(nullable: false),
                    EndTimeUtc = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueEntries_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QuestionId",
                table: "QueueEntries",
                column: "QuestionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueEntries");
        }
    }
}
