#!/usr/bin/env bash
# Creates (or reuses) a free-tier Azure SQL database for GlazeAudio, stores the
# connection string in .NET user-secrets, and applies the EF Core migrations + seed data.
#
# Requirements: Azure CLI (`az`), .NET 10 SDK, dotnet-ef (`dotnet tool install -g dotnet-ef`).
# Usage:        ./infra/azure-sql-setup.sh
# Override any setting with env vars, e.g.:  LOCATION=swedencentral ./infra/azure-sql-setup.sh
#
# Safe to re-run: existing resources are reused. Generated names/passwords are kept in
# infra/.azure-sql.env (git-ignored) so the next run finds the same server.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
API_PROJECT="$REPO_ROOT/src/GlazeAudio.Api"
STATE_FILE="$SCRIPT_DIR/.azure-sql.env"

# Load values from a previous run (server name, admin password).
# shellcheck disable=SC1090
[[ -f "$STATE_FILE" ]] && source "$STATE_FILE"

RESOURCE_GROUP="${RESOURCE_GROUP:-glazeaudio-rg}"
LOCATION="${LOCATION:-northeurope}"
SQL_SERVER="${SQL_SERVER:-glazeaudio-sql-$(head -c 300 /dev/urandom | LC_ALL=C tr -dc 'a-z0-9' | cut -c1-6)}"   # must be globally unique
SQL_DATABASE="${SQL_DATABASE:-GlazeAudioDB}"
SQL_ADMIN="${SQL_ADMIN:-glazeadmin}"
SQL_PASSWORD="${SQL_PASSWORD:-$(head -c 300 /dev/urandom | LC_ALL=C tr -dc 'A-Za-z0-9' | cut -c1-20)Aa1!}"

step() { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }

for tool in az dotnet; do
  command -v "$tool" >/dev/null || { echo "Missing '$tool'. Install it first (see README)."; exit 1; }
done
dotnet ef --version >/dev/null 2>&1 || { echo "Missing dotnet-ef. Run: dotnet tool install --global dotnet-ef"; exit 1; }

step "Checking Azure login"
az account show >/dev/null 2>&1 || az login >/dev/null
echo "Subscription: $(az account show --query name -o tsv)"

step "Resource group '$RESOURCE_GROUP' in $LOCATION"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

step "SQL server '$SQL_SERVER'"
if az sql server show --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
  echo "Exists – resetting the admin password so the saved connection string is valid."
  az sql server update --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" \
    --admin-password "$SQL_PASSWORD" --output none
else
  az sql server create --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" \
    --admin-user "$SQL_ADMIN" --admin-password "$SQL_PASSWORD" --output none
fi

# Save state right away so a failure later doesn't lose the generated name/password.
cat > "$STATE_FILE" <<STATE
RESOURCE_GROUP='$RESOURCE_GROUP'
LOCATION='$LOCATION'
SQL_SERVER='$SQL_SERVER'
SQL_DATABASE='$SQL_DATABASE'
SQL_ADMIN='$SQL_ADMIN'
SQL_PASSWORD='$SQL_PASSWORD'
STATE
chmod 600 "$STATE_FILE"

step "Firewall rules"
MY_IP="$(curl -fsS https://api.ipify.org)"
az sql server firewall-rule create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
  --name AllowDevMachine --start-ip-address "$MY_IP" --end-ip-address "$MY_IP" --output none
echo "Allowed your IP: $MY_IP"
# 0.0.0.0 is Azure's special value for "allow Azure services" (needed later by App Service).
az sql server firewall-rule create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none

step "Database '$SQL_DATABASE' (free offer, auto-pauses instead of billing)"
if az sql db show --name "$SQL_DATABASE" --server "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
  echo "Exists – reusing it."
else
  az sql db create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" --name "$SQL_DATABASE" \
    --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless \
    --use-free-limit --free-limit-exhaustion-behavior AutoPause --output none
fi

CONNECTION_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${SQL_DATABASE};User ID=${SQL_ADMIN};Password=${SQL_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"

step "Saving connection string to user-secrets (never committed to git)"
dotnet user-secrets set "ConnectionStrings:GlazeAudioDb" "$CONNECTION_STRING" --project "$API_PROJECT" >/dev/null
echo "Saved as ConnectionStrings:GlazeAudioDb"

step "EF Core migrations"
if [[ ! -d "$API_PROJECT/Migrations" ]]; then
  dotnet ef migrations add InitialCreate --project "$API_PROJECT"
  echo "Created src/GlazeAudio.Api/Migrations – commit this folder."
fi
echo "Applying migrations + seed data (the first connection can take ~1 min if the DB was paused)..."
dotnet ef database update --project "$API_PROJECT"

step "Done"
echo "Server:   ${SQL_SERVER}.database.windows.net"
echo "Database: ${SQL_DATABASE}"
echo "Run the API:  dotnet run --project src/GlazeAudio.Api   →  http://localhost:5080/docs"
