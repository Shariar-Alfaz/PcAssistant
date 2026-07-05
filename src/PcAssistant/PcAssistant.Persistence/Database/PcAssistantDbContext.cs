using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PcAssistant.Domain.Entity;

namespace PcAssistant.Persistence.Database;

public sealed class PcAssistantDbContext(DbContextOptions<PcAssistantDbContext> options) : DbContext(options), IPcAssistantDbContext
{
    private static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetToUnixMilliseconds =
        new(
            value => value.ToUniversalTime().ToUnixTimeMilliseconds(),
            value => DateTimeOffset.FromUnixTimeMilliseconds(value));

    private static readonly ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetToUnixMilliseconds =
        new(
            value => value.HasValue ? value.Value.ToUniversalTime().ToUnixTimeMilliseconds() : null,
            value => value.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : null);

    public DbSet<CommandLogEntry> CommandLogs => Set<CommandLogEntry>();

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();

    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();

    public DbSet<WebAutomationProject> WebAutomationProjects => Set<WebAutomationProject>();

    public DbSet<WebAutomationFlow> WebAutomationFlows => Set<WebAutomationFlow>();

    public DbSet<WebAutomationStep> WebAutomationSteps => Set<WebAutomationStep>();

    public DbSet<WebSelectorSnapshot> WebSelectorSnapshots => Set<WebSelectorSnapshot>();

    public DbSet<WebAutomationVariable> WebAutomationVariables => Set<WebAutomationVariable>();

    public DbSet<BrowserProfile> BrowserProfiles => Set<BrowserProfile>();

    public DbSet<WebAutomationRun> WebAutomationRuns => Set<WebAutomationRun>();

    public DbSet<WebAutomationRunStepLog> WebAutomationRunStepLogs => Set<WebAutomationRunStepLog>();

    public void ClearTrackedChanges()
    {
        ChangeTracker.Clear();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(session => session.Id);

            entity.Property(session => session.Id)
                .ValueGeneratedNever();

            entity.Property(session => session.Title)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(session => session.CreatedAtUtc)
                .HasConversion(DateTimeOffsetToUnixMilliseconds);

            entity.Property(session => session.UpdatedAtUtc)
                .HasConversion(DateTimeOffsetToUnixMilliseconds);

            entity.HasMany(session => session.CommandLogs)
                .WithOne()
                .HasForeignKey(command => command.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(session => session.UpdatedAtUtc);
        });

        modelBuilder.Entity<CommandLogEntry>(entity =>
        {
            entity.ToTable("command_logs");
            entity.HasKey(command => command.Id);

            entity.Property(command => command.Id)
                .ValueGeneratedNever();

            entity.Property(command => command.ChatSessionId)
                .IsRequired();

            entity.Property(command => command.UserText)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(command => command.CommandLabel)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(command => command.RawPredictedLabel)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(command => command.Source)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(command => command.AssistantMessage)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(command => command.Preview)
                .HasMaxLength(4000);

            entity.Property(command => command.ResolvedPath)
                .HasMaxLength(2048);

            entity.Property(command => command.ExecutionStatus)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(command => command.ExecutionMessage)
                .HasMaxLength(4000);

            entity.Property(command => command.CreatedAtUtc)
                .HasConversion(DateTimeOffsetToUnixMilliseconds);

            entity.Property(command => command.ExecutedAtUtc)
                .HasConversion(NullableDateTimeOffsetToUnixMilliseconds);

            entity.HasIndex(command => command.CreatedAtUtc);

            entity.HasIndex(command => command.ChatSessionId);
        });

        modelBuilder.Entity<ScheduledTask>(entity =>
        {
            entity.ToTable("scheduled_tasks");
            entity.HasKey(task => task.Id);

            entity.Property(task => task.Id)
                .ValueGeneratedNever();

            entity.Property(task => task.TaskType)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(task => task.DisplayName)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(task => task.QueueStatus)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(task => task.ScheduledForUtc)
                .HasConversion(DateTimeOffsetToUnixMilliseconds);

            entity.Property(task => task.CreatedAtUtc)
                .HasConversion(DateTimeOffsetToUnixMilliseconds);

            entity.Property(task => task.CompletedAtUtc)
                .HasConversion(NullableDateTimeOffsetToUnixMilliseconds);

            entity.Property(task => task.LastMessage)
                .HasMaxLength(4000);

            entity.Property(task => task.AppPath)
                .HasMaxLength(2048);

            entity.Property(task => task.AppDisplayName)
                .HasMaxLength(160);

            entity.Property(task => task.RecipientNames)
                .HasMaxLength(4000);

            entity.Property(task => task.MessageText)
                .HasMaxLength(4000);

            entity.Property(task => task.RepeatMode)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(task => new { task.QueueStatus, task.ScheduledForUtc });

            entity.HasIndex(task => task.CommandLogId);
        });

        modelBuilder.Entity<WebAutomationProject>(entity =>
        {
            entity.ToTable("web_automation_projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Id).ValueGeneratedNever();
            entity.Property(project => project.Name).HasMaxLength(160).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(1000);
            entity.Property(project => project.StartUrl).HasMaxLength(2048).IsRequired();
            entity.Property(project => project.CreatedAtUtc).IsRequired();
            entity.HasMany(project => project.Flows)
                .WithOne()
                .HasForeignKey(flow => flow.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(WebAutomationProject.Flows))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(project => project.Name);
        });

        modelBuilder.Entity<WebAutomationFlow>(entity =>
        {
            entity.ToTable("web_automation_flows");
            entity.HasKey(flow => flow.Id);
            entity.Property(flow => flow.Id).ValueGeneratedNever();
            entity.Property(flow => flow.Name).HasMaxLength(160).IsRequired();
            entity.Property(flow => flow.StartUrl).HasMaxLength(2048).IsRequired();
            entity.Property(flow => flow.BrowserChannel).HasMaxLength(64).IsRequired();
            entity.HasMany(flow => flow.Steps)
                .WithOne()
                .HasForeignKey(step => step.FlowId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(flow => flow.Variables)
                .WithOne()
                .HasForeignKey(variable => variable.FlowId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(flow => flow.Runs)
                .WithOne()
                .HasForeignKey(run => run.FlowId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(WebAutomationFlow.Steps))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(WebAutomationFlow.Variables))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(WebAutomationFlow.Runs))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(flow => flow.ProjectId);
        });

        modelBuilder.Entity<WebAutomationStep>(entity =>
        {
            entity.ToTable("web_automation_steps");
            entity.HasKey(step => step.Id);
            entity.Property(step => step.Id).ValueGeneratedNever();
            entity.Property(step => step.StepType).HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.Property(step => step.SelectorType).HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.Property(step => step.Selector).HasMaxLength(2048);
            entity.Property(step => step.Value).HasMaxLength(4000);
            entity.Property(step => step.Url).HasMaxLength(2048);
            entity.Property(step => step.Description).HasMaxLength(1000);
            entity.HasOne(step => step.SelectorSnapshot)
                .WithOne()
                .HasForeignKey<WebSelectorSnapshot>(snapshot => snapshot.StepId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(step => step.RunStepLogs)
                .WithOne()
                .HasForeignKey(log => log.StepId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(WebAutomationStep.RunStepLogs))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(step => new { step.FlowId, step.OrderIndex }).IsUnique();
        });

        modelBuilder.Entity<WebSelectorSnapshot>(entity =>
        {
            entity.ToTable("web_selector_snapshots");
            entity.HasKey(snapshot => snapshot.Id);
            entity.Property(snapshot => snapshot.Id).ValueGeneratedNever();
            entity.Property(snapshot => snapshot.CssSelector).HasMaxLength(2048);
            entity.Property(snapshot => snapshot.XPath).HasMaxLength(2048);
            entity.Property(snapshot => snapshot.Role).HasMaxLength(128);
            entity.Property(snapshot => snapshot.AccessibleName).HasMaxLength(512);
            entity.Property(snapshot => snapshot.InnerText).HasMaxLength(2000);
            entity.Property(snapshot => snapshot.HtmlSnippet).HasMaxLength(4000);
            entity.Property(snapshot => snapshot.ScreenshotPath).HasMaxLength(2048);
        });

        modelBuilder.Entity<WebAutomationVariable>(entity =>
        {
            entity.ToTable("web_automation_variables");
            entity.HasKey(variable => variable.Id);
            entity.Property(variable => variable.Id).ValueGeneratedNever();
            entity.Property(variable => variable.Key).HasMaxLength(128).IsRequired();
            entity.Property(variable => variable.Value).HasMaxLength(4000).IsRequired();
            entity.HasIndex(variable => new { variable.FlowId, variable.Key }).IsUnique();
        });

        modelBuilder.Entity<BrowserProfile>(entity =>
        {
            entity.ToTable("browser_profiles");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Id).ValueGeneratedNever();
            entity.Property(profile => profile.Name).HasMaxLength(160).IsRequired();
            entity.Property(profile => profile.UserDataDirectory).HasMaxLength(2048).IsRequired();
        });

        modelBuilder.Entity<WebAutomationRun>(entity =>
        {
            entity.ToTable("web_automation_runs");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Id).ValueGeneratedNever();
            entity.Property(run => run.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.Property(run => run.ErrorMessage).HasMaxLength(4000);
            entity.Property(run => run.BrowserChannel).HasMaxLength(64);
            entity.HasMany(run => run.StepLogs)
                .WithOne()
                .HasForeignKey(log => log.RunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(WebAutomationRun.StepLogs))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(run => run.FlowId);
        });

        modelBuilder.Entity<WebAutomationRunStepLog>(entity =>
        {
            entity.ToTable("web_automation_run_step_logs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).ValueGeneratedNever();
            entity.Property(log => log.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.Property(log => log.Message).HasMaxLength(4000);
            entity.Property(log => log.ErrorMessage).HasMaxLength(4000);
            entity.Property(log => log.ScreenshotPath).HasMaxLength(2048);
            entity.Property(log => log.ExtractedValue).HasMaxLength(4000);
            entity.HasIndex(log => log.RunId);
        });
    }
}
