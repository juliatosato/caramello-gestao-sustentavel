# Application screenshot gallery

The screenshots show the real Caramello frontend and API running against a separate SQLite database. All operational data is fictional and used exclusively for demonstration. The comparison shows two equal 30-day periods with synthetic activity. None of the figures represents actual café operations or verified environmental impact. The composting locations are public reference information displayed by the application.

| Screen | What it shows |
| --- | --- |
| [01 · Login](screenshots/01-login.png) | Administrator sign-in with empty credential fields. |
| [02 · Dashboard](screenshots/02-dashboard.png) | Consumption and discard totals by unit, discard causes and low-stock alerts. |
| [03 · Inventory](screenshots/03-inventory.png) | Current balances, minimums, lead times and safety stock. |
| [04 · Restocking](screenshots/04-restocking.png) | Suggested quantities based on recent consumption and configured stock parameters. |
| [05 · Waste analysis](screenshots/05-waste-analysis.png) | Discard quantities and ratios per item, with date, category and reason filters. |
| [06 · Coffee grounds](screenshots/06-coffee-grounds.png) | Grounds generated and sent for composting or specialized collection. |
| [07 · Composting guide](screenshots/07-composting-guide.png) | Setup guidance and lists of suitable and unsuitable materials. |
| [08 · Composting locations](screenshots/08-composting-locations.png) | Public municipal locations and guidance on confirming acceptance. |
| [09 · Reports](screenshots/09-reports.png) | Equal-period comparison and options to export CSV files. |

## Recreate the demo on Windows

Build the API and frontend with the development prerequisites in the [local setup guide](DEVELOPMENT.md). Run from the repository root with Python 3.12 or newer:

```powershell
dotnet build .\backend\Caramello.Api
Push-Location .\frontend
npm ci
npm run build
Pop-Location
python -B .\tools\demo\executar.py
```

Choose a disposable password at the hidden prompt. Open the loopback URL printed by the script and sign in as `demo@example.com`. The script provisions a fresh SQLite database in a temporary directory under ignored `artifacts`, with independent keys and backups. It seeds ten items and 60 days of stock movements, coffee grounds and separate environmental records. It closes and cleans up the demo environment when you press Enter.

For automated 1920 × 1080 captures, install Google Chrome and version 15 or later of the Python `websockets` package in your development environment. Then run:

```powershell
python -B .\tools\demo\executar.py --capturar
```

Use `--chrome "path\to\chrome.exe"` for a custom Chromium executable. The capture run uses a temporary browser profile and a generated password held in memory and passed to the local API process. PNG files are written to `docs/screenshots` after the full series passes the capture checks.

For manual capture, use an isolated browser window and a consistent 1920 × 1080 viewport. Capture the login before entering credentials. On **Estoque**, frame **Saldos e parâmetros** and **Sugestões de reposição** separately. In **Borra de café**, open the composting guide for view 07 and return to the **Destinação responsável** section for view 08. The default date filters cover the latest 30 days and **Relatórios** also shows the equivalent previous period. Check the frame for visible personal data before sharing.
