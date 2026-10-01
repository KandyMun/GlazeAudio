# Connects this machine to the EXISTING GlazeAudio Azure SQL database (Windows version of azure-sql-setup.sh).
# Use it on any new device after cloning the repo - it does not create anything in Azure.
#
#   1. Asks for the SQL login (password input is hidden)
#   2. Saves the connection string in .NET user-secrets (never committed to git)
#   3. Allows this device's IP in the Azure SQL firewall (if the Azure CLI is installed)
#   4. Restores packages and applies EF Core migrations + seed data
#
# Requirements: .NET 10 SDK and dotnet-ef (dotnet tool install -g dotnet-ef).
#               Azure CLI (az) is optional - without it, add your IP in the portal.
# Usage (from the repo root, in PowerShell or Windows Terminal):
#   powershell -ExecutionPolicy Bypass -File infra\azure-sql-setup.ps1
#
# Optional parameters:
#   -SqlUser glazeadmin                    skip the login prompt
#   -ConnectionString "Server=..."         skip all prompts
#   -SqlServer / -SqlDatabase / -ResourceGroup   if your names differ

param(
    [string]$ResourceGroup = "glazeaudio-rg",
    [string]$SqlServer = "glazeaudio-sql",       # name only, without .database.windows.net
    [string]$SqlDatabase = "glazeaudio-db",
    [string]$SqlUser = "",
    [string]$ConnectionString = $env:GLAZEAUDIO_CONNECTION_STRING
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ApiProject = Join-Path $RepoRoot "src\GlazeAudio.Api"

function Step([string]$Text) { Write-Host ""; Write-Host "==> $Text" -ForegroundColor Cyan }
function Warn([string]$Text) { Write-Host $Text -ForegroundColor Yellow }
function Fail([string]$Text) { Write-Host $Text -ForegroundColor Red; exit 1 }
function Assert-LastExitCode([string]$What) { if ($LASTEXITCODE -ne 0) { Fail "$What failed (exit code $LASTEXITCODE)." } }

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Fail "Missing .NET SDK. Install it with: winget install Microsoft.DotNet.SDK.10"
}
dotnet ef --version *> $null
if ($LASTEXITCODE -ne 0) { Fail "Missing dotnet-ef. Run: dotnet tool install --global dotnet-ef" }

# ---------------------------------------------------------------- 1. connection string
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Step "Database login"
    Write-Host "Server:   $SqlServer.database.windows.net"
    Write-Host "Database: $SqlDatabase"
    if ([string]::IsNullOrWhiteSpace($SqlUser)) { $SqlUser = Read-Host "SQL admin login" }

    $secure = Read-Host "SQL password" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $SqlPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }

    if ([string]::IsNullOrWhiteSpace($SqlUser) -or [string]::IsNullOrEmpty($SqlPassword)) {
        Fail "Login and password are required."
    }

    $ConnectionString = "Server=tcp:$SqlServer.database.windows.net,1433;Initial Catalog=$SqlDatabase;User ID=$SqlUser;Password=$SqlPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
}

Step "Saving connection string to user-secrets"
dotnet user-secrets set "ConnectionStrings:GlazeAudioDb" "$ConnectionString" --project "$ApiProject" | Out-Null
Assert-LastExitCode "Saving user-secrets"
Write-Host "Saved as ConnectionStrings:GlazeAudioDb (stored outside the repo, in %APPDATA%\Microsoft\UserSecrets)"

# ---------------------------------------------------------------- 2. firewall
Step "Firewall rule for this device"
$MyIp = $null
try { $MyIp = (Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 10).ToString().Trim() } catch { }

$machine = if ($env:COMPUTERNAME) { $env:COMPUTERNAME } else { [Environment]::MachineName }
$cleanName = ($machine -replace '[^A-Za-z0-9-]', '')
if ($cleanName.Length -gt 40) { $cleanName = $cleanName.Substring(0, 40) }
$RuleName = "dev-$cleanName"

$azAvailable = $false
if (Get-Command az -ErrorAction SilentlyContinue) {
    az account show *> $null
    $azAvailable = ($LASTEXITCODE -eq 0)
}

if (-not $MyIp) {
    Warn "Couldn't detect your public IP - add it manually in the portal (SQL server -> Networking)."
}
elseif ($azAvailable) {
    az sql server firewall-rule create --resource-group $ResourceGroup --server $SqlServer `
        --name $RuleName --start-ip-address $MyIp --end-ip-address $MyIp --output none
    Assert-LastExitCode "Creating the firewall rule"
    Write-Host "Allowed $MyIp as rule '$RuleName'."
}
else {
    Warn "Azure CLI not installed or not logged in (az login) - skipping."
    Warn "If the next step fails with 'Client with IP address $MyIp is not allowed', add it in the portal:"
    Warn "  SQL server '$SqlServer' -> Security -> Networking -> Add your client IPv4 address -> Save"
}

# ---------------------------------------------------------------- 3. build + migrate
Step "Restoring packages and building"
dotnet build "$ApiProject" --nologo -v quiet
Assert-LastExitCode "Build"

Step "Applying migrations + seed data"
if (-not (Test-Path (Join-Path $ApiProject "Migrations"))) {
    Warn "No Migrations folder found - creating InitialCreate. Commit it so other devices get it."
    dotnet ef migrations add InitialCreate --project "$ApiProject"
    Assert-LastExitCode "Creating the migration"
}
Write-Host "(The first connection can take ~1 min if the free database was paused.)"
dotnet ef database update --project "$ApiProject" --no-build
Assert-LastExitCode "Applying migrations"

Step "Done"
Write-Host "Run the API:  dotnet run --project src\GlazeAudio.Api   ->  http://localhost:5080/docs"
