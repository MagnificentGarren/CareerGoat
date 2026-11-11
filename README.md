# CareerGoat

CareerGoat is an early-career job-search companion. This repository contains
the Angular development shell, a .NET API, test projects, and Supabase's local
development configuration. The existing `summit-blue.html` landing page and
`construction.html` page remain unchanged in the project root.

## Phase 0 defaults and budget

- Angular 22 and .NET 10 LTS are used for the initial scaffold.
- The API uses a deterministic fake AI provider by default. It makes no
  third-party AI calls and needs no credentials.
- Supabase runs locally; no cloud account or deployment is needed for Phase 0.
- The approved infrastructure ceiling remains **$0/month**. Gemini is the only
  approved paid category and is capped at **$10/month total**; paid AI is not
  wired into this scaffold.
- Do not use real CVs or other personal data. Privacy, retention, and consent
  decisions remain open as recorded in `IMPLEMENTATION-PLAN.md`.

## Prerequisites

- Node.js 24 LTS (24.15 or newer) and npm.
- .NET 10 SDK.
- Docker Desktop for Windows, using Linux containers, started before Supabase.
- Supabase CLI. On Windows, install it using the official Supabase CLI
  instructions. If Scoop is not installed, install it for the current Windows
  user using the official instructions at https://scoop.sh. Then install the
  Supabase CLI:

  ```powershell
  scoop bucket add supabase https://github.com/supabase/scoop-bucket.git
  scoop install supabase
  ```

Verify with `node --version`, `dotnet --version`, `docker info`, and
`supabase --version`.

## Run locally

Open Docker Desktop and wait for its engine to become ready. From the project
root, start the local Supabase services:

```powershell
supabase start
```

In a second PowerShell terminal, run the API:

```powershell
dotnet run --project .\src\CareerGoat.Api --launch-profile http
```

In a third terminal, run the Angular app:

```powershell
Set-Location .\client
npm ci
npm start
```

Open `http://localhost:4200`. The local API health endpoint is
`http://localhost:5219/health`; the development-only sample response is
`http://localhost:5219/api/v1/demo-analysis`. The fake response is static local
sample data, not a real CV analysis. Supabase Studio is available at
`http://localhost:54323`.

Stop local Supabase with `supabase stop`. The API and Angular development server
can be stopped with `Ctrl+C` in their respective terminals.

## Build, format, lint, and test

Run from the project root:

```powershell
dotnet restore .\CareerGoat.slnx
dotnet format .\CareerGoat.slnx
dotnet format .\CareerGoat.slnx --verify-no-changes --no-restore
dotnet build .\CareerGoat.slnx --no-restore
dotnet test .\CareerGoat.slnx --no-build
```

Run the frontend checks from `client`:

```powershell
npm ci
npm run format
npm run format:check
npm run lint
npm run build
npm test
```

GitHub Actions runs the .NET and Angular checks plus Gitleaks secret scanning
for pushes and pull requests. The CI run becomes available after the repository
is connected to GitHub. Never commit actual credentials; use provider secret
stores in later deployment phases.

The Gitleaks GitHub Action does not require a license for a personal-account
repository; organization repositories may require a Gitleaks license. Do not
add paid scanning under the current budget approval.

## Configuration

`.env.example` is a names-only/safe-default reference and is not loaded
automatically. Local behavior comes from the Development launch profile and
`appsettings.Development.json`. The API uses no Supabase or Gemini credentials
in Phase 0; never put service-role keys, database passwords, or Gemini keys in
the Angular client.

For staging, follow [the staging setup and verification guide](./docs/STAGING.md).
The frontend reads the public `CAREERGOAT_API_BASE_URL`, Supabase URL, and anon
key at build time. The API requires `ASPNETCORE_ENVIRONMENT=Staging`, the exact
`Frontend__AllowedOrigins__0` origin, Supabase Auth URL/key, and a secret
PostgreSQL connection string in the API host settings. The anon key is public;
the database connection string is not. Apply database changes using the
versioned Supabase migration instructions in the staging guide. A Render
Dockerfile is prepared for the staging API; review current free-plan usage and
overage terms before creating a Render service.

## Current status and next owner actions

The project now includes Phase 2 authentication, profile/target-role APIs and
UI, an ownership/RLS migration, and cross-user API tests. To complete local
and staging evidence, start Docker Desktop and install the Supabase CLI, then
follow **Run locally**, **Build, format, lint, and test**, and the Phase 2
staging steps. Provider setup, OAuth configuration, migration application,
and sign-in/deletion UX review remain owner actions. Keep the existing
landing-page deployment unchanged.
