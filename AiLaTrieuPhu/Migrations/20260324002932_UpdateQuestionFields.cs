using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiLaTrieuPhu.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Hint",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Hint",
                table: "Questions");
        }
    }
}
