# StarshipsApp

A web application built with ASP.NET Core (.NET 8) and MVC architecture for managing a collection of Starships. The app provides a responsive, sortable, and filterable table of starships, with full Create, Read, Update, and Delete (CRUD) support.

## Features

- Bootstrap table UI: Clean, responsive table using Bootstrap.
- Full CRUD Support: Create, read, update, and delete starships via the UI.
- Client-side Validation: Forms use jQuery Validation for instant feedback.
- Bootstrap Styling: Modern, responsive UI for all pages.
- Docker Support: Run the app in a containerized environment with SQLite persistence.
- Published in Azure: App is hosted on Azure - https://starshipsapp-c6eug4dkcca0etfm.canadacentral-01.azurewebsites.net

## What’s new

- UI: Bootstrap table for listing starships.
- Robust seeding from SWAPI (https://swapi.dev):
  - Fetches all pages (pagination) with a 10s timeout and a User-Agent header.
  - Resilient: if SWAPI is unreachable or returns no data, seeding falls back to 3 embedded ships so the app is never empty.
  - Only seeds when the database is empty.
- Correct JSON mapping:
  - `starship_class` from SWAPI is now mapped to `Starship.StarshipClass` via `JsonPropertyName`.
- Deployment hardening for Azure App Service (Linux):
  - Connection string is read from `ConnectionStrings:DefaultConnection`, with fallback to `CUSTOMCONNSTR_DefaultConnection`.
  - On Linux in Production, the app ensures the SQLite directory exists before running migrations (e.g., `/home/data`).
  - Migrations then run, followed by the resilient seed with logging.
- Documentation updated with precise Azure configuration guidance and troubleshooting.

## Technologies

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core (SQLite)
- Bootstrap 5
- jQuery & jQuery Validation
- Docker & Docker Compose
- xUnit, EF Core InMemory, coverlet (tests)

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional)
- Visual Studio 2022 (optional)

### Local Setup (without Docker)

1. Clone the repository:
   ```sh
   git clone https://github.com/mattrick4/StarshipsApp
   cd StarshipsApp
   ```

2. Configure the database:
- Update the connection string in `appsettings.json` if needed (default is SQLite).
3. Apply migrations and seed data:
   ```sh
   dotnet ef database update
   ```

4. Run the application:
- In Visual Studio press __F5__, or:
  ```sh
  dotnet run
  ```
5. Access the app:
- Navigate to `https://localhost:<port>/Starships` (or `http://localhost:<port>/Starships`).

### Setup with Docker

1. Build and run:
   ```sh
   docker-compose up --build
   ```

2. Access the app:
- Navigate to `http://localhost:5000` (adjust if different).

## Deploy to Azure App Service (Linux)

Fastest path using Visual Studio:

1. Right-click the project > __Publish__ > Azure > __Azure App Service (Linux)__ > Create new.
- Runtime: .NET 8 (LTS). Plan: Free (F1) or higher.
2. In Azure Portal > Your App Service > Settings > Configuration (Environment variables):
- Add Application setting:
  - Name: `ConnectionStrings__DefaultConnection`
  - Value: `Data Source=/home/data/starships.db`
- Save and Restart.
- Note: Double underscore `__` represents `:` in ASP.NET Core configuration keys.
3. Publish from Visual Studio via __Publish__.
4. First start runs migrations and seeds from SWAPI. You should see all starships. If SWAPI is unavailable, 3 fallback ships are added.

Verification
- SSH into the app (Development Tools > SSH) and run:
- `printenv | grep -i connection` (ensure the setting is present)
- `ls -l /home/data` (verify `starships.db` exists)
- App Service > __Log stream__ shows messages like:
- “Seeded N starships from SWAPI.” or “Seeded 3 starships from embedded fallback.”

Reseeding (optional)
- To force reseed, delete the DB file via SSH (`rm /home/data/starships.db`) and Restart.

## Troubleshooting

- Application Error / ServiceUnavailable after publish:
- Ensure `ConnectionStrings__DefaultConnection` is set to `Data Source=/home/data/starships.db` and Restart.
- If you used the Connection strings tab instead, add a setting named `DefaultConnection` (Type: Custom) and ensure the app has the fallback to `CUSTOMCONNSTR_DefaultConnection`.
- Empty table on first run:
- Check Log stream for SWAPI errors. Fallback seed adds 3 items if SWAPI is down.
- Confirm outbound access: SSH then `curl -I https://swapi.dev/api/starships/`.
- Data disappears after restart:
- Verify the connection string points under `/home` (persistent storage), not a temp path.

## Next

- Data and seeding
  - Disable SWAPI seeding after first successful seed via an app setting (for example, `Seed__Enable=false`).
  - Add a background job to refresh data periodically with retry/backoff and caching.
- Azure reliability
  - Enable __Always On__ (non-Free plans) and configure Health Check endpoint.
  - Add Application Insights for distributed tracing and logs.
  - Consider Azure SQL for production workloads and switch to `UseSqlServer` in Production.
- CI/CD
  - Add GitHub Actions to build, test, and deploy to Azure App Service on every push.
  - Use deployment slots for zero-downtime swaps.
- UX and API
  - Add server-side paging/sorting/filtering to complement DataTables for large datasets.
  - Add validation attributes and friendly error pages.
- Testing
  - Add integration tests using `WebApplicationFactory` and a transient SQLite DB.
  - Increase coverage for seeding paths and error handling.
- Architecture
  - Optionally refactor to Razor Pages for a page-focused UI, keeping existing behavior.
  - Extract SWAPI calls behind an interface and use `IHttpClientFactory` with a named client.
- Implement AI features
- Add Authentication

## Design notes

- Read operations use `AsNoTracking()` for Index/Details to avoid tracking overhead.
- Edit (POST) copies values into the existing tracked entity to avoid double-tracking.
- Returns 400 on route/model ID mismatch; 404 if the entity is missing.
- Concurrency handling: 404 if deleted; rethrows `DbUpdateConcurrencyException` on conflict.
- Delete (POST) is idempotent.

## Project Structure

- `Controllers/StarshipsController.cs` – MVC controller for CRUD.
- `Models/Starship.cs` – Entity definition (with JSON attribute for SWAPI mapping).
- `Data/AppDbContext.cs` – EF Core DbContext.
- `Data/DbInitializer.cs` – Resilient, paginated seeding from SWAPI with fallback.
- `Views/Starships/` – Razor views (list/create/edit/details/delete).
- `StarshipsApp.Tests/` – xUnit tests for controller behavior and edge cases.

## License


This project is licensed under the MIT License.



