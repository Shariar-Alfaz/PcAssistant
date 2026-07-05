using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PcAssistant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageAutomationScheduledTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppDisplayName",
                table: "scheduled_tasks",
                type: "TEXT",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppPath",
                table: "scheduled_tasks",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageText",
                table: "scheduled_tasks",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientNames",
                table: "scheduled_tasks",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatDaysOfWeek",
                table: "scheduled_tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RepeatMode",
                table: "scheduled_tasks",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "none");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppDisplayName",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "AppPath",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "MessageText",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "RecipientNames",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "RepeatDaysOfWeek",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "RepeatMode",
                table: "scheduled_tasks");
        }
    }
}
