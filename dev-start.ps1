#Requires -Version 5.1
<#
.SYNOPSIS
    Inicia o ambiente de desenvolvimento local do ReceiptGenerator.
    API e frontend rodam localmente com hot reload.
    Banco de dados: Supabase (remoto).

.USAGE
    .\dev-start.ps1   # inicia API + frontend
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root        = $PSScriptRoot
$ApiProject  = Join-Path $Root "src\ReceiptGenerator.Api\ReceiptGenerator.Api.csproj"
$WebDir      = Join-Path $Root "web"

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host ">>> $msg" -ForegroundColor Cyan
}

function Assert-Command([string]$cmd, [string]$hint) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Host "ERRO: '$cmd' nao encontrado. $hint" -ForegroundColor Red
        exit 1
    }
}

# ---------------------------------------------------------------------------
# Verificar prerequisitos
# ---------------------------------------------------------------------------
Write-Step "Verificando prerequisitos..."
Assert-Command "dotnet" "Instale o .NET SDK 9: https://dotnet.microsoft.com/download"
Assert-Command "node"   "Instale o Node.js >= 22: https://nodejs.org"
Assert-Command "npm"    "npm deve vir com o Node.js"

Write-Host "  .NET : $(dotnet --version)"
Write-Host "  Node : $(node --version)"

# ---------------------------------------------------------------------------
# API (dotnet watch — hot reload)
# ---------------------------------------------------------------------------
Write-Step "Iniciando API com hot reload (porta 5281)..."
$apiCmd = "dotnet watch run --project `"$ApiProject`" --configuration Development"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $apiCmd `
    -WorkingDirectory $Root

# ---------------------------------------------------------------------------
# Frontend (ng serve — hot reload)
# ---------------------------------------------------------------------------
Write-Step "Instalando dependencias npm (se necessario)..."
Push-Location $WebDir
if (-not (Test-Path "node_modules")) {
    & npm install
}
Pop-Location

Write-Step "Iniciando Angular dev server (porta 4200)..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm start" `
    -WorkingDirectory $WebDir

# ---------------------------------------------------------------------------
# Resumo
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  Ambiente de desenvolvimento iniciado!" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend : http://localhost:4200" -ForegroundColor White
Write-Host "  API      : http://localhost:5281" -ForegroundColor White
Write-Host "  Swagger  : http://localhost:5281/swagger" -ForegroundColor White
Write-Host "  Banco    : Supabase (remoto)" -ForegroundColor White
Write-Host ""
Write-Host "  Aguarde ~30s para o Angular compilar antes de abrir o browser." -ForegroundColor Yellow
Write-Host "  Para parar: feche as duas janelas do PowerShell abertas." -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Green
