# AI-Ready Design

Mega Fintrade Risk Monitor is designed to be AI-ready without being AI-dependent.

The .NET monitor can reserve configuration, API, and dashboard space for a future AI advisor service, but it must continue working normally when AI is disabled, unavailable, or not yet implemented.

This design keeps Project 4 stable and independent while allowing Project 5 to be added later as a separate AI service.

---

## Purpose

The purpose of the AI-ready design is to prepare the risk monitor for future decision-support features without coupling the .NET monitoring service to any specific AI provider.

Future AI support may include:

- Natural-language alert explanations
- Daily portfolio risk briefs
- Import-health summaries
- CSV rejection summaries
- Portfolio and symbol-level risk commentary
- Suggested investigation steps
- Executive-style dashboard summaries

These features should be provided by a separate AI advisor service, not directly by the .NET monitor.

---

## Project Boundary

Mega Fintrade Risk Monitor is Project 4.

The future AI advisor is Project 5.

| Project | Responsibility |
|---|---|
| Project 4: Mega Fintrade Risk Monitor .NET | Monitoring, alert rules, alert storage, dashboard, APIs |
| Project 5: Mega Fintrade AI Advisor | AI provider selection, prompt construction, AI summaries, AI explanations |

Project 4 should work without Project 5.

Project 5 may depend on Project 4 data later, but Project 4 should not depend on Project 5 to perform monitoring.

---

## What Project 4 Owns

Mega Fintrade Risk Monitor owns deterministic monitoring behavior.

Project 4 owns:

- Java backend API polling
- Backend availability checks
- Import-health checks
- CSV rejection checks
- Portfolio risk rule evaluation
- Future symbol-level rule evaluation
- Alert generation
- Duplicate active alert prevention
- Alert persistence
- Monitoring snapshots
- Alert APIs
- Monitor status APIs
- AI status placeholder API
- Razor Pages dashboard
- AI Decision Support dashboard placeholder

These features must work even when AI is disabled.

---

## What Project 5 Should Own

The future AI advisor service should own AI-specific responsibilities.

Project 5 should own:

- AI provider selection
- API token management
- Provider-specific request formatting
- LLM prompt construction
- LLM response parsing
- Alert explanation generation
- Daily risk brief generation
- Natural-language analysis
- AI audit logging
- AI usage tracking
- AI response history
- Provider fallback behavior

These responsibilities do not belong inside the .NET risk monitor.

---

## Provider Boundary

Mega Fintrade Risk Monitor should not store or manage provider tokens.

Do not store these in Project 4:

- Gemini API key
- Grok API key
- OpenAI API key
- Ollama provider config
- Anthropic API key
- Any future AI provider token

Project 4 should not contain provider-specific logic such as:

- Gemini client implementation
- Grok client implementation
- OpenAI client implementation
- Provider-specific prompt templates
- Provider-specific retry policy
- Provider-specific token counting
- Provider-specific usage billing logic

The .NET monitor should only know how to contact the future AI advisor service through a configured base URL.

---

## AI Integration Configuration

Project 4 only needs minimal AI integration configuration.

Example configuration:

    AiIntegration:
      Enabled: false
      Project5BaseUrl: http://host.docker.internal:7005

Configuration meanings:

| Setting | Meaning |
|---|---|
| `Enabled` | Whether the monitor should try to use the future AI advisor |
| `Project5BaseUrl` | Base URL of the future AI advisor service |

Current expected value:

    Enabled: false

This means AI integration is disabled by default.

---

## Docker AI Configuration

Docker Compose can reserve optional AI advisor environment variables.

Example:

    AiIntegration__Enabled: false
    AiIntegration__Project5BaseUrl: http://host.docker.internal:7005

The future AI advisor URL is prepared, but Project 5 does not need to exist for Project 4 to run.

If the AI advisor is not running, the .NET monitor should still start normally.

---

## AI Disabled Mode

AI disabled mode is the normal current behavior.

Expected behavior when AI is disabled:

- The dashboard still loads.
- Alert rules still run.
- Monitor APIs still work.
- Alert APIs still work.
- Java backend polling still works.
- SQLite alert storage still works.
- AI status endpoint returns disabled status.
- AI Decision Support panel shows disabled or unavailable status.

Example AI status response:

    {
      "enabled": false,
      "available": false,
      "message": "AI integration is disabled."
    }

AI disabled mode should not be treated as an error.

---

## AI Unavailable Mode

AI unavailable mode may happen in the future if:

- AI integration is enabled
- Project 5 base URL is configured
- Project 5 service is not running
- Project 5 service fails
- Project 5 service times out

Expected behavior:

- The .NET monitor keeps running.
- The dashboard keeps working.
- Deterministic alert rules still run.
- Alert APIs still work.
- AI panel shows unavailable status.
- No provider token details are exposed.

Example AI status response:

    {
      "enabled": true,
      "available": false,
      "project5BaseUrl": "http://host.docker.internal:7005",
      "message": "AI advisor is unavailable."
    }

AI failure should not block core monitoring.

---

## AI Available Mode

AI available mode is a future behavior after Project 5 exists.

In that future design:

1. Project 4 collects monitoring and alert data.
2. Project 4 exposes alert and monitor APIs.
3. Project 5 reads or receives selected monitoring data.
4. Project 5 calls the selected AI provider.
5. Project 5 returns a summary or explanation.
6. Project 4 displays the result in the dashboard.

Project 4 should remain the monitoring source of truth.

Project 5 should remain the AI explanation and decision-support layer.

---

## Dashboard AI Panel

The dashboard can include an AI Decision Support panel.

Current expected state:

    AI integration is disabled.

or:

    AI advisor is unavailable.

This is correct while Project 5 is not implemented.

The panel should not make the dashboard look broken.

Recommended disabled-state wording:

    AI Decision Support is disabled.
    Core monitoring and alert rules are running normally.

Recommended unavailable-state wording:

    AI Advisor is currently unavailable.
    Core monitoring and alert rules are running normally.

The dashboard should never expose provider API keys.

---

## AI Status API

Project 4 can expose a simple AI status endpoint.

Endpoint:

    GET /api/ai/status

Purpose:

- Show whether AI integration is enabled
- Show whether the AI advisor is reachable
- Support the dashboard AI panel
- Confirm disabled mode works correctly

Disabled example:

    {
      "enabled": false,
      "available": false,
      "message": "AI integration is disabled."
    }

Future enabled and available example:

    {
      "enabled": true,
      "available": true,
      "project5BaseUrl": "http://host.docker.internal:7005",
      "message": "AI advisor is available."
    }

Future enabled but unavailable example:

    {
      "enabled": true,
      "available": false,
      "project5BaseUrl": "http://host.docker.internal:7005",
      "message": "AI advisor is unavailable."
    }

---

## Data That May Be Sent to Project 5 Later

In the future, Project 4 may send selected monitoring data to Project 5.

Possible data:

- Active alerts
- Alert history summary
- Portfolio risk summary
- Future symbol-level risk summary
- Latest import status
- CSV rejection summary
- Monitor status
- Java backend reachability status

Example future request to AI advisor:

    {
      "portfolioSummary": {
        "portfolioSharpeRatio": 0.82,
        "portfolioMaxDrawdown": -0.24,
        "latestEquityDate": "2026-05-07"
      },
      "activeAlerts": [
        {
          "type": "DRAWDOWN_BREACH",
          "severity": "HIGH",
          "symbol": null,
          "message": "Portfolio max drawdown breached the configured threshold."
        }
      ]
    }

Project 5 would turn this structured data into natural-language explanation.

---

## Data Project 4 Should Not Send to Project 5

Project 4 should avoid sending unnecessary sensitive or secret data.

Do not send:

- API keys
- Database connection secrets
- Local machine paths unless needed
- Provider credentials
- Raw logs containing secrets
- Unnecessary environment variables

The AI advisor should receive only the monitoring data needed to generate useful explanations.

---

## Deterministic Rules vs AI Explanation

Project 4 alert creation should remain deterministic.

AI should not decide whether a risk alert exists.

Alert decision:

    Deterministic rule engine

Alert explanation:

    Future AI advisor

Example:

| Task | Owner |
|---|---|
| Decide whether Sharpe ratio is too low | Project 4 rule engine |
| Decide whether max drawdown breached threshold | Project 4 rule engine |
| Store active alert | Project 4 alert service |
| Explain the alert in plain English | Future Project 5 AI advisor |
| Generate risk brief | Future Project 5 AI advisor |

This keeps monitoring reliable, testable, and explainable.

---

## Future Provider Selection

Future Project 5 may support multiple providers.

Possible providers:

- Gemini
- Grok
- OpenAI
- Ollama
- Other future providers

Provider selection should happen inside Project 5.

Project 4 should not know which provider is selected.

Project 4 should only call:

    AI advisor base URL

This makes Project 4 stable even if the AI provider changes.

---

## Future API Shape Between Project 4 and Project 5

A future AI advisor endpoint may look like:

    POST /api/ai/risk-brief

Example request:

    {
      "monitorStatus": {
        "javaBackendReachable": true,
        "lastRunSucceeded": true
      },
      "portfolioSummary": {
        "portfolioSharpeRatio": 0.82,
        "portfolioMaxDrawdown": -0.24,
        "latestEquityDate": "2026-05-07"
      },
      "activeAlerts": [
        {
          "type": "DRAWDOWN_BREACH",
          "severity": "HIGH",
          "symbol": null,
          "sourceEndpoint": "/api/reports/summary"
        }
      ]
    }

Example response:

    {
      "summary": "Portfolio risk is elevated because max drawdown breached the configured threshold.",
      "riskLevel": "High",
      "recommendedReviewItems": [
        "Review latest portfolio equity curve.",
        "Check whether recent import data is complete.",
        "Compare drawdown against strategy expectations."
      ]
    }

This is only a future design example.

The current Project 4 implementation does not require Project 5 to exist.

---

## Failure Isolation

AI failure must not break monitoring.

If Project 5 fails:

- Project 4 continues to poll the Java backend.
- Project 4 continues to evaluate alert rules.
- Project 4 continues to store alerts.
- Project 4 continues to serve the dashboard.
- Project 4 continues to expose REST APIs.
- The AI panel shows unavailable status.

AI is an enhancement, not a dependency.

---

## Testing Expectations

AI-ready behavior should be tested in disabled mode.

Test cases:

| Test | Expected result |
|---|---|
| AI disabled in config | `/api/ai/status` returns disabled |
| AI disabled and dashboard loads | Dashboard loads normally |
| AI disabled and manual monitor runs | Monitoring still runs |
| AI disabled and alerts exist | Alert APIs still return alerts |
| Future AI base URL configured but disabled | No AI call is required |

Future tests may include:

| Test | Expected result |
|---|---|
| AI enabled and advisor reachable | AI status returns available |
| AI enabled and advisor down | AI status returns unavailable, app does not crash |
| AI advisor timeout | Dashboard still loads and monitor keeps running |

---

## Security Notes

Project 4 should avoid handling AI provider secrets.

Security principles:

- Do not store provider tokens in Project 4.
- Do not expose provider tokens through API responses.
- Do not print provider tokens in logs.
- Do not commit `.env` files.
- Keep provider-specific secrets in Project 5 when Project 5 is implemented.
- Keep Project 4 environment variables limited to AI enabled flag and AI advisor base URL.

This keeps the monitoring service cleaner and reduces the risk of accidentally exposing secrets.

---

## Documentation Rule

Public README documentation should describe AI as optional.

README should not claim that AI explanations are already implemented unless Project 5 exists and is connected.

Correct wording:

    AI-ready
    Optional future AI advisor
    AI disabled by default
    Core monitor works without AI

Avoid wording like:

    AI-powered monitor

unless the future AI advisor is actually implemented and connected.

---

## Design Notes

AI-ready design principles:

- Project 4 must work without Project 5.
- Project 4 owns deterministic monitoring and alerting.
- Project 5 should own AI provider logic.
- AI provider tokens should not be stored in Project 4.
- AI should explain alerts, not decide deterministic alert rules.
- AI disabled mode should be a normal supported mode.
- AI unavailable mode should not crash the dashboard.
- Docker can reserve the future AI advisor URL.
- Dashboard can reserve an AI Decision Support panel.
- Public documentation should describe AI integration honestly as future-ready and optional.