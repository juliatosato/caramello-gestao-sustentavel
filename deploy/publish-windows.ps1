param([switch]$SkipFrontendBuild)
$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$app = Join-Path $raiz "artifacts\windows\app"
$servidor = Join-Path $app "server"
$redist = Join-Path $raiz "artifacts\windows\redist"

if ((dotnet --version) -notmatch '^10\.') { throw "Instale o SDK .NET 10 antes de publicar." }

if (-not $SkipFrontendBuild) {
  Push-Location (Join-Path $raiz "frontend")
  try {
    npm ci
    if ($LASTEXITCODE -ne 0) { throw "npm ci falhou. Feche o Vite e outros processos que estejam usando node_modules." }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "O build do frontend falhou." }
  } finally { Pop-Location }
}
if (-not (Test-Path (Join-Path $raiz "frontend\dist\index.html"))) { throw "Compile o frontend antes de publicar." }

dotnet publish (Join-Path $raiz "backend\Caramello.Api\Caramello.Api.csproj") -c Release -r win-x64 --self-contained true -p:SkipFrontendBuild=true -o $servidor
if ($LASTEXITCODE -ne 0) { throw "A publicação do servidor local falhou." }
dotnet publish (Join-Path $raiz "desktop\Caramello.Desktop\Caramello.Desktop.csproj") -c Release -r win-x64 --self-contained true -o $app
if ($LASTEXITCODE -ne 0) { throw "A publicação do aplicativo desktop falhou." }

if (-not (Test-Path (Join-Path $servidor "wwwroot\index.html"))) { throw "O frontend não foi incluído no servidor local." }
New-Item -ItemType Directory -Force $redist | Out-Null
Invoke-WebRequest -UseBasicParsing "https://go.microsoft.com/fwlink/p/?LinkId=2124703" -OutFile (Join-Path $redist "MicrosoftEdgeWebview2Setup.exe")

$iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
  $candidatos = @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
  )
  $iscc = $candidatos | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $iscc) {
  Write-Warning "Inno Setup não encontrado. Instale-o gratuitamente e execute: ISCC.exe deploy\CaramelloSetup.iss"
  exit 0
}
& $iscc (Join-Path $PSScriptRoot "CaramelloSetup.iss")
