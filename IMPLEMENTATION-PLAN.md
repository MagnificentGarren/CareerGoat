# CareerGoat Implementation and Deployment Plan

## 1. Goal and current state

CareerGoat helps early-career developers tailor a CV to a role, identify skill gaps, and practise interviews. The repository currently contains static HTML landing pages; it does not yet contain the Angular application, API, or persistence layer.

Build a small, deployable MVP first. Keep the existing landing page available while the application is being built, then migrate its content into the product frontend so there is one maintained web experience.

## 2. Decisions (MVP)

| Area | Decision | Rationale |
|---|---|---|
| Web frontend | Angular SPA, hosted on Vercel | Static hosting, preview deployments, and simple custom-domain setup |
| API/backend | ASP.NET Core Web API; choose a .NET-capable host that fits the approved $0 infrastructure budget | Keeps the planned C# stack without authorizing recurring hosting costs |
| Database | Supabase managed PostgreSQL | Avoids operating a database; provides backups and a straightforward managed Postgres service |
| User authentication | Supabase Auth with Google OAuth | Avoids implementing password storage, reset flows, and account verification |
| File storage | No persistent CV-file storage in the MVP; stream PDFs to the API for bounded in-memory parsing | Minimizes retention and avoids public CV URLs; add private object storage only if a later feature needs it |
| AI | Google Gemini API, called only by the backend | Keeps provider credentials private and centralizes validation, quotas, and logging |
| Source control and CI | GitHub Actions | Runs checks on pull requests and deploys the API; Vercel handles frontend builds/deploys |
| Local development | Angular CLI, .NET SDK, and Supabase CLI | Reproduces frontend, API, Postgres, and Auth locally |

Use the latest supported Angular release and the current .NET LTS release when implementation starts; pin dependency versions and commit lock files. Use PostgreSQL only. Do not add Azure SQL, Redis, Kubernetes, a container registry, or a second database.

### Decisions from product owner (2026-11-10)

- **Initial geography/region:** No specific user geography or country is selected. At setup, choose a region available on the selected free plans, preferably close to the initial audience. Revisit region and data-residency requirements before production.
- **Domain:** Start with a Vercel-provided `*.vercel.app` hostname, aiming for a CareerGoat-branded slug (for example `careergoat.vercel.app`, subject to availability). No custom domain purchase is approved yet.
- **Product tier:** Google OAuth sign-in is intended. Confirmed free-account caps: 3 CV analyses and 5 interview sessions per account per UTC calendar month; up to 10 user turns per interview session; PDFs up to 5 MB and 10 pages; plus per-minute request throttling.
- **Data retention:** Choose minimal retention and user-initiated deletion. Exact retention periods still need approval before real-user launch.
- **Privacy/legal text:** Defer final privacy notice, terms, and AI consent wording until the privacy milestone; do not process real users' CVs before this is resolved.
- **Uploads:** PDF upload is required from the first public release. DOCX is not required for launch.
- **Infrastructure spend:** Hard $0/month ceiling for hosting and other infrastructure. Gemini API usage is the only approved paid category, capped at **$10/month total across all users**. No paid hosting, domain, storage, monitoring, or other service is authorized. Azure App Service is not the selected host unless a genuinely no-cost plan is verified.

Apply the confirmed limits on the server using authenticated account ID, not only in the UI. Do not silently charge users if a free quota is exhausted. Track estimated Gemini spend across all users using the active model's current input/output token rates; reserve a request's maximum allowed cost before sending it, accounting for concurrent requests, and stop AI calls before the $10 monthly cap is exceeded. Fail closed with a clear message until the budget period resets. Configure provider billing alerts or limits where available, but do not treat alerts as a hard cap; document any provider billing/reporting delay and do not claim the application estimate guarantees the final invoice.

These product limits do not guarantee that infrastructure remains within free provider tiers. Confirm service pricing, free-tier limits, OAuth requirements, AI data terms, and free-tier regional availability when accounts are created; provider offers can change.

### Request and data flow

```text
Browser (Angular on Vercel)
  ├── Supabase Auth: sign-in, sign-up, refresh session
  └── HTTPS + Supabase access token
        ↓
ASP.NET Core API (free host selected after verification)
  ├── validates token and authorizes resource ownership
  ├── reads/writes Supabase PostgreSQL
  ├── parses bounded PDF uploads in memory without retaining source files
  └── calls Gemini with validated, size-limited inputs
```

The browser must never receive the Gemini key, database credentials, or Supabase service-role key. All application data reads/writes go through the API. The API validates the Supabase JWT and scopes every query to the authenticated user; never accept a user ID from the request as proof of ownership.

## 3. MVP scope and architecture

### Include in the first usable release

1. Sign up, sign in, sign out, and authenticated application routes.
2. Create a target role with a pasted job description and CV text, or upload a PDF (required at launch); DOCX is deferred.
3. Generate one structured CV-to-role analysis: match score, evidence-based summary, suggested bullet revisions, prioritized skill gaps, and interview questions.
4. Save, list, view, and delete a user's analyses.
5. Provide an interview practice session with one persona and a persisted transcript. Add additional personas after the end-to-end flow works.
6. Deploy a production frontend and API with health checks, error reporting, and documented recovery steps.

### Defer until there is a working MVP

- Stripe/subscriptions/credit purchases.
- Redis, distributed caching, and separate job queues.
- SignalR and token-by-token interview streaming. Start with standard HTTP requests; add streaming only if latency and UX testing justify the added operational complexity.
- MediatR/CQRS, microservices, and elaborate Clean Architecture layers. Start with feature-oriented API services, DTOs, validation, and EF Core; split abstractions when real complexity warrants it.
- Enterprise plans, multiple AI providers, analytics dashboards, and advanced persona customization.

## 4. Repository layout

```text
CareerGoat/
├── .github/workflows/           # CI and API deployment
├── client/                      # Angular application
├── src/
│   ├── CareerGoat.Api/           # HTTP endpoints, auth, middleware
│   ├── CareerGoat.Application/   # Use cases, DTOs, validation
│   └── CareerGoat.Infrastructure/# EF Core, Supabase, Gemini, document parsing
├── tests/
│   ├── CareerGoat.UnitTests/
│   └── CareerGoat.ApiTests/
├── supabase/
│   ├── config.toml
│   └── migrations/
├── .env.example                  # Names and safe local defaults only
└── README.md                     # Setup, test, and deploy instructions
```

Use Supabase CLI for the local backend services (Docker Desktop is a local prerequisite); run the API with the .NET SDK and the frontend with Angular CLI. Keep the existing static landing page deployable during initial development. When the Angular shell is ready, migrate its content and styling into Angular and remove duplicate production entry points only after the replacement is verified.

## 5. Data and privacy

Use Supabase-managed PostgreSQL, with versioned SQL migrations committed to `supabase/migrations/`. EF Core is the API's data-access layer; use Supabase CLI to apply the reviewed SQL migrations in each environment. Do not also maintain EF-generated migrations or a separate hand-run production DDL script.

Initial application tables:

- `profiles`: Supabase Auth user ID, display name, timestamps. Do not store passwords.
- `target_profiles`: owner ID, role title, job description, extracted CV text (only if the user consents to saving), timestamps.
- `cv_analyses`: target profile ID, schema version, structured JSON result, model/provider metadata, timestamps.
- `interview_sessions`: target profile ID, persona key, status, timestamps.
- `interview_messages`: session ID, role, content, sequence number, timestamp. Keep messages as rows rather than one ever-growing JSON chat blob.

Add foreign keys, ownership indexes, uniqueness constraints, and deletion behavior in migrations. Store timestamps in UTC. Keep structured AI results in JSONB only where their shape is expected to evolve; keep frequently filtered fields in typed columns. Add indexes for actual query patterns rather than indexing every JSON field.

CVs and job descriptions can contain personal or confidential information:

- Explain what is sent to Gemini and obtain user consent before processing.
- Do not log prompts, CV text, job descriptions, tokens, or full AI responses.
- Use a private storage bucket and short-lived signed URLs if file downloads are needed.
- Set explicit file-size, page-count, MIME/signature, and supported-format limits; reject malformed or empty extracted text.
- Define deletion behavior for source files, extracted text, analyses, and interview transcripts. Provide user-initiated deletion.
- Set a documented retention policy before public launch; default to the shortest useful retention.
- Check the Gemini provider's current data-use and retention terms before launch.

The API is the only application-data access path. Use a least-privilege database role for runtime access and a separate migration credential. Keep Supabase service-role credentials server-side. If direct browser access to Supabase data is introduced later, require and test Row Level Security policies before enabling it.

## 6. API, AI, and file-processing rules

- Version public routes (for example `/api/v1/...`) and return consistent problem-details errors.
- Validate inputs at the API boundary, enforce request/time limits, and rate-limit expensive AI operations per authenticated user.
- Treat all CV and job-description content as untrusted data. Do not attempt to make prompt injection safe by deleting selected strings; isolate user content in the prompt, constrain model output with a schema, validate the result, and handle refusal/invalid output explicitly.
- Use typed request/response DTOs and a versioned JSON schema for AI output. Persist the schema version so older results remain renderable.
- Make AI requests cancellable and apply timeouts, bounded retries for transient errors, and provider error handling. Do not retry validation errors or quota failures blindly.
- Never let the model decide authorization, billing status, or user identity.
- Validate upload extension, declared MIME type, file signature, size, and parser outcome. Process files in memory or temporary storage with guaranteed cleanup; do not trust the submitted filename.
- Return only the data the current user is authorized to see. Enforce ownership in every read, update, and delete operation.
- Keep CORS limited to the deployed frontend origins. Require HTTPS outside local development.

## 7. Environments, secrets, and deployment

### Environments

- **Local:** Angular and API run locally; Supabase CLI provides local Postgres/Auth/Storage. Use fake/test AI responses by default, with an explicit opt-in for a developer's personal API key.
- **Preview/staging:** Vercel preview frontend + non-production API/database credentials. Use synthetic data only.
- **Production:** Vercel production frontend + API host selected after budget verification + production Supabase project.

Create separate Supabase projects for non-production and production. Never point preview builds at production data.

### Frontend (Vercel)

1. Connect the GitHub repository and configure the Angular build command and output directory.
2. Add environment-specific public configuration (API base URL and Supabase public URL/anon key only).
3. Configure SPA route fallback and the Vercel-provided hostname; add a purchased custom domain only after explicit approval.
4. Require passing checks before production promotion. Verify client-side secrets are absent from built assets.

### Backend (host to be selected under a $0 infrastructure ceiling)

1. Select a .NET-capable host whose current plan can meet the $0/month infrastructure ceiling. Verify sleep/cold-start, uptime, request limits, log retention, deployment, and account/overage terms before choosing.
2. Do not select an Azure App Service plan, container registry, or other paid resource unless its actual recurring cost is $0 and the owner confirms it. If no suitable free .NET host meets the MVP requirements, pause and present the trade-off; do not silently incur cost or change the backend stack.
3. Use one GitHub Actions deployment workflow after restore, format checks, build, and tests pass.
4. Configure the health endpoint, HTTPS-only access, startup/health checks, and structured logs.
5. Store Gemini, Supabase, and database secrets in the chosen host's secret settings. Never put secrets in source control, frontend variables, or workflow logs.
6. Apply reviewed Supabase CLI SQL migrations as a controlled release step before deploying code that requires them. Use additive changes first; destructive column removal is a later release.
7. Keep a previous deployment available for rollback and document how to roll back application code without corrupting data.

### Database and domains

- Choose Supabase and the selected API host's regions based on free-plan availability and proximity to the initial audience; record actual regions before creating resources. Verify hosting/storage/monitoring costs remain $0/month and configure any available quotas/alerts. Enforce and test the $10/month Gemini spend limit before enabling paid AI calls.
- Require TLS for database connections. Use Supabase's recommended connection mode for a persistent API and a direct/migration connection for schema changes where required.
- Verify automated backups, retention, and a restore procedure before public launch; perform a restore test, not just a backup check.
- Use the Vercel hostname for the initial launch. Configure a purchased custom domain and DNS only after approval and staging smoke tests; document ownership and renewal.

### Required configuration

Keep only variable names and harmless local examples in `.env.example`; use the deployment provider's secret store for real values. Expected server-side settings include:

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__CareerGoat
Supabase__Url
Supabase__JwtAudience
Supabase__JwtIssuer
Supabase__ServiceRoleKey
Gemini__ApiKey
Gemini__Model
Frontend__AllowedOrigins
```

Only the Supabase project URL and public anon key may be exposed to the browser. Confirm exact Supabase JWT settings and connection strings from the created project rather than guessing them.

## 8. CI, testing, and observability

For every pull request, CI should:

1. Restore, format-check, build, and test the .NET solution.
2. Install frontend dependencies from the lock file, lint, type-check/build, and run unit tests.
3. Run API integration tests against an isolated test database or local Supabase stack.
4. Check migrations from a clean database and verify the previous schema can migrate forward.
5. Check for accidentally committed secrets and produce deployment artifacts only from a passing commit.

Minimum test coverage by behavior:

- API authorization/ownership tests (including attempts to access another user's records).
- Input validation, limits, and consistent error responses.
- AI output schema validation, malformed/refused responses, and provider timeout/quota behavior using a fake provider.
- PDF size/page limits and malformed/unsupported file handling.
- Angular sign-in states, analysis loading/error states, and rendering of validated results.
- A production-like smoke test: sign in, create analysis, retrieve it, delete it, and verify health endpoints.

Use the selected API host's supported telemetry for availability, latency, exceptions, and dependency failures. Redact personal content and secrets before telemetry. Alert on sustained API failures, elevated latency, and provider quota/error spikes. Provide a user-safe error message and a request/correlation ID for support.

## 9. Responsibilities and approval boundaries

The plan distinguishes work that can be delivered and verified in this repository from actions requiring your accounts, money, identity, legal judgment, or explicit approval. I can prepare configuration and deployment instructions, but I cannot create accounts, accept provider terms, purchase resources, access private credentials, or make legal decisions on your behalf.

| I will do in the project | You will do outside the project |
|---|---|
| Create and maintain the frontend, API, migrations, tests, CI workflows, documentation, and safe example configuration. | Create/own the GitHub repository and provider accounts; grant repository access or connect integrations yourself. |
| Keep secrets out of source control; document the exact secret names and where each belongs. | Enter credentials directly into provider secret settings. Never send secrets in chat or commit them. |
| Build local and staging-ready implementations, automated checks, and repeatable deployment steps. | Choose/approve region, domain, plan/budget, and any paid services before resources are created. |
| Provide preview URLs, test results, migration instructions, and rollback steps when the integrations are available. | Accept Supabase, Vercel, selected API-host, Gemini, and domain-provider terms; add billing details if required and manage account recovery. |
| Implement consent and deletion behavior according to requirements you approve, and flag unresolved privacy decisions. | Approve the product's privacy/retention policy, terms, AI disclosure, and launch readiness; consult appropriate legal advice when needed. |
| Stop before destructive production actions and ask for confirmation. | Explicitly approve production provisioning, DNS changes, production migrations, and launch. |

### Decisions you must provide or approve

The owner has provided initial direction; these items remain open before the relevant implementation/release gate:

1. Initial audience geography is not specified. Choose a free-tier region near the initial audience at setup and confirm data-residency needs before production.
2. Use a Vercel-provided CareerGoat-branded hostname if available; no custom domain is owned/approved yet.
3. Product caps are confirmed: 3 analyses and 5 interview sessions per account per UTC calendar month, up to 10 user turns per session, and PDF limits of 5 MB/10 pages. Infrastructure has a hard $0/month ceiling; Gemini spend is capped at $10/month across all users.
4. Minimal retention and user deletion are approved as principles; exact retention periods are still open. Proposed privacy-preserving default: do not retain uploaded PDF files after request processing; retain extracted text and transcripts only as needed for saved user work, with user-initiated deletion and an agreed inactive-account retention period before launch.
5. Final privacy notice, terms, and Gemini consent wording are deferred until the privacy milestone; no real-user processing before review and approval.
6. PDF uploads are required at launch; DOCX is deferred.
7. Google OAuth must be configured by the owner in Supabase/Google provider settings. Do not assume social sign-in alone is sufficient authorization or abuse control.

Development may proceed with local services, fake AI responses, bounded PDF handling, and no production deployment. Paid Gemini integration may be enabled only with the $10/month aggregate guard in place. Do not infer consent to spend money or publish user data.

## 10. Phases, owners, and completion evidence

Phases are gates, not calendar promises. Each phase has a repository deliverable, an external action where needed, and evidence that can be checked. Do not mark a phase complete based only on code being written; complete its listed checks and capture the evidence in the pull request/release notes or deployment record.

### Phase 0 — Confirm scope and make the project runnable

#### Phase 0 implementation decisions (2026-11-11)

- Use the current supported Angular 22 release and .NET 10 LTS for the scaffold;
  pin frontend dependencies in `client/package-lock.json` and select the .NET 10
  SDK feature band through `global.json`.
- Start with a minimal API and separate unit/API test projects; defer additional
  application/infrastructure layers until a concrete feature needs them.
- Register a deterministic fake AI adapter as the only provider. The sample
  endpoint is available only in Development, and no paid AI credentials or
  provider network calls are used.
- Keep both existing static pages at the project root while Angular is developed
  separately. No production host, cloud account, domain, or spend is selected.
- The existing approved limits remain unchanged: $0/month infrastructure and
  at most $10/month total Gemini spend once a guarded paid integration is
  explicitly implemented and approved.

**I implement**

- Scaffold the Angular client, .NET API, test projects, Supabase local configuration, and initial README while preserving the existing static pages.
- Add safe configuration templates, formatter/build/test commands, and a fake AI adapter so core work does not depend on paid credentials.
- Add a short decision record in the plan for any approved choices that differ from the defaults.

**You do outside the project**

- Create/connect GitHub if it does not exist; keep the approved spend limits recorded in configuration/documentation.
- Install the local prerequisites when prompted: supported Node.js, .NET SDK, Docker Desktop, and Supabase CLI.

**Completion evidence**

- A clean checkout follows README instructions to start the local dependencies, API, and frontend.
- Build, format/lint, and test commands are recorded and pass locally and in pull-request CI.
- Automated secret scanning passes; no real credentials are present in tracked files.
- The scope decisions and unresolved items are written down; no cloud spend or production account is required.

### Phase 1 — Deploy a safe shell to staging

**I implement**

- Add a minimal API health endpoint, environment-aware configuration, frontend API configuration, and CI checks.
- Add deployment workflow/configuration and a staging runbook without embedding credentials.
- Keep the current landing page available until its replacement is ready.

**You do outside the project**

- Create Vercel, a separate non-production Supabase project, and the selected API-host account; connect GitHub and enter secrets directly in provider settings.
- Verify that the selected plans add no recurring infrastructure cost and choose regions with eligible free tiers. If no suitable free .NET API host is available, pause for a decision rather than incur cost. Share only non-secret deployment outputs (for example public preview/API URLs).

**Completion evidence**

- A Vercel preview URL loads over HTTPS and its SPA routes resolve on refresh.
- A staging API URL returns a healthy response; the frontend can reach it without CORS errors.
- CI shows a passing run for the deployed commit, and staging uses only non-production credentials/data.
- A configuration checklist identifies each required setting by name and confirms secrets are stored only in provider secret settings.

### Phase 2 — Authentication and user-owned records

**I implement**

- Supabase Auth sign-up/sign-in/sign-out flows and API token validation.
- Versioned migrations and API/UI flows for profiles and target roles; establish the ownership pattern that later analysis endpoints will use.
- Per-user authorization on every read/write/delete path and automated cross-user access tests.

**You do outside the project**

- Configure staging auth settings and authorized redirect URLs; provide test accounts through the provider, not passwords in chat.
- Review sign-in UX and confirm the account/data deletion expectations.

**Completion evidence**

- Automated tests show unauthenticated requests are rejected and user A cannot read, change, or delete user B's records.
- Staging walkthrough demonstrates sign-up, sign-in, sign-out, target-role creation, list/detail, and deletion.
- Migrations apply from an empty local database and from the previous staging schema without manual edits.

### Phase 3 — CV-to-role analysis

**I implement**

- Pasted-text analysis and bounded PDF upload/parsing, a validated Gemini integration behind a provider interface, output schema validation, quotas, timeouts, and safe errors.
- Consent UI, analysis persistence/deletion, and tests for invalid AI responses and provider failures.
- Stream PDF uploads to the API, validate type/signature/size/page count, parse without retaining the source, and reject malformed or empty extraction results. DOCX remains deferred.

**You do outside the project**

- Create a Gemini project/API key, set provider usage alerts/limits where available, and enter the key only in local secret storage or the selected host's secret settings. The application must also enforce the $10/month aggregate cap.
- Review the consent text and verify Gemini terms/data handling are acceptable before real CVs are used.
- Supply synthetic test CVs/job descriptions or approve test data that contains no sensitive personal information.

**Completion evidence**

- Automated tests cover schema-valid output, malformed/refused output, provider timeout/quota, rate limits, and deletion.
- Staging walkthrough creates an analysis, displays all required fields, reloads it, and deletes it.
- Test logs/telemetry contain no CV, job-description, prompt, API key, or full AI response.
- A smoke test passes once with the fake provider; a real-provider test passes only after you configure credentials and approve non-sensitive test input.

### Phase 4 — Interview practice and operational readiness

**I implement**

- One-persona interview flow with persisted messages and a debrief; add privacy/deletion paths, telemetry redaction, accessibility checks, and recovery documentation.
- Migrate the existing landing-page experience into Angular only after verifying content and mobile parity.
- Add backup/restore and rollback runbooks plus tests that do not access production data.

**You do outside the project**

- Review and approve retention/deletion policy, public-facing product copy, privacy notice, and support contact.
- Configure monitoring recipients and service alerts; arrange a production backup/restore test if the selected plan supports it.

**Completion evidence**

- Staging end-to-end test covers sign-in, analysis, interview, persisted reload, deletion, and health checks.
- Mobile and accessibility checks are recorded; no known blocking issues remain.
- Restore and rollback procedures have been exercised in non-production, with dated evidence and responsible owner.
- The privacy/launch checklist is signed off; app telemetry is confirmed free of user CV/job-description contents.

### Phase 5 — Production release

**I implement**

- Final deployment configuration, production checklist, backward-compatible migration/release steps, smoke tests, and rollback instructions.
- Review the exact release diff and test results before requesting production approval.

**You do outside the project**

- Create production projects and secrets separately from staging, confirm monthly budget and backup settings, configure DNS, and accept provider terms.
- Explicitly approve production migration, deployment, domain/DNS change, and public launch. Run the initial production smoke test with me using non-sensitive test data.

**Completion evidence**

- Approval is recorded for production spend, data/privacy policy, DNS, migration, and launch.
- Production URLs serve HTTPS; auth and API health checks pass; one end-to-end smoke test succeeds and its test records are deleted.
- Production database backup/restore settings, alert recipients, and support path are verified.
- The deployed commit, CI run, migration version, smoke-test result, and rollback target are recorded in the release notes.
- Rollback instructions are available and do not require destructive database reset.

## 11. Handoff and tracking rules

- Work proceeds in phase order. A phase with missing external prerequisites is marked **blocked**, with the exact user action listed; it is not represented as complete.
- Each code phase should be delivered as a coherent change with tests and an updated README/runbook. Report changed files, commands run, results, and any remaining external action.
- Use pull requests or clearly reviewable change sets. Ask before adding paid services, changing the agreed stack, exposing a new data path, or touching production.
- Keep a small release-evidence record: phase, commit, CI run, deployed URL (if applicable), migration version, test result, user approvals, and outstanding risks. Do not record credentials or personal test data.
- After each phase, review the evidence and decide whether to proceed, revise scope, or pause. Re-estimate after Phase 0; dates are not commitments until prerequisites and scope are confirmed.

## 12. Explicit launch checklist

- [ ] Production and non-production accounts/projects are separate.
- [ ] Domain, regions, infrastructure budget, account quotas, and AI provider terms are confirmed.
- [ ] Frontend bundle contains no server secrets; API CORS allows only intended origins.
- [ ] Authentication, JWT validation, per-user authorization, and rate limiting are tested.
- [ ] Upload limits, consent, data deletion, and retention policy are implemented and documented.
- [ ] Database migration, backup, restore, and rollback procedures are exercised.
- [ ] CI is required for deployment; production smoke test and monitoring alerts are active.
- [ ] Privacy notice and terms describe AI processing and handling of CV/job-description data.
- [ ] Payments remain disabled until a separate billing design covers webhook verification, idempotency, entitlements, refunds, and support.
