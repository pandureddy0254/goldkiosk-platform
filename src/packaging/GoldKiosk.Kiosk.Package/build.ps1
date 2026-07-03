<#
  Builds the GoldKiosk kiosk MSIX: publishes Kiosk.UI (entry app) + Kiosk.Api (logon-launched
  edge API, under api\), stages the manifest + visual assets, packs with makeappx, and signs
  with a dev self-signed certificate.

  Local dev only. Production packages are pipeline-signed with the EV cert (never in the repo).
  Usage:  pwsh build.ps1 [-Version 0.1.0.0] [-Configuration Release]
#>
[CmdletBinding()]
param(
    [string]$Version = "0.1.0.0",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = 'Stop'
$pkgDir = $PSScriptRoot
$repo = (Resolve-Path "$pkgDir\..\..\..").Path
$staging = Join-Path $pkgDir "obj\stage"
$outDir = Join-Path $pkgDir "bin"
$publisher = "CN=GoldKiosk Dev, O=AI Kiosks International Inc"

$sdkBin = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64"
$makeappx = Join-Path $sdkBin "makeappx.exe"
$signtool = Join-Path $sdkBin "signtool.exe"

Write-Host "== GoldKiosk MSIX build ($Version, $Configuration/$Runtime) ==" -ForegroundColor Cyan

# 1. Clean staging
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force $staging | Out-Null
New-Item -ItemType Directory -Force $outDir | Out-Null

# 2. Publish the UI (entry app) to the package root, self-contained so the machine needs no runtime
Write-Host "Publishing Kiosk.UI..." -ForegroundColor Yellow
dotnet publish "$repo\src\kiosk\GoldKiosk.Kiosk.UI\GoldKiosk.Kiosk.UI.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=false -p:DebugType=none `
    -o $staging | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Kiosk.UI publish failed" }

# 3. Publish the edge API under api\
Write-Host "Publishing Kiosk.Api..." -ForegroundColor Yellow
$apiOut = Join-Path $staging "api"
dotnet publish "$repo\src\kiosk\GoldKiosk.Kiosk.Api\GoldKiosk.Kiosk.Api.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=false -p:DebugType=none `
    -o $apiOut | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Kiosk.Api publish failed" }

# 4. Patch the published UI config: launch + supervise the edge API in the packaged kiosk
$uiSettingsPath = Join-Path $staging "appsettings.json"
$uiSettings = Get-Content $uiSettingsPath -Raw | ConvertFrom-Json
if (-not $uiSettings.KioskUi) { $uiSettings | Add-Member -NotePropertyName KioskUi -NotePropertyValue ([pscustomobject]@{}) }
$uiSettings.KioskUi | Add-Member -NotePropertyName LaunchLocalApi -NotePropertyValue $true -Force
$uiSettings.KioskUi | Add-Member -NotePropertyName ApiBaseUrl -NotePropertyValue "http://localhost:5201" -Force
($uiSettings | ConvertTo-Json -Depth 12) | Set-Content -Encoding utf8 $uiSettingsPath

# 5. Patch the published API config: MSIX install dir is read-only, so data + logs go to ProgramData
$apiSettingsPath = Join-Path $apiOut "appsettings.json"
$raw = Get-Content $apiSettingsPath -Raw
$raw = $raw.Replace('./data/transactions', 'C:/ProgramData/GoldKiosk/transactions')
$raw = $raw.Replace('./logs/kiosk-api-.log', 'C:/ProgramData/GoldKiosk/logs/kiosk-api-.log')
$apiJson = $raw | ConvertFrom-Json
$apiJson | Add-Member -NotePropertyName Urls -NotePropertyValue "http://localhost:5201" -Force
($apiJson | ConvertTo-Json -Depth 12) | Set-Content -Encoding utf8 $apiSettingsPath

# 6. Manifest (version-stamped) + visual assets. Case-sensitive replace so the lowercase
#    `version` in the XML declaration is untouched; write BOM-free for makeappx.
$manifest = Get-Content (Join-Path $pkgDir "AppxManifest.xml") -Raw
$manifest = $manifest -creplace 'Version="[0-9.]+"', "Version=`"$Version`""
[IO.File]::WriteAllText((Join-Path $staging "AppxManifest.xml"), $manifest, (New-Object Text.UTF8Encoding($false)))
Copy-Item (Join-Path $pkgDir "Images") (Join-Path $staging "Images") -Recurse -Force

# 7. Pack
$msixPath = Join-Path $outDir "GoldKiosk.Kiosk_$Version`_x64.msix"
if (Test-Path $msixPath) { Remove-Item $msixPath -Force }
Write-Host "Packing MSIX..." -ForegroundColor Yellow
& $makeappx pack /d $staging /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed" }

# 8. Dev signing certificate (create + trust once; reused thereafter)
$certPath = Join-Path $pkgDir "obj\GoldKiosk-Dev.pfx"
$certPwd = ConvertTo-SecureString "goldkiosk-dev" -AsPlainText -Force
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $publisher } | Select-Object -First 1
if (-not $cert) {
    Write-Host "Creating dev signing certificate..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -Type Custom -Subject $publisher `
        -KeyUsage DigitalSignature -FriendlyName "GoldKiosk Dev Signing" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
}
Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $certPwd | Out-Null

# 9. Sign
Write-Host "Signing MSIX..." -ForegroundColor Yellow
& $signtool sign /fd SHA256 /a /f $certPath /p "goldkiosk-dev" $msixPath
if ($LASTEXITCODE -ne 0) { throw "signtool sign failed" }

# Export the public cert so install.ps1 can trust it
$cerPath = Join-Path $outDir "GoldKiosk-Dev.cer"
Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null

Write-Host "`nBUILT: $msixPath" -ForegroundColor Green
Write-Host "CERT:  $cerPath" -ForegroundColor Green
Write-Host "Install with:  pwsh install.ps1" -ForegroundColor Cyan
