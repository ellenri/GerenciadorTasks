using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciadorTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChildUserLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentUserId",
                table: "Children",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Children",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Crianças do modelo antigo não tinham vínculo (UserId/ParentUserId = vazio).
            // Removemos esses registros órfãos para que o índice único em UserId seja
            // viável; o SeedData recria as crianças já vinculadas aos seus logins.
            // Em banco vazio é um no-op.
            migrationBuilder.Sql("DELETE FROM Children;");

            migrationBuilder.CreateIndex(
                name: "IX_Children_ParentUserId",
                table: "Children",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_UserId",
                table: "Children",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Children_ParentUserId",
                table: "Children");

            migrationBuilder.DropIndex(
                name: "IX_Children_UserId",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "ParentUserId",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Children");
        }
    }
}
