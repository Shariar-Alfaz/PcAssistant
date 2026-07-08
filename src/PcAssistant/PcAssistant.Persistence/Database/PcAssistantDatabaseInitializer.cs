using Autofac;
using Microsoft.EntityFrameworkCore;
using PcAssistant.Domain.Entity;
using PcAssistant.Domain.Enums;

namespace PcAssistant.Persistence.Database;

public sealed class PcAssistantDatabaseInitializer(
    SqliteProviderBootstrapper sqliteProviderBootstrapper,
    ILifetimeScope lifetimeScope)
{
    public void Initialize()
    {
        sqliteProviderBootstrapper.Initialize();
        using var scope = lifetimeScope.BeginLifetimeScope();
        var dbContext = scope.Resolve<PcAssistantDbContext>();
        dbContext.Database.Migrate();
        SeedDemoWebAutomationFlow(dbContext);
    }

    private static void SeedDemoWebAutomationFlow(PcAssistantDbContext dbContext)
    {
        if (dbContext.WebAutomationProjects.Any(project => project.StartUrl == "https://example.com"))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var project = WebAutomationProject.Create(
            "Example web automation",
            "Demo flow that opens example.com, reads the heading, and captures a screenshot.",
            "https://example.com",
            now);
        var flow = WebAutomationFlow.Create(
            project.Id,
            "Example.com heading capture",
            "https://example.com",
            headless: false,
            "msedge",
            usePersistentSession: true,
            30000,
            now);

        dbContext.WebAutomationProjects.Add(project);
        dbContext.WebAutomationFlows.Add(flow);
        dbContext.WebAutomationSteps.AddRange(
            WebAutomationStep.Create(flow.Id, 1, WebAutomationStepType.GotoUrl, WebSelectorType.Css, null, null, "https://example.com", "Open example.com", 30000, 250, false, false, 0, now),
            WebAutomationStep.Create(flow.Id, 2, WebAutomationStepType.WaitForSelector, WebSelectorType.Css, "h1", null, null, "Wait for h1", 30000, 250, false, false, 0, now),
            WebAutomationStep.Create(flow.Id, 3, WebAutomationStepType.ExtractText, WebSelectorType.Css, "h1", null, null, "Extract h1 text", 30000, 250, false, false, 0, now),
            WebAutomationStep.Create(flow.Id, 4, WebAutomationStepType.Screenshot, WebSelectorType.Css, null, null, null, "Take screenshot", 30000, 0, false, true, 0, now));
        dbContext.SaveChanges();
    }
}
