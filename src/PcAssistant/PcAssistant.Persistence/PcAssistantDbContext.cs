using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PcAssistant.Domain;

namespace PcAssistant.Persistence;

public sealed class PcAssistantDbContext(DbContextOptions<PcAssistantDbContext> options) : DbContext(options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommandLogEntry>(entity =>
        {
            entity.ToTable("command_logs");
            entity.HasKey(command => command.Id);

            entity.Property(command => command.Id)
                .ValueGeneratedNever();

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
        });
    }
}
