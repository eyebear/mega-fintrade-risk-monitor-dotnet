# Dynamic Symbol Compatibility

Mega Fintrade Risk Monitor is designed to support both current portfolio-level monitoring and future symbol-level monitoring.

The monitor must not hard-code symbols such as AAPL, MSFT, GOOGL, or SPY. It should consume whatever symbols the Mega Fintrade Backend Java service returns.

This design keeps the .NET monitor compatible with changes in the upstream quantitative pipeline and Java backend contract.

---

## Purpose

The purpose of dynamic symbol compatibility is to prevent the monitor from depending on a fixed list of tickers.

The list of symbols may change over time because symbol selection belongs upstream to:

- Mega Fintrade Quant Engine
- Mega Fintrade Market Engine C++
- Mega Fintrade Backend Java

The .NET monitor should not decide which symbols exist.

The .NET monitor should only consume the symbols returned by the Java backend and evaluate rules against those returned values.

---

## Current Backend Behavior

The current Java backend may return only portfolio-level report summary data.

In that case, the .NET monitor should evaluate:

- Java backend availability
- Import failure
- CSV rejections
- Portfolio max drawdown
- Portfolio Sharpe ratio
- Portfolio stale data
- Empty report data

The .NET monitor should skip symbol-level rules when no symbol-level data exists.

Missing symbol-level data is not an error.

---

## Future Backend Behavior

A future Java backend version may return symbol-level metrics in addition to portfolio-level metrics.

In that case, the .NET monitor should evaluate:

- Portfolio-level rules
- Symbol-level rules for each returned symbol

The monitor should not require code changes when symbols are added, removed, or replaced.

Example future symbols:

    AAPL
    MSFT
    SPY
    NVDA
    TSLA
    QQQ

The actual symbol list is not controlled by the .NET monitor.

---

## Design Rule

Do not write logic like this:

    if symbol == "AAPL":
      evaluate AAPL rules

    if symbol == "MSFT":
      evaluate MSFT rules

    if symbol == "GOOGL":
      evaluate GOOGL rules

    if symbol == "SPY":
      evaluate SPY rules

Use dynamic iteration instead:

    for each symbol metric returned by Java backend:
      evaluate Sharpe ratio rule
      evaluate max drawdown rule
      evaluate stale data rule

This allows the monitor to support any returned symbol without code changes.

---

## Portfolio-Level DTO Compatibility

The report summary DTO should support portfolio-level metrics even when no symbols are returned.

Portfolio-level fields may include:

| Field | Purpose |
|---|---|
| `PortfolioSharpeRatio` | Used by portfolio low Sharpe ratio rule |
| `PortfolioMaxDrawdown` | Used by portfolio drawdown breach rule |
| `LatestEquityDate` | Used by stale equity data rule |
| `RiskMetricRowCount` | Used by empty report data rule |
| `BacktestResultRowCount` | Used by empty report data rule |
| `StrategySignalRowCount` | Used by empty report data rule |
| `EquityCurveRowCount` | Used by empty report data rule |

If the symbols collection is missing, null, or empty, the monitor should continue normally.

---

## Future Symbol-Level DTO Compatibility

The report summary DTO should also support a future symbols collection.

Symbol-level fields may include:

| Field | Purpose |
|---|---|
| `Symbol` | Symbol name returned by the Java backend |
| `SharpeRatio` | Used by symbol low Sharpe ratio rule |
| `MaxDrawdown` | Used by symbol drawdown breach rule |
| `LatestDataDate` | Used by symbol stale data rule |

Example conceptual structure:

    PortfolioSharpeRatio: 1.15
    PortfolioMaxDrawdown: -0.12
    LatestEquityDate: 2026-05-07
    RiskMetricRowCount: 120
    BacktestResultRowCount: 400
    StrategySignalRowCount: 800
    EquityCurveRowCount: 250
    Symbols:
      - Symbol: AAPL
        SharpeRatio: 1.22
        MaxDrawdown: -0.10
        LatestDataDate: 2026-05-07
      - Symbol: MSFT
        SharpeRatio: 0.82
        MaxDrawdown: -0.18
        LatestDataDate: 2026-05-07

The monitor should process both symbols without needing symbol-specific code.

---

## Alert Symbol Field

The alert model should support a nullable symbol field.

| Symbol value | Meaning |
|---|---|
| `null` | System-level or portfolio-level alert |
| Non-null symbol | Symbol-level alert |

Examples:

| Alert Type | Symbol | Meaning |
|---|---|---|
| `PROJECT1_UNAVAILABLE` | `null` | Java backend unavailable |
| `IMPORT_FAILURE` | `null` | Import job failed |
| `CSV_REJECTIONS_FOUND` | `null` | CSV rejected rows exist |
| `LOW_SHARPE_RATIO` | `null` | Portfolio Sharpe ratio is low |
| `LOW_SHARPE_RATIO` | `AAPL` | AAPL Sharpe ratio is low |
| `DRAWDOWN_BREACH` | `MSFT` | MSFT max drawdown breached threshold |

This lets the same alert type work for both portfolio-level and symbol-level alerts.

---

## Duplicate Alert Prevention

Duplicate active alerts should use this identity:

    alert type + symbol + source endpoint

This is important because symbol-level alerts should not block each other.

Example:

| Alert Type | Symbol | Source Endpoint | Duplicate? |
|---|---|---|---|
| `LOW_SHARPE_RATIO` | `AAPL` | `/api/reports/summary` | Original |
| `LOW_SHARPE_RATIO` | `AAPL` | `/api/reports/summary` | Duplicate |
| `LOW_SHARPE_RATIO` | `MSFT` | `/api/reports/summary` | Not duplicate |
| `LOW_SHARPE_RATIO` | `null` | `/api/reports/summary` | Portfolio-level alert, separate from symbol alerts |

If duplicate detection used only alert type, then an AAPL alert could incorrectly block an MSFT alert.

That is why symbol must be part of duplicate detection.

---

## Dashboard Compatibility

The dashboard should not create hard-coded ticker cards.

Avoid dashboard layouts like:

    AAPL card
    MSFT card
    GOOGL card
    SPY card

Use a dynamic symbol risk table instead:

| Symbol | Sharpe Ratio | Max Drawdown | Latest Data Date | Alert Status |
|---|---:|---:|---|---|
| AAPL | 1.25 | -0.12 | 2026-05-07 | Normal |
| MSFT | 0.82 | -0.18 | 2026-05-07 | Low Sharpe |
| SPY | 1.10 | -0.09 | 2026-05-07 | Normal |

If symbol-level metrics are unavailable, the dashboard should show a clear message:

    Symbol-level metrics are not available from the Java backend yet.
    Portfolio-level monitoring is active.

This makes the current backend behavior understandable without making the dashboard look broken.

---

## Rule Engine Compatibility

The alert rule engine should evaluate system-level and portfolio-level rules first.

Then it should evaluate symbol-level rules only when symbol data exists.

Conceptual flow:

    evaluate system-level rules

    evaluate portfolio-level rules

    if summary symbols exist:
      for each symbol:
        evaluate symbol drawdown rule
        evaluate symbol Sharpe ratio rule
        evaluate symbol stale data rule

If the symbols list is empty, the symbol rule loop should simply do nothing.

Empty symbol data should not throw an exception.

---

## API Compatibility

API responses should be able to include alerts with or without symbols.

Example portfolio-level alert:

    {
      "type": "LOW_SHARPE_RATIO",
      "symbol": null,
      "severity": "MEDIUM",
      "sourceEndpoint": "/api/reports/summary"
    }

Example symbol-level alert:

    {
      "type": "LOW_SHARPE_RATIO",
      "symbol": "AAPL",
      "severity": "MEDIUM",
      "sourceEndpoint": "/api/reports/summary"
    }

The API should not require separate endpoints for each ticker.

Optional future filtering can use query parameters:

    /api/alerts?symbol=AAPL
    /api/alerts/active?symbol=AAPL

This keeps the API flexible and simple.

---

## Configuration Compatibility

Alert thresholds should be independent of symbol names.

Example:

    AlertRules:
      MaxDrawdownThreshold: -0.20
      MinimumSharpeRatio: 1.0
      StaleDataDays: 3
      CsvRejectionThreshold: 0

These thresholds apply to:

- Portfolio-level metrics
- Future symbol-level metrics

The configuration should not contain hard-coded ticker-specific settings unless a future feature explicitly requires per-symbol thresholds.

Current design uses shared thresholds for all symbols.

---

## Testing Expectations

Dynamic symbol compatibility should be tested with at least two backend response shapes.

Test 1: Portfolio-only response

    PortfolioSharpeRatio: 0.75
    PortfolioMaxDrawdown: -0.10
    Symbols: empty

Expected behavior:

    Portfolio-level rules run.
    Symbol-level rules are skipped.
    No exception is thrown.

Test 2: Portfolio plus symbol-level response

    PortfolioSharpeRatio: 1.20
    PortfolioMaxDrawdown: -0.08
    Symbols:
      - AAPL
      - MSFT

Expected behavior:

    Portfolio-level rules run.
    AAPL symbol rules run.
    MSFT symbol rules run.
    Alerts for AAPL and MSFT are treated separately.

Test 3: Different symbols with same alert type

    AAPL LOW_SHARPE_RATIO
    MSFT LOW_SHARPE_RATIO

Expected behavior:

    Both alerts can exist because their symbols are different.

---

## Why This Matters

Dynamic symbol compatibility matters because Mega Fintrade is a multi-service platform.

Symbol selection may change upstream for many reasons:

- New strategy universe
- New portfolio allocation model
- New market data source
- New backtest configuration
- New Java backend report contract
- Future user-selected symbols
- Future AI advisor recommendations

If the .NET monitor hard-coded symbols, each upstream change would require monitor code changes.

With dynamic symbol compatibility, the monitor stays stable while upstream services evolve.

---

## Design Notes

Dynamic symbol compatibility principles:

- Do not hard-code ticker names.
- Treat symbol-level data as optional.
- Evaluate portfolio-level rules even when no symbol data exists.
- Evaluate symbol-level rules only when symbol data exists.
- Store null symbol for system-level and portfolio-level alerts.
- Store returned symbol value for symbol-level alerts.
- Use alert type, symbol, and source endpoint for duplicate detection.
- Render symbol-level dashboard data as a dynamic table.
- Keep thresholds independent from symbol names.
- Keep Java backend as the source of returned symbol data.