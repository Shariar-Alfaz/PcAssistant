using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions;

namespace PcAssistant.Persistence;

internal sealed class CommandHistoryUnitOfWorkFactory(
    IDbContextFactory<PcAssistantDbContext> dbContextFactory) : ICommandHistoryUnitOfWorkFactory
{
    public ICommandHistoryUnitOfWork Create()
    {
        return new CommandHistoryUnitOfWork(dbContextFactory.CreateDbContext());
    }
}
