# ServiceBridge

ServiceBridge is the production v1.0 evolution of the mature MVP codebase. The repository preserves the existing layered monolith, business modules, tests, and Razor Pages/Web API split while renaming the product and codebase to ServiceBridge.

## Solution Structure

```text
ServiceBridge.sln
ServiceBridge.Domain
ServiceBridge.Application
ServiceBridge.Infrastructure
ServiceBridge.Api
ServiceBridge.Web
ServiceBridge.Shared
ServiceBridge.Tests
docs/
```

## Technical Direction

- .NET 8
- ASP.NET Core Web API
- Razor Pages
- EF Core and SQL Server
- xUnit tests
- Clean/Onion-style layered monolith
- CQRS-lite without MediatR
- No microservices
- No workflow engine

## Validate

```powershell
dotnet build ServiceBridge.sln
dotnet test ServiceBridge.sln
```
