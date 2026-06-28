# ServiceBridge

ServiceBridge is the production v1.0 successor to the PayBridge MVP. It is a digital service mediation and payment platform for helping users in Iran purchase, activate, renew, and track international digital services.

This repository starts with PR 1 only: repository and branding foundation. It intentionally does not implement authentication, catalog, pricing, orders, payments, admin operations, agent workflows, migrations, or production UI.

## Solution Structure

```text
ServiceBridge.sln

src/
  ServiceBridge.Domain
  ServiceBridge.Application
  ServiceBridge.Infrastructure
  ServiceBridge.Api
  ServiceBridge.Web
  ServiceBridge.Shared

tests/
  ServiceBridge.Tests

docs/
  ServiceBridge.ProjectGuideline.md
  ServiceBridge.Roadmap.md
  ServiceBridge.Architecture.md
  ServiceBridge.BusinessRules.md
```

## Technical Direction

- .NET 8
- ASP.NET Core Web API
- Razor Pages web project
- EF Core-ready Infrastructure project
- xUnit tests
- Clean/Onion architecture
- CQRS-lite without MediatR
- Production-ready monolith, not microservices

## Validate

```powershell
dotnet build
dotnet test
```
