# Local development on Windows

Install the .NET 10 SDK and Node.js with npm. Open PowerShell at the repository root. The commands below use a separate, ignored database with no real customer records.

## Apply migrations and start the API

```powershell
$projeto = '.\backend\Caramello.Api\Caramello.Api.csproj'
$dadosDemo = Join-Path (Get-Location).Path 'artifacts\local-dev'
$env:CARAMELLO_DATABASE_PATH = Join-Path $dadosDemo 'data\caramello.db'
$env:CARAMELLO_KEYS_PATH = Join-Path $dadosDemo 'keys'
$env:CARAMELLO_BACKUP_PATH = Join-Path $dadosDemo 'backups'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_ENVIRONMENT = 'Development'

dotnet run --project $projeto --no-launch-profile -- --migrate-desktop
if ($LASTEXITCODE -ne 0) { throw 'Database migration failed.' }
dotnet run --project $projeto --no-launch-profile -- --urls http://localhost:5000
```

The migration command exits after preparing the schema. Use a fresh PowerShell session if you have connection-string or bootstrap overrides from another environment. Use the [Windows guide](../DESKTOP.md) for upgrades involving existing data.

## Start the UI and create a local account

In a second PowerShell window at the repository root:

```powershell
Set-Location .\frontend
npm ci
npm run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Open `http://localhost:5173`. Vite forwards `/api` to `http://localhost:5000`. On the first visit, **Configuração inicial** lets you set the administrator of the empty local database. Use the reserved example address `demo@example.com` and a disposable password chosen locally. The password must contain at least 12 characters, including uppercase, lowercase, a digit and a symbol. There are no preset credentials.

The API also supports `CARAMELLO_BOOTSTRAP_ADMIN_EMAIL` and `CARAMELLO_BOOTSTRAP_ADMIN_PASSWORD` for initial provisioning. Set them only in the server process environment for the first start after migration, then remove both. The first-run form is sufficient for this development workflow.

## Verify the build

From the repository root:

```powershell
dotnet test .\backend\Caramello.Api.Tests
dotnet build .\desktop\Caramello.Desktop
Push-Location .\frontend
npm run build
Pop-Location
```

Use the [Windows packaging guide](../DESKTOP.md) to build the installer. Build outputs, demo data and configuration stay local.
