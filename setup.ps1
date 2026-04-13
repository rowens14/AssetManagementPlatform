# ==============================================================================
# setup.ps1 — Asset Management Platform — One-Time Setup Script
# ==============================================================================
# Run this script once from the repo root before launching the app for the
# first time. It will:
#   1. Check that .NET 10 and PostgreSQL are installed
#   2. Ask for your PostgreSQL password
#   3. Create the asset_mgmt database
#   4. Generate a secure random JWT key
#   5. Write appsettings.Development.json with your real credentials
#   6. Restore NuGet packages
# After this script completes, open the solution in Visual Studio and press Run,
# or cd into AssetManagement.Server and run: dotnet run
# ==============================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Asset Management Platform - Setup" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# -- Step 1: Check .NET 10 -----------------------------------------------------
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -notlike "10.*") {
        Write-Host "ERROR: .NET 10 SDK is required but found version $dotnetVersion" -ForegroundColor Red
        Write-Host "Download it from: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Red
        exit 1
    }
    Write-Host "  .NET $dotnetVersion found." -ForegroundColor Green
} catch {
    Write-Host "ERROR: dotnet command not found. Install .NET 10 SDK from:" -ForegroundColor Red
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Red
    exit 1
}

# -- Step 2: Find psql ---------------------------------------------------------
Write-Host "Locating PostgreSQL..." -ForegroundColor Yellow
$psqlPath = $null
$pgPaths = @(
    "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    "C:\Program Files\PostgreSQL\16\bin\psql.exe"
)
foreach ($p in $pgPaths) {
    if (Test-Path $p) { $psqlPath = $p; break }
}
if (-not $psqlPath) {
    try {
        $psqlPath = (Get-Command psql -ErrorAction Stop).Source
    } catch {
        Write-Host "ERROR: PostgreSQL not found. Install it from: https://www.postgresql.org/download/windows/" -ForegroundColor Red
        exit 1
    }
}
Write-Host "  PostgreSQL found at: $psqlPath" -ForegroundColor Green

# -- Step 3: Get PostgreSQL password -------------------------------------------
Write-Host ""
Write-Host "Enter your PostgreSQL password for the 'postgres' user:" -ForegroundColor Yellow
$pgPassword = Read-Host -AsSecureString "Password"
$pgPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pgPassword))

# Test the connection
Write-Host "Testing database connection..." -ForegroundColor Yellow
$env:PGPASSWORD = $pgPasswordPlain
try {
    $result = & $psqlPath -U postgres -c "SELECT 1;" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Could not connect to PostgreSQL. Check your password." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Connection successful." -ForegroundColor Green
} catch {
    Write-Host "ERROR: Could not connect to PostgreSQL. Check your password." -ForegroundColor Red
    exit 1
}

# -- Step 4: Create the database -----------------------------------------------
Write-Host "Creating database 'asset_mgmt'..." -ForegroundColor Yellow
$dbExists = & $psqlPath -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname='asset_mgmt';" 2>&1
if ($dbExists -match "1") {
    Write-Host "  Database 'asset_mgmt' already exists, skipping." -ForegroundColor Green
} else {
    & $psqlPath -U postgres -c "CREATE DATABASE asset_mgmt;" 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to create database." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Database 'asset_mgmt' created." -ForegroundColor Green
}

# -- Step 5: Generate JWT key --------------------------------------------------
Write-Host "Generating JWT signing key..." -ForegroundColor Yellow
$jwtKey = [Convert]::ToBase64String((1..48 | ForEach-Object { [byte](Get-Random -Max 256) }))
Write-Host "  JWT key generated." -ForegroundColor Green

# -- Step 6: Write appsettings.Development.json --------------------------------
Write-Host "Writing appsettings.Development.json..." -ForegroundColor Yellow
$serverDir = Join-Path $PSScriptRoot "AssetManagement.Server"
$devSettings = @{
    ConnectionStrings = @{
        AssetDb = "Host=localhost;Port=5432;Database=asset_mgmt;Username=postgres;Password='$pgPasswordPlain'"
    }
    Jwt = @{
        Key           = $jwtKey
        Issuer        = "AssetManagement.Server"
        Audience      = "AssetManagement.Client"
        ExpiryMinutes = 480
    }
    Logging = @{
        LogLevel = @{
            Default                                          = "Debug"
            "Microsoft.AspNetCore"                           = "Warning"
            "Microsoft.EntityFrameworkCore.Database.Command" = "Information"
        }
    }
} | ConvertTo-Json -Depth 5

$devSettingsPath = Join-Path $serverDir "appsettings.Development.json"
Set-Content -Path $devSettingsPath -Value $devSettings -Encoding UTF8
Write-Host "  Written to: $devSettingsPath" -ForegroundColor Green

# -- Step 7: Restore NuGet packages --------------------------------------------
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore (Join-Path $PSScriptRoot "AssetManagement.sln") | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Package restore had issues. Try opening the solution in Visual Studio." -ForegroundColor Yellow
} else {
    Write-Host "  Packages restored." -ForegroundColor Green
}

# -- Done ----------------------------------------------------------------------
$env:PGPASSWORD = ""
Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Setup complete." -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  Option A - Visual Studio:" -ForegroundColor White
Write-Host "    Open AssetManagement.sln, set AssetManagement.Server as" -ForegroundColor Gray
Write-Host "    the startup project, and press Run." -ForegroundColor Gray
Write-Host ""
Write-Host "  Option B - Terminal:" -ForegroundColor White
Write-Host "    cd AssetManagement.Server" -ForegroundColor Gray
Write-Host "    dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  The app will create all database tables and seed demo data" -ForegroundColor Gray
Write-Host "  automatically on first launch." -ForegroundColor Gray
Write-Host ""
Write-Host "  Default logins:" -ForegroundColor White
Write-Host "    admin    / admin123  (Admin)"   -ForegroundColor Gray
Write-Host "    manager  / pass123   (Manager)" -ForegroundColor Gray
Write-Host "    viewer   / view123   (Viewer)"  -ForegroundColor Gray
Write-Host ""
