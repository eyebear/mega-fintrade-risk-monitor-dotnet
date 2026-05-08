# AI Integration Boundary

Mega Fintrade Risk Monitor .NET is AI-ready, but it does not implement AI provider logic.

The risk monitor remains a deterministic monitoring service. It polls the Java backend, evaluates alert rules, stores monitoring snapshots, persists alerts, and displays a dashboard.

AI Decision Support is reserved for a future external AI advisor service.

## What the risk monitor stores

The risk monitor stores only:

| Item | Purpose |
|---|---|
| Enabled flag | Whether the AI panel should show enabled/configured state |
| AI advisor base URL | Where a future AI advisor service may be called |
| AI status result | Whether the future AI advisor appears available |
| AI analysis result | What the dashboard may display later |

## What the risk monitor must not store

| Provider / Token Type | Stored in monitor? |
|---|---|
| Gemini API key | No |
| Grok API key | No |
| OpenAI API key | No |
| Ollama provider config | No |
| Future AI provider token | No |

## Future AI advisor responsibilities

| Responsibility | Owner |
|---|---|
| Provider selection | AI advisor |
| API token management | AI advisor |
| Prompt construction | AI advisor |
| Alert explanation | AI advisor |
| Risk brief generation | AI advisor |
| AI history | AI advisor |
| AI audit logging | AI advisor |

## Current Phase 8 behavior

The current implementation uses a disabled/no-op AI client.

This means:

- The dashboard can display AI readiness status.
- `GET /api/ai/status` returns a stable status response.
- The application works even when no AI advisor exists.
- No AI provider SDK is required.
- No API key is stored in the risk monitor.
- Core monitoring continues to work without AI.

## Future data passed to AI advisor

A future AI advisor can receive:

| Data | Purpose |
|---|---|
| Portfolio Sharpe ratio | Explain portfolio-level risk-adjusted performance |
| Portfolio max drawdown | Explain portfolio downside risk |
| Latest equity date | Explain stale data risk |
| Symbol-level metrics | Explain symbol-specific risk when available |
| Active alerts | Explain current unresolved risk conditions |

The symbol list must remain dynamic.

The risk monitor must not build AI logic around fixed symbols such as AAPL, MSFT, GOOGL, or SPY. Any symbol-level data must come from the Java backend response.