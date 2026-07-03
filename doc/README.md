# Offline Documentation

This folder stores local copies of the external documentation used for this project.

Use these files first when making repo changes. Refresh from upstream only when the task depends on current behavior, version-specific details, or docs that are not present locally.

## .NET MAUI Blazor Hybrid

Local files:

- `vendor/maui-blazor-hybrid/aspnetcore-blazor-hybrid-index.md`
- `vendor/maui-blazor-hybrid/maui-blazor-hybrid-tutorial.md`
- `vendor/maui-blazor-hybrid/maui-blazor-web-app.md`

Source:

- `https://raw.githubusercontent.com/dotnet/AspNetCore.Docs/main/aspnetcore/blazor/hybrid/index.md`
- `https://raw.githubusercontent.com/dotnet/AspNetCore.Docs/main/aspnetcore/blazor/hybrid/tutorials/maui.md`
- `https://raw.githubusercontent.com/dotnet/AspNetCore.Docs/main/aspnetcore/blazor/hybrid/tutorials/maui-blazor-web-app.md`

## Tailwind CSS

Local files:

- `vendor/tailwind/tailwind-cli.html`
- `vendor/tailwind/detecting-classes-in-source-files.html`
- `vendor/tailwind/functions-and-directives.html`

Source:

- `https://tailwindcss.com/docs/installation/tailwind-cli`
- `https://tailwindcss.com/docs/detecting-classes-in-source-files`
- `https://tailwindcss.com/docs/functions-and-directives`

## Refresh

Run these commands from the repository root to refresh the downloaded docs:

```powershell
$downloads = @(
  @{ Url = 'https://raw.githubusercontent.com/dotnet/AspNetCore.Docs/main/aspnetcore/blazor/hybrid/index.md'; Out = 'doc\vendor\maui-blazor-hybrid\aspnetcore-blazor-hybrid-index.md' },
  @{ Url = 'https://raw.githubusercontent.com/dotnet/AspNetCore.Docs/main/aspnetcore/blazor/hybrid/tutorials/maui.md'; Out = 'doc\vendor\maui-blazor-hybrid\maui-blazor-hybrid-tutorial.md' },
  @{ Url = 'https://raw.githubusercontent.com/dotnet/AspNetCore.Docs/main/aspnetcore/blazor/hybrid/tutorials/maui-blazor-web-app.md'; Out = 'doc\vendor\maui-blazor-hybrid\maui-blazor-web-app.md' },
  @{ Url = 'https://tailwindcss.com/docs/installation/tailwind-cli'; Out = 'doc\vendor\tailwind\tailwind-cli.html' },
  @{ Url = 'https://tailwindcss.com/docs/detecting-classes-in-source-files'; Out = 'doc\vendor\tailwind\detecting-classes-in-source-files.html' },
  @{ Url = 'https://tailwindcss.com/docs/functions-and-directives'; Out = 'doc\vendor\tailwind\functions-and-directives.html' }
)

foreach ($download in $downloads) {
  Invoke-WebRequest -Uri $download.Url -OutFile $download.Out
}
```
