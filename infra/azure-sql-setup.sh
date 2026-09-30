#!/usr/bin/env bash
# Connects this machine to the EXISTING GlazeAudio Azure SQL database.
# Use it on any new device after cloning the repo – it does not create anything in Azure.
#
#   1. Asks for the SQL login (password input is hidden)
#   2. Saves the connection string in .NET user-secrets (never committed to git)
#   3. Allows this device's IP in the Azure SQL firewall (if the Azure CLI is installed)
#   4. Restores packages and applies EF Core migrations + seed data
#
# Requirements: .NET 10 SDK and dotnet-ef (`dotnet tool install -g dotnet-ef`).
#               Azure CLI (`az`) is optional – without it, add your IP in the portal.
# Usage:        ./infra/azure-sql-setup.sh
#
# Defaults can be overridden with env vars, e.g.  SQL_USER=glazeadmin ./infra/azure-sql-setup.sh
# or skip the prompts entirely:                  GLAZEAUDIO_CONNECTION_STRING="Server=..." ./infra/azure-sql-setup.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
API_PROJECT="$REPO_ROOT/src/GlazeAudio.Api"

RESOURCE_GROUP="${RESOURCE_GROUP:-glazeaudio-rg}"
SQL_SERVER="${SQL_SERVER:-glazeaudio-sql}"        # name only, without .database.windows.net
SQL_DATABASE="${SQL_DATABASE:-glazeaudio-db}"

step() { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }
warn() { printf '\033[1;33m%s\033[0m\n' "$*"; }

command -v dotnet >/dev/null || { echo "Missing .NET SDK – install dotnet-sdk first."; exit 1; }
dotnet ef --version >/dev/null 2>&1 || { echo "Missing dotnet-ef. Run: dotnet tool install --global dotnet-ef"; exit 1; }

# ---------------------------------------------------------------- 1. connection string
if [[ -n "${GLAZEAUDIO_CONNECTION_STRING:-}" ]]; then
  CONNECTION_STRING="$GLAZEAUDIO_CONNECTION_STRING"
else
  step "Database login"
  echo "Server:   ${SQL_SERVER}.database.windows.net"
  echo "Database: ${SQL_DATABASE}"
  [[ -n "${SQL_USER:-}" ]] || read -rp "SQL admin login: " SQL_USER
  read -rsp "SQL password: " SQL_PASSWORD; echo
  [[ -n "$SQL_USER" && -n "$SQL_PASSWORD" ]] || { echo "Login and password are required."; exit 1; }

  CONNECTION_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${SQL_DATABASE};User ID=${SQL_USER};Password=${SQL_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
fi

step "Saving connection string to user-secrets"
dotnet user-secrets set "ConnectionStrings:GlazeAudioDb" "$CONNECTION_STRING" --project "$API_PROJECT" >/dev/null
echo "Saved as ConnectionStrings:GlazeAudioDb (stored outside the repo, in ~/.microsoft/usersecrets)"

# ---------------------------------------------------------------- 2. firewall
step "Firewall rule for this device"
MY_IP="$(curl -fsS https://api.ipify.org || true)"
RULE_NAME="dev-$(hostname | tr -cd 'A-Za-z0-9-' | cut -c1-40)"

if [[ -z "$MY_IP" ]]; then
  warn "Couldn't detect your public IP – add it manually in the portal (SQL server → Networking)."
elif command -v az >/dev/null && az account show >/dev/null 2>&1; then
  az sql server firewall-rule create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
    --name "$RULE_NAME" --start-ip-address "$MY_IP" --end-ip-address "$MY_IP" --output none
  echo "Allowed $MY_IP as rule '$RULE_NAME'."
else
  warn "Azure CLI not installed or not logged in (az login) – skipping."
  warn "If the next step fails with 'Client with IP address $MY_IP is not allowed', add it in the portal:"
  warn "  SQL server '$SQL_SERVER' → Security → Networking → Add your client IPv4 address → Save"
fi

# ---------------------------------------------------------------- 3. build + migrate
step "Restoring packages and building"
dotnet build "$API_PROJECT" --nologo -v quiet

step "Applying migrations + seed data"
if [[ ! -d "$API_PROJECT/Migrations" ]]; then
  warn "No Migrations folder found – creating InitialCreate. Commit it so other devices get it."
  dotnet ef migrations add InitialCreate --project "$API_PROJECT"
fi
echo "(The first connection can take ~1 min if the free database was paused.)"
dotnet ef database update --project "$API_PROJECT" --no-build

step "Done"
echo "Run the API:  dotnet run --project src/GlazeAudio.Api   →  http://localhost:5080/docs"
