using Microsoft.EntityFrameworkCore.Migrations;

namespace ShopkeepersQuiz.Api.Migrations
{
    public partial class AddAbilityIdToQuestion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbilityId",
                table: "Questions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AbilityId",
                table: "Questions",
                column: "AbilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Abilities_AbilityId",
                table: "Questions",
                column: "AbilityId",
                principalTable: "Abilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Abilities_AbilityId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_AbilityId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AbilityId",
                table: "Questions");
        }
    }
}
