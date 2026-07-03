using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PcAssistant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommandHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "command_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserText = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CommandLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RawPredictedLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    RequiresConfirmation = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssistantMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Preview = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ResolvedPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsDangerous = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExecutionStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ExecutionMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExecutedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_command_logs_CreatedAtUtc",
                table: "command_logs",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_logs");
        }
    }
}
