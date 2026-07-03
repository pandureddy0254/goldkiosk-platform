<#
.SYNOPSIS
    Apply every SQL file in this folder to a target PostgreSQL database, in lexical order.

.DESCRIPTION
    Runs psql -f against each .sql file with ON_ERROR_STOP=1 so the first failure aborts.
    Designed to be run from the folder that contains this script.

.PARAMETER Database
    Target database name. Default: goldkiosk_local.

.PARAMETER PgHost
    PostgreSQL host. Default: localhost. (Avoids the built-in $Host automatic variable.)

.PARAMETER Port
    PG port. Default: 5432.

.PARAMETER PgUser
    PG username. Default: postgres.

.EXAMPLE
    ./apply-all.ps1
    ./apply-all.ps1 -Database goldkiosk_sit -PgHost gk-pg-sit-sea.postgres.database.azure.com -PgUser myprincipal
#>

[CmdletBinding()]
param(
    [string]$Database = "goldkiosk_local",
    [string]$PgHost   = "localhost",
    [int]   $Port     = 5432,
    [string]$PgUser   = "postgres"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Force UTF-8 everywhere so the SQL files (which contain Arabic, currency
# symbols, em-dashes, etc.) are read correctly by psql on Windows.
$env:PGCLIENTENCODING       = "UTF8"
[Console]::OutputEncoding   = [System.Text.Encoding]::UTF8
$OutputEncoding             = [System.Text.Encoding]::UTF8
chcp 65001 | Out-Null

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Applying GoldKiosk DB scripts" -ForegroundColor Cyan
Write-Host ("  Host:     {0}:{1}" -f $PgHost, $Port)
Write-Host ("  User:     {0}" -f $PgUser)
Write-Host ("  Database: {0}" -f $Database)
Write-Host ("  Folder:   {0}" -f $scriptDir)
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

$files = Get-ChildItem -Path $scriptDir -Filter "*.sql" | Sort-Object Name

if ($files.Count -eq 0) {
    Write-Warning "No .sql files found in $scriptDir"
    exit 1
}

$start = Get-Date
$applied = 0

foreach ($f in $files) {
    Write-Host ("> " + $f.Name) -ForegroundColor Yellow

    $psqlArgs = @(
        "--host=$PgHost",
        "--port=$Port",
        "--username=$PgUser",
        "--dbname=$Database",
        "--file=$($f.FullName)",
        "--variable=ON_ERROR_STOP=1",
        "--quiet"
    )

    & psql @psqlArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host ("FAILED on " + $f.Name + " - aborting.") -ForegroundColor Red
        exit $LASTEXITCODE
    }

    $applied = $applied + 1
    Write-Host ""
}

$elapsed = (Get-Date) - $start
Write-Host "================================================================" -ForegroundColor Green
Write-Host ("OK - Applied {0} file(s) in {1:N1}s" -f $applied, $elapsed.TotalSeconds) -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
