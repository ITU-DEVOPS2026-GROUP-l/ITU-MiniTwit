# ITU MiniTwit / Chirp

ITU Minitwit / CHirp is a .NET 8 ASP.NET Core Razor Pages implementation application. Users can register, log in, post short messages called cheeps, browse other users cheeps, follow other users, and like or dislike cheeps. The application also exposes MiniTwit API endpoints for the simulator and a full monitoring setup using Prometheus for metrics, Promtail for collecting logs from containers, Loki for log storage and Grafana for log and metric dashboard.

The solution is organized as a layered application, where the ASP.NET application uses Onion Layered Structure:

- `src/Chirp.Core` contains domain models and repository interfaces.
- `src/Chirp.Application` contains application services and DTOs.
- `src/Chirp.Infrastructure` contains Entity Framework Core contexts, migrations, repositories, and infrastructure services.
- `src/Chirp.Web` contains Razor Pages, controllers, static assets (wwwroot files), startup configuration and authentication.
- `test/` contains unit, integration, UI, and end-to-end tests.
- `Monitoring/` contains Prometheus, Grafana, Loki, and Promtail configuration.
- `remote_files/` contains the production Docker Compose, Nginx, and deployment script used by CI/CD.

## Requirements

- .NET SDK 8.0.419 or a compatible .NET 8 SDK.
- Docker, for local PostgreSQL.

## Local Development

Restore and build the solution:

```powershell
dotnet restore
dotnet build
```

Run the web app:

```powershell
dotnet run --project src/Chirp.Web/Chirp.Web.csproj
```
By default, development configuration points to PostgreSQL on localhost port `5433` and falls back to the legacy SQLite database when PostgreSQL is not reachable. To start the local PostgreSQL database:

```powershell
docker compose -f docker-compose.postgres.yml up -d
dotnet run --project src/Chirp.Web/Chirp.Web.csproj
```

The development PostgreSQL connection string is configured in `src/Chirp.Web/appsettings.Development.json`:

SQLite uses `src/Chirp.Web/data/chirp.db` in development and `/app/data/chirp.db` in production-style containers.

## Provisioning
The production environment can be visited at: Minitwit.me

Before running these commands, please set your environment variables as such:
$env:DIGITAL_OCEAN_TOKEN="<insert-digital-ocean-api-key>"
$env:DOCKER_USERNAME="officialchutney"
$env:DOCKER_PASSWORD="<insert-dockerhub-password>"

For provisioning of a DigitalOcean droplet we use Vagrant and have written a vagrant file. To make use of this run the following commands:

``` powershell
cd /ITU-MiniTwit/
vagrant up minitwit-demo --provider=digital_ocean
```
If this process stalls use CTRL-C to interrupt the process and then run 
```powershell
vagrant provision minitwit-demo
```
When this process has run, connect to the droplet using
```powershell
vagrant ssh
```

When connected to the droplet, please run the following commands:
```
cd /minitwit
sed -i 's/\r$//' deploy.sh docker-compose.yml nginx.conf
chmod +x deploy.sh

read -rsp "DB password: " DB_PASSWORD
echo

cat > .env <<EOF
DOCKER_USERNAME=officialchutney
IMAGE_TAG=latest
CHIRP_DB_CONNECTION='<Insert DB connectionstring>'
EOF

chmod 600 .env

docker compose pull nginx minitwit
docker compose up -d --remove-orphans --scale minitwit=2 minitwit nginx
```
You will be asked for tha password for the database, which you will have to enter.

For inspecting the status of the server run
```powershell
docker compose logs --tail=80 minitwit
```

Now the droplet should be running and available and you can inspect it via the link provided on the server, or DigitalOcean.
## Testing

Run all tests:

```powershell
dotnet test
```

The Playwright test project requires browser binaries. After building the Playwright project, install them with:

```powershell
dotnet build test/nPlaywrightTests/nPlaywrightTests.csproj
pwsh test/nPlaywrightTests/bin/Debug/net8.0/playwright.ps1 install --with-deps
dotnet test
```

The GitHub Actions CI workflow restores dependencies, builds the solution, installs Playwright browsers, runs Semgrep, and runs the automated test suite on pull requests and pushes to `main`.

## Production System

Production is containerized with Docker. The application image is built from `Dockerfile` and runs `Chirp.Web.dll` on port `5000`.

The deployed system in `remote_files/docker-compose.yml` consists of:

- `nginx`, exposed on host port `5000`, forwarding traffic to the web app.
- `minitwit`, running two replicas of the Chirp application image.
- `prometheus`, exposed on port `9090`.
- `grafana`, exposed on port `3000`.
- `loki` and `promtail` for log collection.

Production requires these environment variables:

- `DOCKER_USERNAME`: Docker Hub username containing `minitwitimage`.
- `CHIRP_DB_CONNECTION`: PostgreSQL connection string used by the application in production.

On a configured server, deployment is performed from `/minitwit` folder:

The deploy script validates the required variables, pulls the configured image, starts the Compose stack with two `minitwit` replicas, and opens the required ports.

## Continuous Deployment

The repository contains three GitHub Actions workflows:

- `.github/workflows/Auto_Test_Build.yml`: CI for pull requests and pushes to `main`.
- `.github/workflows/continous-deployment.yml`: builds and pushes `${DOCKER_USERNAME}/minitwitimage` to Docker Hub, then deploys to the primary and backup servers over SSH.

Continuous deployment requires these repository secrets:

- `DOCKER_USERNAME`
- `DOCKER_PASSWORD`
- `SSH_USER`
- `SSH_KEY`
- `SSH_HOST_PRIMARY`
- `SSH_HOST_BACKUP`
- `CHIRP_DB_CONNECTION`

When changes are merged to `main`, the deployment workflow builds a Docker image, scans the image with Docker Scout, syncs deployment files to each server, and runs `remote_files/deploy.sh` with the new image tag.

## Contributing Changes Into Production

This repository follows a trunk-based workflow:

1. Create or pick up a GitHub issue with a clear description and acceptance criteria.
2. Create a short-lived feature branch from `main`.
3. Implement the change and add or update tests.
4. Run `dotnet build` and `dotnet test` locally before opening a pull request.
5. Open a pull request into `main`.
6. Wait for CI to pass and address review feedback.
7. Merge into `main` after approval.

After the merge, the continuous deployment workflow builds the production Docker image and deploys it to the configured production servers. Do not deploy unreviewed local changes directly to production.

# Video Demonstrations

## Monitoring Dashboards in Action
![Monitoring GIF](./report/images/Monitoring_Dashboards_in_action.gif)


## Logging Dashboards in Action
![Logging GIF](./report/images/logging-in-action.gif)

## IaC in Action
![IaC GIF](./report/images/iac-in-action.gif)


## CI/CD in Action
![CICD ONE GIF](./report/images/ci-cd-in-action-part1.gif)
![CICD TWO GIF](./report/images/ci-cd-in-action-part2.gif)
![CICD THREE GIF](./report/images/ci-cd-in-action-part3.gif)
![CICD FOUR GIF](./report/images/ci-cd-in-action-part4.gif)

