# Docker Usage

Mega Fintrade Risk Monitor supports Docker runtime packaging for local testing, portfolio demonstration, and future deployment preparation.

Docker support allows the .NET monitor to run in a repeatable container environment while still connecting to the Mega Fintrade Backend Java service running on the host machine or, in a future setup, in the same Docker network.

---

## Docker Files

Docker-related files:

| File | Purpose |
|---|---|
| `Dockerfile` | Builds and packages the ASP.NET Core risk monitor |
| `.dockerignore` | Keeps the Docker build context clean |
| `docker-compose.yml` | Runs the monitor container locally |
| `appsettings.Docker.json` | Stores Docker-specific application settings |
| `.env.example` | Documents optional local Docker environment overrides |

These files allow the monitor to be built and run without changing the normal local `.NET` development workflow.

---

## Runtime Ports

Default local URLs:

| Service | Browser URL |
|---|---|
| Risk Monitor dashboard/API | `http://localhost:5189` |
| Dashboard | `http://localhost:5189/dashboard` |
| Swagger | `http://localhost:5189/swagger` |
| Java backend on host Mac | `http://localhost:8080` |
| Future AI advisor on host Mac | `http://localhost:7005` |

The Docker container listens internally on:

    8080

Docker Compose maps it to the host machine as:

    5189:8080

This means:

- Browser uses `localhost:5189`
- Container listens on `8080`
- Docker Compose connects the two

---

## Recommended Docker Command

From the project root, run:

    docker compose up --build

This command:

- Builds the image if needed
- Starts the risk monitor container
- Maps host port `5189` to container port `8080`
- Uses Docker-specific environment settings
- Mounts the SQLite data directory to a named Docker volume

Open the dashboard:

    http://localhost:5189/dashboard

Open Swagger:

    http://localhost:5189/swagger

Stop the container:

    docker compose down

---

## Docker Build Only

To build the Docker image without starting the container:

    docker build -t mega-fintrade-risk-monitor-dotnet .

Confirm the image exists:

    docker images | grep mega-fintrade-risk-monitor-dotnet

Build through Docker Compose:

    docker compose build

Expected image name:

    mega-fintrade-risk-monitor-dotnet:latest

---

## Docker Compose Service

The Docker Compose service name is:

    mega-fintrade-risk-monitor

The container name is:

    mega-fintrade-risk-monitor

Check running containers:

    docker ps

Expected result should include:

    mega-fintrade-risk-monitor

Check logs:

    docker logs mega-fintrade-risk-monitor

Follow logs live:

    docker logs -f mega-fintrade-risk-monitor

---

## Java Backend URL Rule

The Java backend URL depends on where the Java backend is running.

### Case 1: Java backend runs directly on the host machine

Browser URL:

    http://localhost:8080

URL from inside the .NET monitor container:

    http://host.docker.internal:8080

This is the default Docker configuration.

Reason:

- Inside a Docker container, `localhost` means the container itself.
- It does not mean the host Mac.
- To reach a service running directly on the host machine, Docker uses `host.docker.internal`.

### Case 2: Java backend runs in the same Docker Compose network

Future container-to-container URL:

    http://portfolio-backend:8080

This is not the current default.

This would be used later if the Java backend is added to the same Docker Compose file as a service named `portfolio-backend`.

---

## Docker Configuration

Docker-specific settings are stored in:

    appsettings.Docker.json

Important Docker settings:

| Setting | Docker value | Purpose |
|---|---|---|
| `ConnectionStrings:RiskMonitorDatabase` | `Data Source=/app/data/risk-monitor.db` | Stores SQLite database inside container data directory |
| `JavaBackendApi:BaseUrl` | `http://host.docker.internal:8080` | Connects to Java backend running on host machine |
| `JavaBackendApi:TimeoutSeconds` | `10` | Java backend request timeout |
| `AiIntegration:Enabled` | `false` | Keeps AI disabled by default |
| `AiIntegration:Project5BaseUrl` | `http://host.docker.internal:7005` | Reserves future AI advisor URL |
| `Monitoring:PollingIntervalSeconds` | `60` | Background monitor interval |

The Dockerfile sets:

    ASPNETCORE_ENVIRONMENT=Docker

This tells ASP.NET Core to load:

    appsettings.Docker.json

Local non-Docker development should continue using:

    appsettings.json
    appsettings.Development.json

---

## Environment Variables

Docker Compose provides environment variables that can override configuration values.

The Compose file includes defaults, so `.env` is optional.

To create a local `.env` file:

    cp .env.example .env

Edit it:

    code .env

Do not commit `.env`.

Important variables:

| Variable | Default | Purpose |
|---|---|---|
| `RISK_MONITOR_HOST_PORT` | `5189` | Host port used by browser |
| `RISK_MONITOR_CONTAINER_PORT` | `8080` | Internal container port |
| `RISK_MONITOR_DATABASE` | `Data Source=/app/data/risk-monitor.db` | SQLite database path |
| `JAVA_BACKEND_BASE_URL` | `http://host.docker.internal:8080` | Java backend URL from inside Docker |
| `JAVA_BACKEND_TIMEOUT_SECONDS` | `10` | Java backend request timeout |
| `AI_INTEGRATION_ENABLED` | `false` | Keeps future AI advisor disabled |
| `AI_ADVISOR_BASE_URL` | `http://host.docker.internal:7005` | Future Project 5 AI advisor URL |
| `MONITORING_POLLING_INTERVAL_SECONDS` | `60` | Background monitor interval |

---

## SQLite Persistence

The monitor uses SQLite for alert and monitoring data.

Inside the container, SQLite data is stored at:

    /app/data/risk-monitor.db

Docker Compose maps this directory to a named volume:

    mega-fintrade-risk-monitor-sqlite-data

This volume keeps alert data after the container is removed.

Normal stop command:

    docker compose down

This removes the container but keeps the SQLite volume.

Do not use this unless you intentionally want to delete saved SQLite data:

    docker compose down -v

The `-v` option removes Docker volumes.

Check the volume:

    docker volume ls | grep mega-fintrade-risk-monitor

Expected volume:

    mega-fintrade-risk-monitor-sqlite-data

---

## Basic Runtime Test

Start the app:

    docker compose up --build

Open dashboard:

    http://localhost:5189/dashboard

Open Swagger:

    http://localhost:5189/swagger

Test health endpoint:

    curl http://localhost:5189/api/health

Test monitor status:

    curl http://localhost:5189/api/monitor/status

Test AI status:

    curl http://localhost:5189/api/ai/status

Test alerts:

    curl http://localhost:5189/api/alerts

Manually run monitoring:

    curl -X POST http://localhost:5189/api/monitor/run

Stop the app:

    docker compose down

---

## Expected Behavior Without Java Backend

The .NET monitor should start even if the Java backend is not running.

If the Java backend is unavailable, expected behavior is:

- Dashboard still loads
- Swagger still loads
- Health endpoint still responds
- AI status endpoint still responds
- Monitor status may report Java backend unavailable
- Manual monitor run should not crash the application
- A backend unavailable alert may be created

This behavior is correct.

Java backend unavailable means the upstream dependency is not reachable. It does not mean the .NET monitor container failed.

---

## Expected Behavior With Java Backend Running

Start the Java backend on the host machine first.

Expected host URL:

    http://localhost:8080

Then start the .NET monitor:

    docker compose up --build

The .NET monitor container should reach Java backend through:

    http://host.docker.internal:8080

Expected behavior:

- Monitor status shows Java backend reachable
- Manual monitoring can fetch Java backend data
- Report summary can be evaluated
- Import audit can be checked
- CSV rejections can be checked
- Alert rules can generate alerts if thresholds are breached

---

## AI Advisor Behavior

AI advisor support is reserved for future Project 5.

Current Docker default:

    AI_INTEGRATION_ENABLED=false

Future AI advisor URL:

    http://host.docker.internal:7005

Expected current behavior:

- AI panel shows disabled or unavailable
- `/api/ai/status` returns disabled status
- Core monitoring still works
- No AI provider token is needed

The .NET monitor should not store:

- Gemini API keys
- Grok API keys
- OpenAI API keys
- Ollama provider configuration
- Any future AI provider token

Provider selection and API token management belong to the future AI advisor service.

---

## Common Commands

Start with rebuild:

    docker compose up --build

Start in detached mode:

    docker compose up --build -d

Stop:

    docker compose down

Stop and remove volume:

    docker compose down -v

View running containers:

    docker ps

View all containers:

    docker ps -a

View logs:

    docker logs mega-fintrade-risk-monitor

Follow logs:

    docker logs -f mega-fintrade-risk-monitor

Rebuild image only:

    docker compose build

Remove unused Docker build cache:

    docker builder prune

Remove unused Docker resources:

    docker system prune

---

## Troubleshooting

### Dashboard does not open

Check whether the container is running:

    docker ps

Check logs:

    docker logs mega-fintrade-risk-monitor

Confirm the port mapping:

    0.0.0.0:5189->8080/tcp

Open:

    http://localhost:5189/dashboard

If port `5189` is already used by another process, change:

    RISK_MONITOR_HOST_PORT

in `.env`.

---

### Docker build fails during restore

Run:

    docker build -t mega-fintrade-risk-monitor-dotnet .

If restore fails, check:

- `MegaFintradeRiskMonitor.csproj` exists at repo root
- Dockerfile restores the main project file
- `.dockerignore` is not excluding required source files
- Docker Desktop is running
- Internet access is available for NuGet restore

---

### Java backend is unavailable in dashboard

Check whether Java backend is running on host:

    http://localhost:8080

In Docker mode, the .NET monitor must use:

    http://host.docker.internal:8080

Check:

- `appsettings.Docker.json`
- `docker-compose.yml`
- `.env` if you created one

The value should usually be:

    JAVA_BACKEND_BASE_URL=http://host.docker.internal:8080

---

### SQLite database is not persisting

Check that the Docker volume exists:

    docker volume ls | grep mega-fintrade-risk-monitor

Confirm Compose includes:

    risk-monitor-sqlite-data:/app/data

Do not use:

    docker compose down -v

unless you want to delete the saved SQLite data.

---

### AI status shows disabled

This is expected.

AI is disabled by default because Project 5 is not required for Project 4 to run.

Core monitoring and alerting should still work normally.

---

### Container keeps restarting

Check logs:

    docker logs mega-fintrade-risk-monitor

Common causes:

- Invalid configuration
- SQLite directory permission issue
- Port conflict
- Application startup exception
- Missing required appsettings file

The container should not crash just because the Java backend is unavailable.

If it does, that indicates an application error that should be fixed.

---

## Clean Rebuild Procedure

Use this when Docker behavior seems stale.

Stop container:

    docker compose down

Rebuild image:

    docker compose build --no-cache

Start again:

    docker compose up

Check logs:

    docker logs -f mega-fintrade-risk-monitor

This keeps the SQLite volume unless you explicitly use `-v`.

---

## Full Reset Procedure

Use this only when you intentionally want to remove the container and SQLite volume.

Stop and remove container plus volume:

    docker compose down -v

Rebuild:

    docker compose build --no-cache

Start:

    docker compose up

Warning:

    docker compose down -v

deletes the named SQLite volume and removes saved alert data.

---

## Docker Design Notes

Docker design principles:

- Docker runtime should not break local `dotnet run`.
- Docker should use `appsettings.Docker.json`.
- Docker should use `host.docker.internal` when Java backend runs on the host machine.
- SQLite data should persist through a named Docker volume.
- AI advisor configuration should be optional and disabled by default.
- The monitor should start even when Java backend is unavailable.
- The dashboard and APIs should remain available in Docker mode.
- Docker Compose should provide a simple repeatable runtime command.
- The project should remain free to run locally during development.