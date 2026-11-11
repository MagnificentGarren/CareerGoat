# Client

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.6.

## Development server

Set the local Supabase URL and anon key in PowerShell, then start the server.
The `prestart` script writes them to an ignored local runtime file:

```powershell
$env:CAREERGOAT_SUPABASE_URL = "http://127.0.0.1:54321"
$env:CAREERGOAT_SUPABASE_ANON_KEY = "<local anon key>"
npm start
```

Configure the API process and local database separately as described in the
repository's [staging and local setup guide](../docs/STAGING.md). Never put
database credentials in the client or its runtime configuration.

Once the server is running, open `http://localhost:4200/`. The application
automatically reloads whenever you modify source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
