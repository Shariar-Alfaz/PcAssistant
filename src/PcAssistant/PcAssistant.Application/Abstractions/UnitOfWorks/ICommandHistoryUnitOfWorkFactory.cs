namespace PcAssistant.Application.Abstractions.UnitOfWorks;

public interface ICommandHistoryUnitOfWorkFactory
{
    ICommandHistoryUnitOfWork Create();
}
