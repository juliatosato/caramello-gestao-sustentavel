$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$saida = Join-Path $raiz "artifacts\linux-arm64"

if ((dotnet --version) -notmatch '^10\.') {
  throw "Instale o SDK .NET 10 antes de publicar."
}

dotnet publish (Join-Path $raiz "backend\Caramello.Api\Caramello.Api.csproj") `
  -c Release `
  -r linux-arm64 `
  --self-contained false `
  -o $saida

if (-not (Test-Path (Join-Path $saida "wwwroot\index.html"))) {
  throw "O frontend nao foi incluido na publicacao."
}

Write-Host "Publicacao criada em $saida"
