# API Reference

Mega Fintrade Risk Monitor exposes REST APIs for health checks, monitoring status, manual monitoring execution, alert management, and AI integration status.

These APIs are designed for local testing, dashboard support, future automation, and portfolio demonstration.

The service can be accessed directly through .NET local runtime or through Docker Compose.

---

## Base URLs

Local .NET runtime:

    http://localhost:5189

Docker Compose runtime:

    http://localhost:5189

Swagger UI:

    http://localhost:5189/swagger

Dashboard:

    http://localhost:5189/dashboard

The exact local .NET port may vary depending on `launchSettings.json` or terminal output. Docker Compose maps the container to host port `5189` by default.

---

## API Groups

Main API groups:

| Group | Purpose |
|---|---|
| Health API | Basic service health check |
| Monitor API | Current monitoring state and manual monitor execution |
| Alert API | Active, historical, and resolved alert records |
| AI Status API | Future AI advisor availability and disabled-mode status |

---

## Health API

The health API confirms that the .NET monitor application is running.

### GET /api/health

Purpose:

- Confirm the service process is alive
- Support quick local testing
- Support simple container runtime checks
- Confirm that the web API pipeline is reachable

Request:

    GET /api/health

Example curl:

    curl http://localhost:5189/api/health

Expected response:

    {
      "status": "Healthy",
      "service": "Mega Fintrade Risk Monitor"
    }

Actual field names may vary depending on the current controller implementation, but the response should indicate that the service is running.

---

## Monitor API

The monitor API exposes the current monitoring state and allows manual monitoring execution.

Monitoring normally runs through a background worker, but the manual endpoint is useful for testing and demonstration.

---

## GET /api/monitor/status

Purpose:

- Return the latest monitor status
- Show whether the Java backend is reachable
- Show last monitoring run information
- Support dashboard status display
- Help debug Docker connectivity to the Java backend

Request:

    GET /api/monitor/status

Example curl:

    curl http://localhost:5189/api/monitor/status

Expected behavior:

| Java backend state | Expected monitor behavior |
|---|---|
| Java backend running | Status shows reachable or successful monitoring result |
| Java backend unavailable | Status shows unavailable or failed backend check |
| No previous run | Status should still return a valid response |

Example response shape:

    {
      "isJavaBackendReachable": true,
      "lastRunAtUtc": "2026-05-08T18:30:00Z",
      "lastRunSucceeded": true,
      "message": "Monitoring completed successfully."
    }

If the Java backend is unavailable, an example response may look like:

    {
      "isJavaBackendReachable": false,
      "lastRunAtUtc": "2026-05-08T18:30:00Z",
      "lastRunSucceeded": false,
      "message": "Java backend is unavailable."
    }

The monitor should not crash when the Java backend is unavailable.

---

## POST /api/monitor/run

Purpose:

- Manually trigger a monitoring cycle
- Test Java backend connectivity
- Evaluate alert rules on demand
- Create new alert records when rule conditions are met
- Support dashboard or Postman manual testing

Request:

    POST /api/monitor/run

Example curl:

    curl -X POST http://localhost:5189/api/monitor/run

Expected behavior:

| Situation | Expected result |
|---|---|
| Java backend is reachable | Fetch Java backend data, evaluate rules, save alerts if needed |
| Java backend is unavailable | Handle failure, report unavailable state, avoid crashing |
| Duplicate active alert exists | Do not create duplicate active alert |
| New alert condition exists | Store new alert in SQLite |

Example response shape:

    {
      "startedAtUtc": "2026-05-08T18:30:00Z",
      "completedAtUtc": "2026-05-08T18:30:02Z",
      "success": true,
      "alertsCreated": 1,
      "message": "Manual monitoring run completed."
    }

If the Java backend is unavailable:

    {
      "startedAtUtc": "2026-05-08T18:30:00Z",
      "completedAtUtc": "2026-05-08T18:30:01Z",
      "success": false,
      "alertsCreated": 1,
      "message": "Java backend unavailable. Monitoring run completed with system alert."
    }

---

## Alert API

Alert APIs expose active alerts, alert history, individual alert records, and alert resolution behavior.

Alerts are stored in SQLite.

Alert records can represent:

- System-level issues
- Portfolio-level risk issues
- Future symbol-level risk issues

---

## Alert Model

Common alert fields:

| Field | Purpose |
|---|---|
| `id` | Alert record identifier |
| `symbol` | Nullable symbol field; null means system-level or portfolio-level alert |
| `type` | Alert type, such as `LOW_SHARPE_RATIO` |
| `severity` | Alert severity, such as `MEDIUM` or `HIGH` |
| `message` | Human-readable alert message |
| `sourceEndpoint` | Java backend endpoint or monitor source that produced the alert |
| `sourceValue` | Value that triggered the alert |
| `thresholdValue` | Configured threshold used by the rule |
| `isActive` | Whether the alert is unresolved |
| `createdAtUtc` | Alert creation time |
| `resolvedAtUtc` | Alert resolution time, if resolved |

Example alert:

    {
      "id": 1,
      "symbol": null,
      "type": "LOW_SHARPE_RATIO",
      "severity": "MEDIUM",
      "message": "Portfolio Sharpe ratio is below the configured minimum.",
      "sourceEndpoint": "/api/reports/summary",
      "sourceValue": "0.75",
      "thresholdValue": "1.00",
      "isActive": true,
      "createdAtUtc": "2026-05-08T18:30:00Z",
      "resolvedAtUtc": null
    }

Example future symbol-level alert:

    {
      "id": 2,
      "symbol": "AAPL",
      "type": "DRAWDOWN_BREACH",
      "severity": "HIGH",
      "message": "AAPL max drawdown breached the configured threshold.",
      "sourceEndpoint": "/api/reports/summary",
      "sourceValue": "-0.25",
      "thresholdValue": "-0.20",
      "isActive": true,
      "createdAtUtc": "2026-05-08T18:35:00Z",
      "resolvedAtUtc": null
    }

---

## GET /api/alerts

Purpose:

- Return all alert records
- Include active and resolved alerts
- Support dashboard alert tables
- Support API testing through Swagger, curl, or Postman

Request:

    GET /api/alerts

Example curl:

    curl http://localhost:5189/api/alerts

Example response:

    [
      {
        "id": 1,
        "symbol": null,
        "type": "LOW_SHARPE_RATIO",
        "severity": "MEDIUM",
        "message": "Portfolio Sharpe ratio is below the configured minimum.",
        "sourceEndpoint": "/api/reports/summary",
        "sourceValue": "0.75",
        "thresholdValue": "1.00",
        "isActive": true,
        "createdAtUtc": "2026-05-08T18:30:00Z",
        "resolvedAtUtc": null
      }
    ]

Optional future filter:

    GET /api/alerts?symbol=AAPL

Purpose of optional symbol filter:

- Return alerts for one symbol
- Support dynamic symbol-level monitoring
- Keep API flexible without creating ticker-specific endpoints

---

## GET /api/alerts/active

Purpose:

- Return unresolved active alerts only
- Support dashboard active alert section
- Help users focus on current issues

Request:

    GET /api/alerts/active

Example curl:

    curl http://localhost:5189/api/alerts/active

Example response:

    [
      {
        "id": 1,
        "symbol": null,
        "type": "PROJECT1_UNAVAILABLE",
        "severity": "CRITICAL",
        "message": "Mega Fintrade Backend Java is unavailable.",
        "sourceEndpoint": "/api/reports/summary",
        "sourceValue": "Connection failed",
        "thresholdValue": null,
        "isActive": true,
        "createdAtUtc": "2026-05-08T18:30:00Z",
        "resolvedAtUtc": null
      }
    ]

Optional future filter:

    GET /api/alerts/active?symbol=AAPL

---

## GET /api/alerts/history

Purpose:

- Return resolved and historical alerts
- Support auditability
- Preserve monitoring history after alerts are resolved

Request:

    GET /api/alerts/history

Example curl:

    curl http://localhost:5189/api/alerts/history

Example response:

    [
      {
        "id": 3,
        "symbol": null,
        "type": "IMPORT_FAILURE",
        "severity": "HIGH",
        "message": "Latest Java backend import job failed.",
        "sourceEndpoint": "/api/import/audit",
        "sourceValue": "FAILED",
        "thresholdValue": null,
        "isActive": false,
        "createdAtUtc": "2026-05-08T17:00:00Z",
        "resolvedAtUtc": "2026-05-08T17:30:00Z"
      }
    ]

---

## GET /api/alerts/{id}

Purpose:

- Return one alert by identifier
- Support detailed inspection from dashboard or API client
- Useful when debugging a specific alert condition

Request:

    GET /api/alerts/{id}

Example curl:

    curl http://localhost:5189/api/alerts/1

Example response:

    {
      "id": 1,
      "symbol": null,
      "type": "LOW_SHARPE_RATIO",
      "severity": "MEDIUM",
      "message": "Portfolio Sharpe ratio is below the configured minimum.",
      "sourceEndpoint": "/api/reports/summary",
      "sourceValue": "0.75",
      "thresholdValue": "1.00",
      "isActive": true,
      "createdAtUtc": "2026-05-08T18:30:00Z",
      "resolvedAtUtc": null
    }

Expected not-found behavior:

    HTTP 404 Not Found

---

## POST /api/alerts/{id}/resolve

Purpose:

- Mark an active alert as resolved
- Keep the alert in history
- Allow a future recurrence of the same issue to create a new active alert

Request:

    POST /api/alerts/{id}/resolve

Example curl:

    curl -X POST http://localhost:5189/api/alerts/1/resolve

Expected behavior:

| Existing alert state | Expected result |
|---|---|
| Active alert exists | Mark as resolved |
| Alert already resolved | Return existing resolved state or no-op success depending on implementation |
| Alert does not exist | Return not found |

Example response:

    {
      "id": 1,
      "symbol": null,
      "type": "LOW_SHARPE_RATIO",
      "severity": "MEDIUM",
      "message": "Portfolio Sharpe ratio is below the configured minimum.",
      "sourceEndpoint": "/api/reports/summary",
      "sourceValue": "0.75",
      "thresholdValue": "1.00",
      "isActive": false,
      "createdAtUtc": "2026-05-08T18:30:00Z",
      "resolvedAtUtc": "2026-05-08T18:45:00Z"
    }

---

## Optional DELETE /api/alerts/{id}

Purpose:

- Remove an alert record if cleanup behavior is implemented
- This endpoint is optional
- Normal operation should prefer resolving alerts instead of deleting them

Request:

    DELETE /api/alerts/{id}

Example curl:

    curl -X DELETE http://localhost:5189/api/alerts/1

Expected behavior:

| Situation | Expected result |
|---|---|
| Delete endpoint implemented | Alert is deleted or not found |
| Delete endpoint not implemented | Endpoint may return 404 or 405 |

Recommended production-style behavior:

- Resolve alerts instead of deleting them
- Keep alert history for auditability
- Use delete only for development cleanup or explicit maintenance

---

## AI Status API

AI integration is optional.

Mega Fintrade Risk Monitor should work normally when AI is disabled or unavailable.

The .NET monitor does not store API tokens for Gemini, Grok, OpenAI, Ollama, or any future AI provider.

---

## GET /api/ai/status

Purpose:

- Return AI integration status
- Confirm whether the future AI advisor is enabled
- Confirm whether the monitor is operating in disabled AI mode
- Support dashboard AI panel

Request:

    GET /api/ai/status

Example curl:

    curl http://localhost:5189/api/ai/status

Example disabled-mode response:

    {
      "enabled": false,
      "available": false,
      "message": "AI integration is disabled."
    }

Example future enabled response:

    {
      "enabled": true,
      "available": true,
      "project5BaseUrl": "http://host.docker.internal:7005",
      "message": "AI advisor is available."
    }

The AI advisor service is outside this .NET monitor project.

---

## Java Backend Dependency Behavior

The monitor depends on these Java backend endpoints:

| Java Backend Endpoint | Used For |
|---|---|
| `/api/reports/summary` | Portfolio and future symbol-level risk summary |
| `/api/import/audit` | Import status and failure detection |
| `/api/import/rejections` | CSV rejection detection |

If the Java backend is unavailable, the monitor should:

- Continue running
- Return monitor status
- Avoid crashing the dashboard
- Generate or expose a backend unavailable condition
- Allow the user to inspect the problem through APIs and dashboard

---

## Docker API Testing

Start the monitor:

    docker compose up --build

Test health:

    curl http://localhost:5189/api/health

Test monitor status:

    curl http://localhost:5189/api/monitor/status

Manually run monitoring:

    curl -X POST http://localhost:5189/api/monitor/run

Test AI status:

    curl http://localhost:5189/api/ai/status

Test all alerts:

    curl http://localhost:5189/api/alerts

Test active alerts:

    curl http://localhost:5189/api/alerts/active

Test alert history:

    curl http://localhost:5189/api/alerts/history

Stop the monitor:

    docker compose down

---

## Local .NET API Testing

Run the application:

    dotnet run --project MegaFintradeRiskMonitor.csproj

Test health:

    curl http://localhost:5189/api/health

Test monitor status:

    curl http://localhost:5189/api/monitor/status

Manually run monitoring:

    curl -X POST http://localhost:5189/api/monitor/run

Test AI status:

    curl http://localhost:5189/api/ai/status

Test all alerts:

    curl http://localhost:5189/api/alerts

---

## Expected HTTP Status Codes

Common expected status codes:

| Status Code | Meaning |
|---|---|
| 200 OK | Request succeeded |
| 201 Created | Resource created, if used by implementation |
| 204 No Content | Request succeeded with no body, if used by implementation |
| 400 Bad Request | Invalid request |
| 404 Not Found | Requested alert or route does not exist |
| 405 Method Not Allowed | HTTP method is not supported for the route |
| 500 Internal Server Error | Unexpected server error |

Backend unavailability should preferably be represented through monitor status and alerts, not by crashing the .NET service.

---

## Swagger Usage

Swagger UI is available at:

    http://localhost:5189/swagger

Swagger can be used to:

- Inspect all available API routes
- Test monitor status
- Trigger manual monitoring
- View alert API responses
- Resolve alerts
- Check AI integration status

If an endpoint name changes in code, Swagger should be treated as the current source for the exact route and response shape.

---

## Alert API Design Notes

Alert API design principles:

- Alerts should be queryable.
- Active alerts should be easy to separate from history.
- Resolving an alert should not delete it.
- Alert history should remain available.
- System-level alerts should use a null symbol.
- Portfolio-level alerts should use a null symbol.
- Symbol-level alerts should store the returned symbol value.
- Duplicate active alerts should be prevented by alert type, symbol, and source endpoint.
- Future symbol filtering can use query parameters instead of ticker-specific endpoints.

---

## API Design Notes

General API design principles:

- Keep Java backend calls inside the client layer.
- Keep monitor APIs focused on monitoring state and alerts.
- Keep AI status separate from alert rule decisions.
- Do not expose provider tokens through API responses.
- Keep responses clear enough for dashboard and Postman testing.
- Keep the service operational even when Java backend or future AI advisor is unavailable.