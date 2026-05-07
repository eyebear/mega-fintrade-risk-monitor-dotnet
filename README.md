# Mega Fintrade Risk Monitor (.NET)

Mega Fintrade Risk Monitor is a C#/.NET real-time monitoring, alerting, and dashboard service for the Mega Fintrade Platform.

It monitors the Java backend APIs, evaluates risk and import-health conditions, stores alert records, exposes monitoring and alert APIs, and displays the platform’s operational status through a Razor Pages dashboard.

This service is designed to be AI-ready but not AI-dependent. Future AI analysis will be handled by a separate AI advisor service. Mega Fintrade Risk Monitor must continue working normally even when the AI advisor service is disabled or unavailable.

## Platform Role

Mega Fintrade is a multi-service financial data platform built with Java, Python, C++, and C#/.NET.

The corrected platform data flow is:

1. Mega Fintrade Quant Engine downloads and prepares raw market data.

2. Mega Fintrade Market Engine C++ processes raw market data, validates records, removes invalid rows, calculates daily returns, and produces cleaned market outputs.

3. Mega Fintrade Quant Engine consumes the cleaned C++ outputs, runs quantitative analytics and backtesting, and produces strategy signals, risk metrics, backtest results, and portfolio equity curve files.

4. Mega Fintrade Backend Java imports the processed quantitative outputs, stores them, exposes report APIs, tracks import audit history, and records rejected CSV rows.

5. Mega Fintrade Risk Monitor .NET polls the Java backend APIs, evaluates deterministic alert rules, stores monitoring results, exposes alert APIs, and displays active system and portfolio risk conditions through a dashboard.

6. A future Mega Fintrade AI Advisor may provide optional AI-generated alert explanations, daily risk briefs, and decision-support summaries. The AI advisor is optional and must remain separate from the core monitoring service.

## Purpose

Mega Fintrade Risk Monitor provides the monitoring and alerting layer for the Mega Fintrade Platform.

Its main responsibilities are:

- Poll Java backend report and import-health APIs
- Detect unavailable backend services
- Evaluate deterministic risk rules
- Evaluate import-health rules
- Detect CSV rejection problems
- Store alert records
- Store monitoring snapshots
- Expose REST APIs for health, monitor status, and alerts
- Display active alerts through a Razor Pages dashboard
- Reserve an optional AI Decision Support panel for future integration
- Continue running even when the AI advisor service is unavailable

## Current Architecture

Mega Fintrade Risk Monitor is built as an ASP.NET Core application with both API and dashboard support.

Core components:

- ASP.NET Core Web API controllers
- Razor Pages dashboard
- Configuration-based Java backend connection
- Centralized options classes
- Planned background monitoring worker
- Planned alert rule engine
- Planned Entity Framework Core persistence
- Planned SQLite database
- Planned Swagger / OpenAPI documentation
- Planned Docker packaging
- Planned GitHub Actions CI

## Technology Stack

- C#
- .NET
- ASP.NET Core
- Razor Pages
- Web API Controllers
- Entity Framework Core
- SQLite
- BackgroundService
- IHttpClientFactory
- Swagger / OpenAPI
- xUnit
- Docker
- GitHub Actions
- VS Code

## Java Backend API Dependencies

Mega Fintrade Risk Monitor is designed to consume the following Java backend endpoints:

- GET /api/reports/summary
- GET /api/import/audit
- GET /api/import/rejections

These endpoint paths should remain centralized in the client and DTO layer. The monitoring service should not scatter Java backend endpoint strings across controllers or services.

Current planned local Java backend base URL:

http://localhost:8080

## AI Integration Boundary

Mega Fintrade Risk Monitor is AI-ready, but it does not directly manage AI providers.

Mega Fintrade Risk Monitor does not store or manage API tokens for:

- Gemini
- Grok
- OpenAI
- Ollama
- Any future AI provider

A future Mega Fintrade AI Advisor service should own:

- AI provider selection
- API token management
- LLM prompt construction
- AI alert explanation
- AI daily risk brief generation
- AI analysis history
- AI audit logging

Mega Fintrade Risk Monitor only needs to know:

- Whether AI integration is enabled
- Where the AI advisor service is located
- Whether the AI advisor service is reachable
- What AI result the AI advisor service returns

If the AI advisor service is disabled or unavailable, Mega Fintrade Risk Monitor must continue to operate normally.

## Configuration

Main configuration values are stored in `appsettings.json`.

Expected configuration sections:

- Project1Api
- AiIntegration
- AlertRules
- Monitoring

Example responsibilities:

- Project1Api stores the Java backend base URL and timeout settings.
- AiIntegration stores whether future AI integration is enabled and where the AI advisor service is located.
- AlertRules stores configurable risk and import-health thresholds.
- Monitoring stores background polling interval settings.

## Current Endpoints

Health check:

GET /api/health

Monitor configuration/status:

GET /api/monitor/status

Swagger UI:

/swagger

## Local Development

Restore dependencies:

dotnet restore mega-fintrade-risk-monitor-dotnet.sln

Build the solution:

dotnet build mega-fintrade-risk-monitor-dotnet.sln

Run the application:

dotnet run --project MegaFintradeRiskMonitor.csproj

The application will run on the local port shown in the terminal.

Example:

http://localhost:5189

## Current Development Status

Completed foundation work:

- Repository created
- ASP.NET Core web application created
- Razor Pages enabled
- Web API controllers enabled
- Swagger added
- Clean folder structure added
- Configuration options added
- Health endpoint added
- Monitor status endpoint added

Planned next work:

- Add `.gitignore`
- Add GitHub Actions CI
- Add Java backend API client
- Add DTOs matching Java backend response contracts
- Add alert domain models
- Add SQLite persistence
- Add alert rule engine
- Add background monitoring worker
- Add alert APIs
- Build the dashboard UI
- Add Docker support

## Local Ports

Planned local ports:

- Java backend: http://localhost:8080
- Risk monitor dashboard and API: http://localhost:5189 or the port shown by `dotnet run`
- Future AI advisor service: http://localhost:7005

## Design Principles

Mega Fintrade Risk Monitor follows these design principles:

- Monitoring logic should be deterministic and testable.
- Java backend API calls should be centralized in a client layer.
- Alert rules should use configurable thresholds.
- Alert storage should prevent duplicate active alerts.
- The dashboard should work without AI.
- AI integration should remain optional.
- Provider tokens should never be stored in the monitoring service.
- Local development should remain simple and free.
- The project should remain portfolio-ready with CI, tests, documentation, and Docker support.