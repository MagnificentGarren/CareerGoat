# Staging setup and verification

This guide prepares a Vercel preview frontend and a non-production API. Do not
replace the current landing-page deployment or create a paid resource. First
verify that the selected Vercel, Supabase, and .NET API-host plans and regions
meet the approved $0/month infrastructure limit. If a suitable free .NET host
cannot be confirmed, stop and agree on the trade-off before creating it.

The API host has not been selected yet. The workflow
`.github/workflows/api-staging-package.yml` verifies and packages the API for a
host-specific deployment; it does not deploy to an unspecified provider. Once
the free host is selected, add its documented deployment step and required
provider settings rather than storing credentials in this repository.

## 1. Create isolated staging resources

1. Connect the GitHub repository to Vercel and create a **separate staging
   Vercel project** with the project root set to `client`. Keep the existing
   landing page and any production project/deployment unchanged.
2. Create a separate non-production Supabase project in a region eligible for
   the verified free plan. This shell does not connect to Supabase yet; do not
   put its credentials in the frontend or workflow.
3. Select a .NET 10-capable API host only after verifying its current free-plan
   pricing, region, sleep/cold-start behavior, request limits, log retention,
   deployment method, and overage terms. Enable HTTPS and an HTTP health check
   at `/health`.
4. Configure the Vercel project to deploy previews only for staging until the
   replacement landing page is approved. Use a stable preview/branch URL so
   the exact CORS origin remains valid across deploys.

## 2. Configure provider settings

Add these values in provider settings. The API base URL and frontend origin are
public configuration, not secrets.

| Setting | Value | Where |
|---|---|---|
| `CAREERGOAT_API_BASE_URL` | `https://<staging-api-host>` (no path or trailing slash required) | Vercel **Preview** environment |
| `ASPNETCORE_ENVIRONMENT` | `Staging` | API host environment |
| `Frontend__AllowedOrigins__0` | Exact stable Vercel origin, e.g. `https://<staging-project>.vercel.app` | API host environment |

Set `CAREERGOAT_API_BASE_URL` before triggering a Vercel build. The build
requires it on Vercel and embeds only this public URL in
`runtime-config.js`. Do not add a trailing path, query, credentials, or
non-HTTPS URL. Local builds default to `http://localhost:5219`.

The API fails to start outside Development when no allowed frontend origin is
configured. It accepts only exact origins; use the origin alone, with no path
or trailing slash. Do not use `*`.

### Secret checklist

- The Phase 1 shell requires **no API secrets**: it has no database, Supabase
  Auth, or paid AI integration.
- Do not add secrets to GitHub Actions, `vercel.json`, source files, or this
  checklist. If a later phase requires secrets, store them only in the
  relevant provider's secret settings.
- `ConnectionStrings__CareerGoat`, `Supabase__Url`,
  `Supabase__JwtAudience`, `Supabase__JwtIssuer`,
  `Supabase__ServiceRoleKey`, `Gemini__ApiKey`, and `Gemini__Model` are not
  consumed by this shell. Do not configure unused values now. Never expose a
  database connection string, service-role key, or Gemini key to Vercel.

## 3. Build and deploy

1. Confirm the repository's `CI` workflow passes on the commit to preview.
   It runs .NET restore/format/build/tests, Angular format/lint/build/tests,
   and Gitleaks scanning.
2. In GitHub Actions, run **Staging API package** on the commit to deploy. It
   repeats the .NET checks and publishes a seven-day `careergoat-api-staging`
   artifact. Deploy that artifact using the selected host's documented
   integration. Do not claim staging is deployed until the host-specific
   deployment completes successfully.
3. Vercel builds the Angular client from `client/vercel.json` when the
   repository integration creates a Preview deployment. Do not promote this
   shell to the production landing-page deployment.
4. If the chosen host deploys directly from GitHub rather than consuming the
   artifact, configure it to deploy only the verified commit after CI passes.
   Record the actual host workflow and rollback target after selecting it.

No Supabase migrations or production data are required for this shell. Use only
synthetic data in staging.

## 4. Verify the deployment

Replace the example URLs with the non-secret deployment URLs:

```powershell
Invoke-RestMethod https://<staging-api-host>/health
Invoke-WebRequest https://<staging-preview>.vercel.app
Invoke-WebRequest https://<staging-preview>.vercel.app/a/preview-route
```

The health response must contain `{"status":"healthy"}` and return HTTP 200.
The Angular shell should show **Connected** for API health; an unavailable
status requires checking the configured base URL, HTTPS certificate, API
availability, and exact CORS origin. Refreshing a nested SPA path must return
the Angular shell over HTTPS.

Record the Vercel preview URL, API URL, deployed commit, passing CI run, chosen
regions/plans, and the health/CORS/SPA-refresh results. Do not record secrets.
The shell keeps the existing static landing pages in the repository; do not
remove or replace them until the landing-page migration is separately approved
and verified.

## 5. Configuration checklist

- [ ] The API host and Vercel staging project are separate from production.
- [ ] The selected plans and regions have been checked against the $0/month
  infrastructure limit.
- [ ] Vercel Preview has `CAREERGOAT_API_BASE_URL`; no secret is used as a
  frontend build variable.
- [ ] The API host has `ASPNETCORE_ENVIRONMENT=Staging` and the exact
  `Frontend__AllowedOrigins__0` value.
- [ ] Any future API credentials are stored only in the API host's secret
  settings; none are needed for the current shell.
- [ ] CI, API health, browser-to-API CORS, HTTPS, and SPA refresh checks pass.
- [ ] The production landing page remains unchanged.
