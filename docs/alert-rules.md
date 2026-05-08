# Alert Rules

Mega Fintrade Risk Monitor evaluates deterministic alert rules against data returned by the Mega Fintrade Backend Java service.

The alert rule engine is designed to convert backend system status, import health, portfolio risk metrics, CSV rejection information, and future symbol-level metrics into clear alert records.

The monitor does not use AI to decide whether an alert should be generated. Alert generation is rule-based, deterministic, configurable, and testable.

---

## Alert Rule Purpose

Alert rules help identify problems such as:

- Java backend unavailable
- Failed import jobs
- Rejected CSV records
- Portfolio drawdown breach
- Low portfolio Sharpe ratio
- Stale portfolio data
- Empty report data
- Future symbol-level drawdown breach
- Future symbol-level low Sharpe ratio
- Future symbol-level stale data

Each alert rule produces an alert candidate. The alert service then decides whether that alert should be saved as a new active alert or ignored as a duplicate active alert.

---

## Rule Evaluation Flow

The monitoring flow is:

1. The monitor calls the Java backend API client.
2. The client retrieves portfolio report summary, import audit data, and CSV rejection data.
3. The monitor passes the returned data to the alert rule engine.
4. The rule engine evaluates system-level rules.
5. The rule engine evaluates portfolio-level rules.
6. If symbol-level data exists, the rule engine evaluates each symbol dynamically.
7. Alert candidates are passed to the alert service.
8. The alert service prevents duplicate active alerts.
9. New alert records are stored in SQLite.
10. The dashboard and alert APIs display the latest alert state.

Conceptual flow:

    Java Backend APIs
      -> Java Backend API Client
      -> Monitoring Service
      -> Alert Rule Engine
      -> Alert Service
      -> SQLite
      -> REST APIs and Dashboard

---

## Alert Levels

Alert severity describes the urgency of the issue.

| Severity | Meaning |
|---|---|
| `LOW` | Informational or low-risk issue |
| `MEDIUM` | Important issue that should be reviewed |
| `HIGH` | Serious issue that may affect platform reliability or portfolio risk |
| `CRITICAL` | Severe system-level issue that prevents normal monitoring |

Severity should remain deterministic and should not depend on AI-generated interpretation.

---

## Alert Types

The monitor supports these alert types:

| Alert Type | Meaning |
|---|---|
| `PROJECT1_UNAVAILABLE` | The Java backend cannot be reached |
| `IMPORT_FAILURE` | The latest Java backend import job failed |
| `CSV_REJECTIONS_FOUND` | Rejected CSV rows exist |
| `DRAWDOWN_BREACH` | Portfolio or symbol max drawdown breached the configured threshold |
| `LOW_SHARPE_RATIO` | Portfolio or symbol Sharpe ratio is below the configured threshold |
| `STALE_EQUITY_DATA` | Portfolio or symbol data is older than the configured stale-data threshold |
| `EMPTY_REPORT_DATA` | Java backend report data is empty or all major row counts are zero |

---

## Alert Scope

Alerts can be system-level, portfolio-level, or symbol-level.

| Scope | Symbol value | Example |
|---|---|---|
| System-level | `null` | Java backend unavailable |
| Portfolio-level | `null` | Portfolio drawdown breach |
| Symbol-level | Non-null symbol | Low Sharpe ratio for AAPL |

The `Symbol` field is nullable.

A null symbol means the alert belongs to the system or the overall portfolio.

A non-null symbol means the alert belongs to a specific returned symbol from the Java backend.

---

## Configurable Thresholds

Alert rules should use configurable thresholds from application settings.

Example configuration section:

    AlertRules:
      MaxDrawdownThreshold: -0.20
      MinimumSharpeRatio: 1.0
      StaleDataDays: 3
      CsvRejectionThreshold: 0

Threshold meanings:

| Setting | Meaning |
|---|---|
| `MaxDrawdownThreshold` | Maximum acceptable drawdown before an alert is created |
| `MinimumSharpeRatio` | Minimum acceptable Sharpe ratio before an alert is created |
| `StaleDataDays` | Maximum allowed age of latest data before stale-data alert |
| `CsvRejectionThreshold` | Maximum allowed rejected CSV rows before rejection alert |

Thresholds should not depend on symbol names.

This keeps the monitor compatible with current portfolio-only data and future flexible-symbol data.

---

## Rule: Java Backend Unavailable

Alert type:

    PROJECT1_UNAVAILABLE

Trigger condition:

    Java backend API call fails, times out, or cannot connect.

Severity:

    CRITICAL

Scope:

    System-level

Symbol:

    null

Source endpoint:

    Java backend base URL or failed endpoint

Purpose:

This rule detects whether the upstream Java backend is unreachable.

The monitor should not crash when the Java backend is unavailable. Instead, it should record or expose an unavailable status and generate a system-level alert.

Example alert message:

    Mega Fintrade Backend Java is unavailable or did not respond within the configured timeout.

---

## Rule: Import Failure

Alert type:

    IMPORT_FAILURE

Trigger condition:

    Latest import audit status indicates failure.

Severity:

    HIGH

Scope:

    System-level

Symbol:

    null

Source endpoint:

    /api/import/audit

Purpose:

This rule detects whether the Java backend import process failed.

Import failures are important because the monitor depends on the Java backend to import processed risk metrics, backtest results, strategy signals, and portfolio equity curve data.

Example alert message:

    Latest Java backend import job failed.

---

## Rule: CSV Rejections Found

Alert type:

    CSV_REJECTIONS_FOUND

Trigger condition:

    Rejected CSV row count is greater than the configured threshold.

Default threshold:

    0

Severity:

    MEDIUM

Scope:

    System-level

Symbol:

    null

Source endpoint:

    /api/import/rejections

Purpose:

This rule detects data-quality problems from imported CSV files.

A rejected row may indicate invalid numeric values, missing required fields, invalid dates, unknown symbols, or malformed input records.

Example alert message:

    Java backend reported rejected CSV rows.

---

## Rule: Portfolio Max Drawdown Breach

Alert type:

    DRAWDOWN_BREACH

Trigger condition:

    Portfolio max drawdown is less than or equal to the configured max drawdown threshold.

Example:

    Portfolio max drawdown = -0.25
    Configured threshold = -0.20
    Alert should be generated

Severity:

    HIGH

Scope:

    Portfolio-level

Symbol:

    null

Source endpoint:

    /api/reports/summary

Purpose:

This rule detects whether portfolio losses have exceeded the configured risk tolerance.

Example alert message:

    Portfolio max drawdown breached the configured threshold.

---

## Rule: Portfolio Low Sharpe Ratio

Alert type:

    LOW_SHARPE_RATIO

Trigger condition:

    Portfolio Sharpe ratio is below the configured minimum Sharpe ratio.

Example:

    Portfolio Sharpe ratio = 0.75
    Configured minimum = 1.00
    Alert should be generated

Severity:

    MEDIUM

Scope:

    Portfolio-level

Symbol:

    null

Source endpoint:

    /api/reports/summary

Purpose:

This rule detects whether the portfolio’s risk-adjusted return is below the configured standard.

Example alert message:

    Portfolio Sharpe ratio is below the configured minimum.

---

## Rule: Stale Equity Data

Alert type:

    STALE_EQUITY_DATA

Trigger condition:

    Latest equity data date is older than the configured stale-data threshold.

Example:

    Latest equity date = 2026-05-01
    Current date = 2026-05-08
    Configured stale threshold = 3 days
    Alert should be generated

Severity:

    MEDIUM

Scope:

    Portfolio-level or symbol-level

Symbol:

    null for portfolio-level stale data
    symbol value for future symbol-level stale data

Source endpoint:

    /api/reports/summary

Purpose:

This rule detects whether monitoring data is outdated.

Stale data may indicate that the upstream quant pipeline, C++ market engine, Java import process, or scheduled job failed to refresh properly.

Example alert message:

    Latest portfolio equity data is older than the configured stale-data threshold.

---

## Rule: Empty Report Data

Alert type:

    EMPTY_REPORT_DATA

Trigger condition:

    All major report row counts are zero or missing.

Example checked fields:

    riskMetricRowCount
    backtestResultRowCount
    strategySignalRowCount
    equityCurveRowCount

Severity:

    LOW

Scope:

    Portfolio-level or system-level

Symbol:

    null

Source endpoint:

    /api/reports/summary

Purpose:

This rule detects whether the Java backend report data appears empty.

Empty data may happen before the first import, after a failed import, or when the Java backend is connected to a new empty database.

Example alert message:

    Java backend report summary contains no imported report data.

---

## Future Rule: Symbol Max Drawdown Breach

Alert type:

    DRAWDOWN_BREACH

Trigger condition:

    Symbol max drawdown is less than or equal to the configured max drawdown threshold.

Severity:

    HIGH

Scope:

    Symbol-level

Symbol:

    Returned symbol from Java backend

Source endpoint:

    /api/reports/summary

Purpose:

This rule is used when the Java backend returns symbol-level risk metrics.

The monitor should dynamically evaluate each returned symbol. It should not assume a fixed symbol list.

Example:

    Symbol = MSFT
    Symbol max drawdown = -0.24
    Configured threshold = -0.20
    Alert should be generated for MSFT

---

## Future Rule: Symbol Low Sharpe Ratio

Alert type:

    LOW_SHARPE_RATIO

Trigger condition:

    Symbol Sharpe ratio is below the configured minimum Sharpe ratio.

Severity:

    MEDIUM

Scope:

    Symbol-level

Symbol:

    Returned symbol from Java backend

Source endpoint:

    /api/reports/summary

Purpose:

This rule is used when the Java backend returns symbol-level Sharpe ratio data.

The monitor should evaluate each symbol independently.

Example:

    Symbol = AAPL
    Symbol Sharpe ratio = 0.82
    Configured minimum = 1.00
    Alert should be generated for AAPL

---

## Dynamic Symbol Rule

The monitor should never hard-code symbols.

Do not build rules like:

    EvaluateAaplRule()
    EvaluateMsftRule()
    EvaluateGooglRule()
    EvaluateSpyRule()

Use dynamic evaluation:

    foreach symbol metric returned by Java backend:
      evaluate drawdown rule
      evaluate Sharpe ratio rule
      evaluate stale data rule

This design allows future symbols to be monitored without changing the .NET monitor code.

---

## Duplicate Active Alert Prevention

The alert service should prevent duplicate active alerts.

Duplicate identity:

    alert type + symbol + source endpoint

This means the following are treated separately:

| Alert Type | Symbol | Source Endpoint | Result |
|---|---|---|---|
| `LOW_SHARPE_RATIO` | `AAPL` | `/api/reports/summary` | Original |
| `LOW_SHARPE_RATIO` | `AAPL` | `/api/reports/summary` | Duplicate |
| `LOW_SHARPE_RATIO` | `MSFT` | `/api/reports/summary` | Not duplicate |
| `LOW_SHARPE_RATIO` | `null` | `/api/reports/summary` | Portfolio-level alert, not duplicate of symbol alerts |

Duplicate prevention avoids alert spam while preserving separate alerts for different symbols.

---

## Alert Resolution

Alerts can be resolved through the alert API.

When an alert is resolved:

- `IsActive` becomes false
- `ResolvedAtUtc` is set
- The alert remains in history
- A future recurrence of the same issue can create a new active alert

Alert history should not be deleted during normal operation.

---

## Rule Examples

Example 1: Java backend unavailable

    Backend call:
      GET /api/reports/summary

    Result:
      Connection refused

    Alert:
      Type: PROJECT1_UNAVAILABLE
      Severity: CRITICAL
      Symbol: null
      SourceEndpoint: /api/reports/summary

Example 2: Portfolio low Sharpe ratio

    PortfolioSharpeRatio: 0.70
    MinimumSharpeRatio: 1.00

    Alert:
      Type: LOW_SHARPE_RATIO
      Severity: MEDIUM
      Symbol: null
      SourceEndpoint: /api/reports/summary

Example 3: Portfolio drawdown breach

    PortfolioMaxDrawdown: -0.28
    MaxDrawdownThreshold: -0.20

    Alert:
      Type: DRAWDOWN_BREACH
      Severity: HIGH
      Symbol: null
      SourceEndpoint: /api/reports/summary

Example 4: Future symbol-level low Sharpe ratio

    Symbol: MSFT
    SharpeRatio: 0.76
    MinimumSharpeRatio: 1.00

    Alert:
      Type: LOW_SHARPE_RATIO
      Severity: MEDIUM
      Symbol: MSFT
      SourceEndpoint: /api/reports/summary

---

## Design Notes

Alert rule design principles:

- Rules should be deterministic.
- Rules should be testable without calling real Java backend APIs.
- Rules should use configurable thresholds.
- Rules should not depend on AI.
- Rules should support portfolio-only data.
- Rules should support future symbol-level data.
- Rules should not hard-code ticker symbols.
- Rules should preserve source endpoint information.
- Rules should generate clear human-readable messages.
- Duplicate prevention should use alert type, symbol, and source endpoint.
- System-level and portfolio-level alerts should use a null symbol.
- Symbol-level alerts should store the returned symbol value.