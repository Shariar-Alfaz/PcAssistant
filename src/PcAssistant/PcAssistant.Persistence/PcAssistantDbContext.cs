using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PcAssistant.Domain;

namespace PcAssistant.Persistence;

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
    }
}
