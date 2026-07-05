using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PcAssistant.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebAutomationBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "browser_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    UserDataDirectory = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_browser_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "web_automation_projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    StartUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_automation_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "web_automation_flows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    StartUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Headless = table.Column<bool>(type: "INTEGER", nullable: false),
                    BrowserChannel = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UsePersistentSession = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultTimeoutMs = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_automation_flows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_web_automation_flows_web_automation_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "web_automation_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "web_automation_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FlowId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    BrowserChannel = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Headless = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_automation_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_web_automation_runs_web_automation_flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "web_automation_flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "web_automation_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FlowId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StepType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SelectorType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Selector = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TimeoutMs = table.Column<int>(type: "INTEGER", nullable: false),
                    DelayAfterMs = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: false),
                    TakeScreenshotAfterStep = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_automation_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_web_automation_steps_web_automation_flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "web_automation_flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "web_automation_variables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FlowId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_automation_variables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_web_automation_variables_web_automation_flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "web_automation_flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "web_automation_run_step_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ScreenshotPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    ExtractedValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_automation_run_step_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_web_automation_run_step_logs_web_automation_runs_RunId",
                        column: x => x.RunId,
                        principalTable: "web_automation_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_web_automation_run_step_logs_web_automation_steps_StepId",
                        column: x => x.StepId,
                        principalTable: "web_automation_steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "web_selector_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CssSelector = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    XPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    AccessibleName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    InnerText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    HtmlSnippet = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    X = table.Column<double>(type: "REAL", nullable: true),
                    Y = table.Column<double>(type: "REAL", nullable: true),
                    Width = table.Column<double>(type: "REAL", nullable: true),
                    Height = table.Column<double>(type: "REAL", nullable: true),
                    ScreenshotPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_selector_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_web_selector_snapshots_web_automation_steps_StepId",
                        column: x => x.StepId,
                        principalTable: "web_automation_steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_web_automation_flows_ProjectId",
                table: "web_automation_flows",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_web_automation_projects_Name",
                table: "web_automation_projects",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_web_automation_run_step_logs_RunId",
                table: "web_automation_run_step_logs",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_web_automation_run_step_logs_StepId",
                table: "web_automation_run_step_logs",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_web_automation_runs_FlowId",
                table: "web_automation_runs",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_web_automation_steps_FlowId_OrderIndex",
                table: "web_automation_steps",
                columns: new[] { "FlowId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_web_automation_variables_FlowId_Key",
                table: "web_automation_variables",
                columns: new[] { "FlowId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_web_selector_snapshots_StepId",
                table: "web_selector_snapshots",
                column: "StepId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "browser_profiles");

            migrationBuilder.DropTable(
                name: "web_automation_run_step_logs");

            migrationBuilder.DropTable(
                name: "web_automation_variables");

            migrationBuilder.DropTable(
                name: "web_selector_snapshots");

            migrationBuilder.DropTable(
                name: "web_automation_runs");

            migrationBuilder.DropTable(
                name: "web_automation_steps");

            migrationBuilder.DropTable(
                name: "web_automation_flows");

            migrationBuilder.DropTable(
                name: "web_automation_projects");
        }
    }
}
