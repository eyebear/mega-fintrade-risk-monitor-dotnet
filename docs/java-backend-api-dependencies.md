# Java Backend API Dependencies

Mega Fintrade Risk Monitor depends on the Mega Fintrade Backend Java service for portfolio risk data, import audit history, and CSV rejection information.

The .NET monitor does not read raw CSV files directly. It does not calculate portfolio analytics by itself. Instead, it consumes processed data from the Java backend through REST APIs, evaluates deterministic alert rules, stores monitoring results, and displays current platform health through APIs and a dashboard.

---

## Service Relationship

The relationship between the Java backend and the .NET monitor is:

| Service | Responsibility |
|---|---|
| Mega Fintrade Backend Java | Imports processed quantitative outputs, stores records, exposes report and import-health APIs |
| Mega Fintrade Risk Monitor .NET | Polls Java backend APIs, evaluates alert rules, stores alerts, displays monitoring status |

The Java backend is the upstream data source for the .NET monitor.

The .NET monitor should not duplicate the Java backend’s ETL responsibility. It should only consume the backend’s API results and turn those results into operational monitoring signals.

---

## Consumed Endpoints

Mega Fintrade Risk Monitor currently consumes these Java backend endpoints:

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/reports/summary` | Read portfolio-level and future symbol-level risk summary data |
| GET | `/api/import/audit` | Read import job history and latest import status |
| GET | `/api/import/rejections` | Read rejected CSV row records |

These endpoints are consumed through the Java backend API client layer in the .NET project.

Controllers, Razor Pages, background workers, and rule services should not directly build Java backend URLs. Java backend communication should remain centralized in the client layer.

---

## Endpoint: GET /api/reports/summary

This endpoint provides the main portfolio risk summary consumed by the monitor.

The monitor uses this endpoint to evaluate portfolio-level risk rules such as:

- Low Sharpe ratio
- Max drawdown breach
- Stale equity data
- Empty report data

Expected portfolio-level fields may include:

| Field | Purpose |
|---|---|
| `portfolioSharpeRatio` | Portfolio Sharpe ratio used by the low Sharpe ratio rule |
| `portfolioMaxDrawdown` | Portfolio max drawdown used by the drawdown breach rule |
| `latestEquityDate` | Latest equity curve date used by the stale data rule |
| `riskMetricRowCount` | Number of imported risk metric rows |
| `backtestResultRowCount` | Number of imported backtest result rows |
| `strategySignalRowCount` | Number of imported strategy signal rows |
| `equityCurveRowCount` | Number of imported equity curve rows |
| `symbols` | Optional future symbol-level risk metrics |

The current Java backend may only return portfolio-level metrics. The .NET monitor must handle that correctly.

Future Java backend versions may return symbol-level metrics. The .NET monitor should evaluate each returned symbol dynamically without hard-coding ticker names.

---

## Portfolio-Level Behavior

When `/api/reports/summary` returns only portfolio-level data, the monitor evaluates only system-level and portfolio-level rules.

Example behavior:

| Data returned | Monitor behavior |
|---|---|
| Portfolio Sharpe ratio | Evaluate low Sharpe ratio rule |
| Portfolio max drawdown | Evaluate drawdown breach rule |
| Latest equity date | Evaluate stale data rule |
| Row counts | Evaluate empty report data rule |
| No symbol list | Skip symbol-level rules without error |

Missing symbol-level data is not an error.

This is important because the current Java backend may not yet provide dynamic symbol-level metrics.

---

## Future Symbol-Level Behavior

The monitor is designed to support future symbol-level metrics from the Java backend.

If the Java backend later returns a `symbols` collection, the monitor should evaluate each symbol independently.

Example future symbol-level structure:

    symbols:
      - symbol: AAPL
        sharpeRatio: 1.25
        maxDrawdown: -0.12
        latestDataDate: 2026-05-07
      - symbol: MSFT
        sharpeRatio: 0.82
        maxDrawdown: -0.18
        latestDataDate: 2026-05-07

The monitor should not assume a fixed list such as:

    AAPL
    MSFT
    GOOGL
    SPY

Instead, it should process whatever symbols the Java backend returns.

Symbol-level alerts should store the symbol value in the alert record.

Portfolio-level and system-level alerts should leave the symbol field null.

---

## Endpoint: GET /api/import/audit

This endpoint provides Java backend import job history.

The monitor uses it to evaluate import-health rules.

Expected information may include:

| Field | Purpose |
|---|---|
| Import job name | Identifies which import process ran |
| Status | Indicates success or failure |
| Started time | Shows when the import began |
| Completed time | Shows when the import ended |
| Message | Gives additional import details |
| Records processed | Shows import volume if available |
| Records rejected | Shows failed row count if available |

The monitor uses this endpoint to detect failed imports.

A failed import may produce an alert such as:

    IMPORT_FAILURE

Import failure alerts are system-level alerts, so the symbol field should be null.

---

## Endpoint: GET /api/import/rejections

This endpoint provides rejected CSV row records from the Java backend.

The monitor uses this endpoint to detect data-quality problems.

Expected information may include:

| Field | Purpose |
|---|---|
| Source file | Shows which CSV file contained the rejected row |
| Row number | Identifies the rejected row |
| Reason | Explains why the row was rejected |
| Raw row content | Helps debug invalid input data |
| Created time | Shows when the rejection was recorded |

The monitor uses this endpoint to detect whether rejected rows exist.

A rejection problem may produce an alert such as:

    CSV_REJECTIONS_FOUND

CSV rejection alerts are system-level alerts, so the symbol field should be null.

---

## Configuration

The Java backend connection is configured through the `JavaBackendApi` section.

Example local development configuration:

    JavaBackendApi:
      BaseUrl: http://localhost:8080
      TimeoutSeconds: 10

Example Docker configuration:

    JavaBackendApi:
      BaseUrl: http://host.docker.internal:8080
      TimeoutSeconds: 10

The key rule is:

| Runtime mode | Java backend URL from .NET monitor |
|---|---|
| .NET monitor runs directly on Mac | `http://localhost:8080` |
| .NET monitor runs in Docker and Java backend runs on Mac | `http://host.docker.internal:8080` |
| .NET monitor and Java backend run in same Docker network | `http://portfolio-backend:8080` |

The Docker configuration should not replace normal local development configuration.

Local `dotnet run` should keep using `localhost`.

Docker runtime should use `host.docker.internal`.

---

## Client Layer Rule

Java backend API calls should be centralized in the API client layer.

The monitor should avoid this pattern:

    Controller -> raw Java backend URL
    Razor Page -> raw Java backend URL
    Rule Engine -> raw Java backend URL
    Background Worker -> raw Java backend URL

The preferred pattern is:

    Controller / Razor Page / Background Worker
      -> Java backend API client
      -> Java backend REST endpoint

This keeps the code easier to test, easier to configure, and easier to change if Java backend endpoint names change later.

---

## Failure Handling

The .NET monitor must continue running even when the Java backend is unavailable.

Java backend failure should not crash the monitor.

Expected behavior:

| Situation | Expected monitor behavior |
|---|---|
| Java backend is running | Fetch data and evaluate rules |
| Java backend is unavailable | Record backend unavailable status and continue running |
| Java backend times out | Handle timeout and continue running |
| Java backend returns empty report | Evaluate empty report data rule |
| Java backend returns portfolio-only data | Evaluate portfolio-level rules only |
| Java backend returns portfolio and symbol data | Evaluate both portfolio and symbol-level rules |

A backend outage may produce an alert such as:

    PROJECT1_UNAVAILABLE

This is a system-level alert, so the symbol field should be null.

---

## Alert Source Mapping

Alerts generated from Java backend API data should keep their source endpoint clear.

Example mapping:

| Alert type | Source endpoint |
|---|---|
| `PROJECT1_UNAVAILABLE` | Java backend base URL or failed endpoint |
| `LOW_SHARPE_RATIO` | `/api/reports/summary` |
| `DRAWDOWN_BREACH` | `/api/reports/summary` |
| `STALE_EQUITY_DATA` | `/api/reports/summary` |
| `EMPTY_REPORT_DATA` | `/api/reports/summary` |
| `IMPORT_FAILURE` | `/api/import/audit` |
| `CSV_REJECTIONS_FOUND` | `/api/import/rejections` |

This makes alerts easier to debug from the dashboard and API responses.

---

## Duplicate Alert Identity

Duplicate active alerts should be prevented using:

    alert type + symbol + source endpoint

This allows separate alerts for different symbols while preventing repeated duplicate alerts for the same unresolved issue.

Example:

| Alert | Duplicate? |
|---|---|
| `LOW_SHARPE_RATIO`, `AAPL`, `/api/reports/summary` | Original |
| `LOW_SHARPE_RATIO`, `AAPL`, `/api/reports/summary` | Duplicate |
| `LOW_SHARPE_RATIO`, `MSFT`, `/api/reports/summary` | Not duplicate |
| `LOW_SHARPE_RATIO`, `null`, `/api/reports/summary` | Portfolio-level alert, separate from symbol alerts |

---

## Design Notes

Java backend dependency rules:

- The Java backend is the source of imported report and data-quality information.
- The .NET monitor should not import CSV files directly.
- The .NET monitor should not calculate full quantitative analytics directly.
- Java backend API calls should remain centralized in the client layer.
- The monitor should support portfolio-only responses.
- The monitor should support future symbol-level responses.
- The monitor should not hard-code ticker symbols.
- The monitor should continue running if the Java backend is unavailable.
- Docker runtime should use `host.docker.internal` when Java backend runs on the host machine.
- Future same-network Docker deployment can use a service name such as `portfolio-backend`.