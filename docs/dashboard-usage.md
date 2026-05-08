# Dashboard Usage

Mega Fintrade Risk Monitor includes a Razor Pages dashboard for viewing system status, Java backend connectivity, active alerts, portfolio risk information, import health, CSV rejection health, alert history, and future AI decision-support status.

The dashboard is designed for quick monitoring and portfolio demonstration. It provides a simple browser-based view of the same monitoring state exposed through the REST APIs.

---

## Dashboard URL

Local .NET runtime:

    http://localhost:5189/dashboard

Docker Compose runtime:

    http://localhost:5189/dashboard

If the local .NET runtime uses a different port, check the terminal output after running:

    dotnet run --project MegaFintradeRiskMonitor.csproj

Docker Compose maps the application to host port `5189` by default.

---

## Starting the Application

The dashboard can be used in either local .NET mode or Docker Compose mode.

### Option 1: Run directly with .NET

From the project root:

    dotnet restore mega-fintrade-risk-monitor-dotnet.sln
    dotnet build mega-fintrade-risk-monitor-dotnet.sln
    dotnet run --project MegaFintradeRiskMonitor.csproj

Then open:

    http://localhost:5189/dashboard

### Option 2: Run with Docker Compose

From the project root:

    docker compose up --build

Then open:

    http://localhost:5189/dashboard

To stop Docker Compose:

    docker compose down

---

## Dashboard Purpose

The dashboard helps users answer these questions:

- Is the .NET risk monitor running?
- Can the monitor reach the Java backend?
- When did monitoring last run?
- Are there active alerts?
- Are portfolio risk metrics healthy?
- Did Java backend imports fail?
- Are there rejected CSV rows?
- Is symbol-level data available?
- Is the future AI advisor enabled or disabled?

The dashboard should remain usable even when the Java backend is unavailable.

---

## Expected Dashboard Sections

The dashboard may include these sections:

| Section | Purpose |
|---|---|
| System Status | Shows monitor status and Java backend reachability |
| Active Alerts | Shows unresolved alerts that need attention |
| Portfolio Risk Summary | Shows portfolio Sharpe ratio, max drawdown, and latest data date |
| Symbol Risk Summary | Shows future symbol-level metrics when available |
| Import Health | Shows latest Java backend import status |
| CSV Rejection Health | Shows rejected CSV row status |
| Alert History | Shows resolved and historical alerts |
| AI Decision Support | Shows disabled or unavailable status until Project 5 exists |

The exact layout may change as the UI evolves, but the dashboard should remain focused on monitoring state and alert visibility.

---

## System Status Section

The System Status section shows whether the risk monitor is operating normally.

It may display:

- Current monitor status
- Java backend reachability
- Last monitoring run time
- Last monitoring result
- Error message if the Java backend is unavailable

Example healthy state:

    Monitor Status: Running
    Java Backend: Reachable
    Last Run: 2026-05-08 18:30 UTC
    Last Result: Monitoring completed successfully

Example backend unavailable state:

    Monitor Status: Running
    Java Backend: Unavailable
    Last Result: Java backend did not respond

A Java backend unavailable message does not mean the .NET monitor is broken.

It usually means one of these is true:

- The Java backend is not running
- The Java backend is running on a different port
- Docker is using the wrong backend URL
- The Java backend endpoint failed or timed out

---

## Active Alerts Section

The Active Alerts section shows unresolved alerts.

Active alerts may include:

- Java backend unavailable
- Import failure
- CSV rejections found
- Portfolio max drawdown breach
- Portfolio low Sharpe ratio
- Stale equity data
- Empty report data
- Future symbol-level risk alerts

Typical alert fields:

| Field | Meaning |
|---|---|
| Type | Alert category, such as `LOW_SHARPE_RATIO` |
| Severity | Alert urgency, such as `MEDIUM` or `HIGH` |
| Symbol | Null for system or portfolio alerts; symbol value for symbol-level alerts |
| Message | Human-readable explanation |
| Source Endpoint | Java backend endpoint or monitor source that produced the alert |
| Created Time | When the alert was created |

If there are no active alerts, the dashboard should show a clear normal state.

Example:

    No active alerts.

---

## Portfolio Risk Summary Section

The Portfolio Risk Summary section shows portfolio-level risk metrics returned by the Java backend.

Common fields:

| Field | Purpose |
|---|---|
| Portfolio Sharpe Ratio | Used to evaluate risk-adjusted return |
| Portfolio Max Drawdown | Used to detect drawdown breach |
| Latest Equity Date | Used to detect stale portfolio data |
| Risk Metric Row Count | Used to confirm imported risk metric data exists |
| Backtest Result Row Count | Used to confirm imported backtest result data exists |
| Strategy Signal Row Count | Used to confirm imported signal data exists |
| Equity Curve Row Count | Used to confirm imported equity curve data exists |

Example interpretation:

| Metric | Example | Meaning |
|---|---:|---|
| Sharpe Ratio | 1.25 | Above minimum threshold, likely normal |
| Max Drawdown | -0.10 | Less severe than -0.20 threshold, likely normal |
| Latest Equity Date | 2026-05-07 | Recent data, likely normal |

If portfolio data is missing or all row counts are zero, the monitor may generate:

    EMPTY_REPORT_DATA

---

## Symbol Risk Summary Section

The Symbol Risk Summary section is designed for future symbol-level backend data.

The dashboard should not use hard-coded ticker cards.

Avoid fixed layouts like:

    AAPL card
    MSFT card
    GOOGL card
    SPY card

Preferred layout:

    Dynamic symbol risk table

Example:

| Symbol | Sharpe Ratio | Max Drawdown | Latest Data Date | Alert Status |
|---|---:|---:|---|---|
| AAPL | 1.25 | -0.12 | 2026-05-07 | Normal |
| MSFT | 0.82 | -0.18 | 2026-05-07 | Low Sharpe |
| SPY | 1.10 | -0.09 | 2026-05-07 | Normal |

If symbol-level data is not available from the Java backend yet, the dashboard should show:

    Symbol-level metrics are not available from the Java backend yet.
    Portfolio-level monitoring is active.

Missing symbol-level data is not an error.

---

## Import Health Section

The Import Health section shows whether the Java backend import process is healthy.

It is based on:

    GET /api/import/audit

This section may display:

- Latest import job name
- Latest import status
- Import start time
- Import completion time
- Processed record count
- Rejected record count
- Import message

Important statuses:

| Status | Meaning |
|---|---|
| Success | Latest import completed normally |
| Failed | Latest import failed and may require attention |
| Unknown | No import status is available |

If the latest import failed, the monitor may generate:

    IMPORT_FAILURE

---

## CSV Rejection Health Section

The CSV Rejection Health section shows whether the Java backend recorded rejected CSV rows.

It is based on:

    GET /api/import/rejections

Rejected rows may indicate:

- Invalid dates
- Missing required values
- Invalid numeric fields
- Malformed CSV rows
- Unsupported or unexpected data format
- Upstream data pipeline problems

If rejected rows exist above the configured threshold, the monitor may generate:

    CSV_REJECTIONS_FOUND

Default threshold:

    0

This means any rejected row can produce an alert unless the threshold is changed.

---

## Alert History Section

The Alert History section shows resolved or historical alerts.

Alert history is useful for:

- Auditing previous issues
- Showing when risk conditions occurred
- Confirming that alerts were resolved
- Demonstrating monitoring behavior over time

Resolved alerts should remain in history.

Normal alert resolution should not delete alert records.

---

## AI Decision Support Section

The AI Decision Support section is reserved for future Project 5 integration.

Current expected behavior:

    AI integration is disabled.

or:

    AI advisor is unavailable.

This is correct while Project 5 is not implemented or not running.

The .NET monitor should not store:

- Gemini API keys
- Grok API keys
- OpenAI API keys
- Ollama provider settings
- Any future AI provider tokens

The future AI advisor service should own provider selection, token management, prompt construction, and AI-generated explanation logic.

The dashboard should continue working even when AI is disabled.

---

## Manual Monitoring From Dashboard

The dashboard may include a button such as:

    Run Monitor

Purpose:

- Trigger monitoring immediately
- Test Java backend connectivity
- Evaluate alert rules on demand
- Refresh dashboard data after a backend import

Equivalent API command:

    curl -X POST http://localhost:5189/api/monitor/run

If the Java backend is unavailable, manual monitoring should still complete without crashing the .NET service.

It may create or display a system-level unavailable alert:

    PROJECT1_UNAVAILABLE

---

## Auto Refresh

The dashboard may include auto-refresh behavior.

Purpose:

- Keep system status current
- Show newly generated alerts
- Refresh backend availability state
- Reduce manual page reloads during demos

If auto-refresh is enabled, the page should refresh at a simple interval.

If auto-refresh is not enabled, the user can manually reload the browser page.

---

## Common Dashboard Scenarios

### Scenario 1: Everything is healthy

Expected dashboard state:

    Java Backend: Reachable
    Active Alerts: None
    Import Health: Success
    CSV Rejections: None
    AI: Disabled or unavailable

This is a normal successful monitoring state.

### Scenario 2: Java backend is not running

Expected dashboard state:

    Java Backend: Unavailable
    Active Alerts: PROJECT1_UNAVAILABLE
    Portfolio Risk Summary: Unavailable or empty
    AI: Disabled or unavailable

This means the Java backend should be started or the backend URL should be checked.

### Scenario 3: Import failed

Expected dashboard state:

    Import Health: Failed
    Active Alerts: IMPORT_FAILURE

This means the Java backend import process should be inspected.

### Scenario 4: CSV rows were rejected

Expected dashboard state:

    CSV Rejection Health: Rejections found
    Active Alerts: CSV_REJECTIONS_FOUND

This means input data quality should be inspected.

### Scenario 5: Portfolio risk threshold breached

Expected dashboard state:

    Portfolio Risk Summary: Shows breached metric
    Active Alerts: DRAWDOWN_BREACH or LOW_SHARPE_RATIO

This means the portfolio risk condition crossed a configured threshold.

### Scenario 6: Symbol data is missing

Expected dashboard state:

    Symbol-level metrics are not available from the Java backend yet.
    Portfolio-level monitoring is active.

This is normal for the current Java backend if symbol-level report data has not been implemented yet.

---

## Docker Dashboard Notes

When running through Docker Compose:

    docker compose up --build

Open:

    http://localhost:5189/dashboard

Java backend URL rule:

| Java backend location | URL used by .NET monitor |
|---|---|
| Java backend runs directly on Mac | `http://host.docker.internal:8080` |
| Java backend runs in same Docker network | `http://portfolio-backend:8080` |

If the dashboard shows Java backend unavailable while Docker is running, check:

- Is the Java backend running?
- Is the Java backend on port `8080`?
- Does `appsettings.Docker.json` use `host.docker.internal`?
- Does `docker-compose.yml` override `JavaBackendApi__BaseUrl` correctly?

---

## API Links From Dashboard

Useful API links for verifying dashboard data:

| API | Purpose |
|---|---|
| `/api/health` | Confirms the .NET monitor is running |
| `/api/monitor/status` | Shows latest monitor status |
| `/api/monitor/run` | Manually runs monitoring through POST |
| `/api/alerts` | Shows all alerts |
| `/api/alerts/active` | Shows active alerts |
| `/api/alerts/history` | Shows resolved or historical alerts |
| `/api/ai/status` | Shows AI integration status |

Swagger UI:

    http://localhost:5189/swagger

---

## Troubleshooting

### Dashboard does not open

Check whether the app is running.

For Docker:

    docker ps

Expected container:

    mega-fintrade-risk-monitor

Check logs:

    docker logs mega-fintrade-risk-monitor

For local .NET:

    dotnet run --project MegaFintradeRiskMonitor.csproj

Then check the port printed in the terminal.

---

### Java backend shows unavailable

This usually means the upstream Java backend is not reachable.

Check local Java backend:

    http://localhost:8080

In Docker mode, the .NET monitor should call:

    http://host.docker.internal:8080

This is different from browser access.

Browser uses:

    http://localhost:8080

Container uses:

    http://host.docker.internal:8080

---

### AI panel shows disabled

This is expected.

Project 5 AI Advisor is not required for Project 4 to run.

The dashboard should still work normally.

---

### Active alert does not duplicate

This is expected.

Duplicate active alerts are prevented by:

    alert type + symbol + source endpoint

If the same unresolved issue happens repeatedly, the monitor should not spam duplicate active alerts.

---

### Alert remains in history after resolution

This is expected.

Resolved alerts should remain in alert history for auditability.

Normal operation should resolve alerts rather than delete them.

---

## Dashboard Design Notes

Dashboard design principles:

- Keep monitoring status visible.
- Show active alerts clearly.
- Keep portfolio-level risk metrics easy to understand.
- Treat missing symbol-level data as normal if the Java backend does not provide it yet.
- Use dynamic symbol tables instead of hard-coded ticker cards.
- Keep AI panel optional and non-blocking.
- Do not expose provider tokens in the dashboard.
- Keep the dashboard usable even when the Java backend is unavailable.
- Support both local .NET runtime and Docker Compose runtime.