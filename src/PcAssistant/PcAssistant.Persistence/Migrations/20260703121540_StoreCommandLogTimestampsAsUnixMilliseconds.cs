using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PcAssistant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreCommandLogTimestampsAsUnixMilliseconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_command_logs_CreatedAtUtc",
                table: "command_logs");

            migrationBuilder.AddColumn<long>(
                name: "CreatedAtUtcUnix",
                table: "command_logs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ExecutedAtUtcUnix",
                table: "command_logs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE command_logs
                SET
                    CreatedAtUtcUnix = CASE
                        WHEN typeof(CreatedAtUtc) = 'integer' THEN CreatedAtUtc
                        ELSE COALESCE(CAST(strftime('%s', CreatedAtUtc) AS INTEGER) * 1000, 0)
                    END,
                    ExecutedAtUtcUnix = CASE
                        WHEN ExecutedAtUtc IS NULL THEN NULL
                        WHEN typeof(ExecutedAtUtc) = 'integer' THEN ExecutedAtUtc
                        ELSE CAST(strftime('%s', ExecutedAtUtc) AS INTEGER) * 1000
                    END
                """);

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "command_logs");

            migrationBuilder.DropColumn(
                name: "ExecutedAtUtc",
                table: "command_logs");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtcUnix",
                table: "command_logs",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ExecutedAtUtcUnix",
                table: "command_logs",
                newName: "ExecutedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_command_logs_CreatedAtUtc",
                table: "command_logs",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_command_logs_CreatedAtUtc",
                table: "command_logs");

            migrationBuilder.AddColumn<string>(
                name: "CreatedAtUtcText",
                table: "command_logs",
                type: "TEXT",
                nullable: false,
                defaultValue: "1970-01-01 00:00:00+00:00");

            migrationBuilder.AddColumn<string>(
                name: "ExecutedAtUtcText",
                table: "command_logs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE command_logs
                SET
                    CreatedAtUtcText = strftime('%Y-%m-%d %H:%M:%f+00:00', CreatedAtUtc / 1000, 'unixepoch'),
                    ExecutedAtUtcText = CASE
                        WHEN ExecutedAtUtc IS NULL THEN NULL
                        ELSE strftime('%Y-%m-%d %H:%M:%f+00:00', ExecutedAtUtc / 1000, 'unixepoch')
                    END
                """);

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "command_logs");

            migrationBuilder.DropColumn(
                name: "ExecutedAtUtc",
                table: "command_logs");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtcText",
                table: "command_logs",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ExecutedAtUtcText",
                table: "command_logs",
                newName: "ExecutedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_command_logs_CreatedAtUtc",
                table: "command_logs",
                column: "CreatedAtUtc");
        }
    }
}
