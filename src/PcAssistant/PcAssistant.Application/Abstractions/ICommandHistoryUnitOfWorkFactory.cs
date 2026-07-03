namespace PcAssistant.Application.Abstractions;

public interface ICommandHistoryUnitOfWorkFactory
{
    ICommandHistoryUnitOfWork Create();
}
