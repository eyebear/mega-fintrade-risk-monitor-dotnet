# Mega Fintrade Risk Monitor (.NET)

Project 4 of the Mega Fintrade Platform.

## Purpose

Mega Fintrade Risk Monitor is a C#/.NET monitoring and alerting service for the Mega Fintrade Platform.

It is responsible for monitoring the Java PortfolioRiskPlatform backend, evaluating deterministic risk and import-health rules, storing alert records, exposing alert APIs, and displaying monitoring status through a simple dashboard.

This project is also designed to be AI-ready, but not AI-dependent. Future AI analysis will be handled by Project 5 as a separate service. Project 4 must continue working normally even when Project 5 is disabled or unavailable.

## Platform Role

The overall Mega Fintrade data flow is:

Project 3 C++ Market Engine produces cleaned market data and daily returns.

Project 2 Python Quant Engine produces strategy signals, backtest results, risk metrics, and portfolio equity curve outputs.

Project 1 Java Backend imports and exposes processed portfolio, report, and import-health data through REST APIs.

Project 4 .NET Risk Monitor polls Project 1 APIs, evaluates alert rules, stores alerts, exposes monitoring APIs, and displays a dashboard.

Future Project 5 AI Advisor may provide optional AI-generated risk explanations and summaries.

## Current Project 4 Responsibilities

- Poll Project 1 Java backend APIs
- Evaluate alert rules
- Store alerts and monitoring snapshots
- Expose REST APIs for health, monitor status, and alerts
- Provide a Razor Pages dashboard
- Reserve an optional AI Decision Support panel
- Continue running even when Project 5 is unavailable

## Planned Project 1 API Dependencies

Project 4 is designed to consume the following Project 1 endpoints:

- GET /api/reports/summary
- GET /api/import/audit
- GET /api/import/rejections

These contracts should remain centralized in the Project 4 client and DTO layer.

## Technology Stack

- C#
- .NET
- ASP.NET Core
- Razor Pages
- Web API Controllers
- Entity Framework Core
- SQLite
- BackgroundService
- Swagger / OpenAPI
- xUnit
- Docker
- GitHub Actions

## Local Development

Restore dependencies:

dotnet restore mega-fintrade-risk-monitor-dotnet.sln

Build the project:

dotnet build mega-fintrade-risk-monitor-dotnet.sln

Run the app:

dotnet run --project MegaFintradeRiskMonitor.csproj

The app will run on the local port shown in the terminal.

Example:

http://localhost:5189

## Current Endpoints

Health check:

GET /api/health

Monitor configuration/status:

GET /api/monitor/status

Swagger UI:

/swagger

## Project 5 AI Boundary

Project 4 does not store Gemini, Grok, OpenAI, Ollama, or other AI provider tokens.

Future Project 5 will own:

- AI provider selection
- API token management
- LLM prompt building
- AI alert explanation
- AI daily risk brief generation
- AI analysis history

Project 4 only needs to know whether Project 5 is enabled, where Project 5 is located, and what AI result Project 5 returns.

