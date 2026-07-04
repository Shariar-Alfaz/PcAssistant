using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PcAssistant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var legacyChatSessionId = new Guid("11111111-1111-1111-1111-111111111111");
            var migratedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            migrationBuilder.AddColumn<Guid>(
                name: "ChatSessionId",
                table: "command_logs",
                type: "TEXT",
                nullable: false,
                defaultValue: legacyChatSessionId);

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.Id);
                });

            migrationBuilder.Sql(
                $"""
                INSERT INTO chat_sessions (Id, Title, CreatedAtUtc, UpdatedAtUtc)
                SELECT '{legacyChatSessionId}', 'Previous commands', {migratedAt}, {migratedAt}
                WHERE EXISTS (SELECT 1 FROM command_logs);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_command_logs_ChatSessionId",
                table: "command_logs",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_UpdatedAtUtc",
                table: "chat_sessions",
                column: "UpdatedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_command_logs_chat_sessions_ChatSessionId",
                table: "command_logs",
                column: "ChatSessionId",
                principalTable: "chat_sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_command_logs_chat_sessions_ChatSessionId",
                table: "command_logs");

            migrationBuilder.DropTable(
                name: "chat_sessions");

            migrationBuilder.DropIndex(
                name: "IX_command_logs_ChatSessionId",
                table: "command_logs");

            migrationBuilder.DropColumn(
                name: "ChatSessionId",
                table: "command_logs");
        }
    }
}
