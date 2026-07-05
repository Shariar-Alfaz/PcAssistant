using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PcAssistant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CommandLogId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TaskType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    QueueStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    QueuePosition = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledForUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    LastMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_tasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_tasks_CommandLogId",
                table: "scheduled_tasks",
                column: "CommandLogId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_tasks_QueueStatus_ScheduledForUtc",
                table: "scheduled_tasks",
                columns: new[] { "QueueStatus", "ScheduledForUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_tasks");
        }
    }
}
