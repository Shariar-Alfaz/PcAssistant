namespace PcAssistant.Application.Abstractions.UnitOfWorks;

public interface IWebAutomationUnitOfWorkFactory
{
    IWebAutomationUnitOfWork Create();
}
