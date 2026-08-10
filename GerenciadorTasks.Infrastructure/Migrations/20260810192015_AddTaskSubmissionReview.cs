using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskSubmissionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewerComment",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionImageUrl",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewerComment",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SubmissionImageUrl",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Tasks");
        }
    }
}
