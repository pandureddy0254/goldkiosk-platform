<#
  Trusts the dev signing certificate (LocalMachine TrustedPeople) and installs the signed
  GoldKiosk kiosk MSIX. Must run elevated to add the machine-trust entry.
  Usage (elevated):  pwsh install.ps1
#>
[CmdletBinding()]
param(
    [string]$Version = "0.1.0.0"
)

$ErrorActionPreference = 'Stop'
$pkgDir = $PSScriptRoot
$outDir = Join-Path $pkgDir "bin"
$msix = Join-Path $outDir "GoldKiosk.Kiosk_$Version`_x64.msix"
$cer = Join-Path $outDir "GoldKiosk-Dev.cer"

if (-not (Test-Path $msix)) { throw "Package not found: $msix — run build.ps1 first." }

Write-Host "Trusting dev certificate (machine TrustedPeople)..." -ForegroundColor Yellow
Import-Certificate -FilePath $cer -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null

# Remove any prior install so a re-run is clean
$existing = Get-AppxPackage -Name "GoldKiosk.Kiosk" -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Removing previous install..." -ForegroundColor Yellow
    Remove-AppxPackage -Package $existing.PackageFullName
}

Write-Host "Installing package..." -ForegroundColor Yellow
Add-AppxPackage -Path $msix

$pkg = Get-AppxPackage -Name "GoldKiosk.Kiosk"
Write-Host "`nINSTALLED: $($pkg.PackageFullName)" -ForegroundColor Green
Write-Host "Launch from the Start menu (GoldKiosk) or:" -ForegroundColor Cyan
Write-Host "  explorer.exe shell:AppsFolder\$($pkg.PackageFamilyName)!GoldKioskKiosk" -ForegroundColor Cyan
