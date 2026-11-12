#!/usr/bin/env bash
# One-time setup for publishing GlazeAudio to Azure from this repository.
#
# Creates (or reuses) next to your existing Azure SQL database:
#   - an App Service plan + App Service (Linux, .NET 10) for the API
#   - a Static Web App (Free) for the React frontend
# and configures them:
#   - API settings: connection string, JWT signing secret, CORS for the frontend's address
#   - GitHub Actions secrets/variables used by .github/workflows/deploy-*.yml
#     (set automatically with the GitHub CLI `gh` if it's installed and logged in; otherwise printed)
#
# Requirements: Azure CLI (`az login`), optionally GitHub CLI (`gh auth login`).
# Works in Azure Cloud Shell, Linux/macOS, WSL and Git Bash.
# Usage:  ./infra/azure-deploy-setup.sh
# Override names with env vars, e.g.:  API_APP=my-api SKU=B1 ./infra/azure-deploy-setup.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STATE_FILE="$SCRIPT_DIR/.deploy.env"            # remembers generated names (git-ignored)
# shellcheck disable=SC1090
[[ -f "$STATE_FILE" ]] && source "$STATE_FILE"

RESOURCE_GROUP="${RESOURCE_GROUP:-glazeaudio-rg}"
LOCATION="${LOCATION:-swedencentral}"           # App Service region (same as the database)
SWA_LOCATION="${SWA_LOCATION:-westeurope}"      # Static Web Apps only exist in a few regions
SQL_SERVER="${SQL_SERVER:-glazeaudio-sql}"
SQL_DATABASE="${SQL_DATABASE:-glazeaudio-db}"
PLAN="${PLAN:-glazeaudio-plan}"
SKU="${SKU:-F1}"                                # F1 = free; use B1 if F1 isn't allowed in your subscription
API_APP="${API_APP:-glazeaudio-api-$(head -c 300 /dev/urandom | LC_ALL=C tr -dc 'a-z0-9' | cut -c1-6)}"  # globally unique
SWA_NAME="${SWA_NAME:-glazeaudio-web}"

step() { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }
warn() { printf '\033[1;33m%s\033[0m\n' "$*"; }

command -v az >/dev/null || { echo "Missing Azure CLI. Install it (Arch: yay -S azure-cli) or use Azure Cloud Shell."; exit 1; }
az account show >/dev/null 2>&1 || az login >/dev/null
echo "Subscription: $(az account show --query name -o tsv)"

cat > "$STATE_FILE" <<STATE
RESOURCE_GROUP='$RESOURCE_GROUP'
LOCATION='$LOCATION'
API_APP='$API_APP'
SWA_NAME='$SWA_NAME'
PLAN='$PLAN'
STATE

# ---------------------------------------------------------------- App Service (API)
step "App Service plan '$PLAN' ($SKU, Linux)"
if ! az appservice plan show -g "$RESOURCE_GROUP" -n "$PLAN" >/dev/null 2>&1; then
  az appservice plan create -g "$RESOURCE_GROUP" -n "$PLAN" --location "$LOCATION" --sku "$SKU" --is-linux --output none
fi

step "App Service '$API_APP' (.NET 10)"
if ! az webapp show -g "$RESOURCE_GROUP" -n "$API_APP" >/dev/null 2>&1; then
  az webapp create -g "$RESOURCE_GROUP" -p "$PLAN" -n "$API_APP" --runtime "DOTNETCORE:10.0" --output none
fi
az webapp update -g "$RESOURCE_GROUP" -n "$API_APP" --https-only true --output none
API_URL="https://$(az webapp show -g "$RESOURCE_GROUP" -n "$API_APP" --query defaultHostName -o tsv)"
echo "API: $API_URL"

# ---------------------------------------------------------------- Static Web App (frontend)
step "Static Web App '$SWA_NAME' (Free)"
if ! az staticwebapp show -g "$RESOURCE_GROUP" -n "$SWA_NAME" >/dev/null 2>&1; then
  az staticwebapp create -g "$RESOURCE_GROUP" -n "$SWA_NAME" --location "$SWA_LOCATION" --sku Free --output none
fi
WEB_URL="https://$(az staticwebapp show -g "$RESOURCE_GROUP" -n "$SWA_NAME" --query defaultHostname -o tsv)"
echo "Web: $WEB_URL"

# ---------------------------------------------------------------- API configuration
step "API settings (database, JWT secret, CORS)"
read -rp  "SQL admin login for ${SQL_SERVER}: " SQL_USER
read -rsp "SQL password: " SQL_PASSWORD; echo
CONNECTION_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${SQL_DATABASE};User ID=${SQL_USER};Password=${SQL_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"

# Shows up in the app as ConnectionStrings:GlazeAudioDb.
az webapp config connection-string set -g "$RESOURCE_GROUP" -n "$API_APP" \
  --connection-string-type SQLAzure --settings GlazeAudioDb="$CONNECTION_STRING" --output none

# Keep the existing signing key on re-runs, so logged-in users aren't logged out.
EXISTING_KEY="$(az webapp config appsettings list -g "$RESOURCE_GROUP" -n "$API_APP" --query "[?name=='Jwt__SigningKey'].value | [0]" -o tsv 2>/dev/null || true)"
JWT_KEY="${EXISTING_KEY:-$(head -c 48 /dev/urandom | base64 | tr -d '\n')}"

az webapp config appsettings set -g "$RESOURCE_GROUP" -n "$API_APP" --output none --settings \
  "Jwt__SigningKey=$JWT_KEY" \
  "Cors__AllowedOrigins__0=$WEB_URL" \
  "Cors__AllowedOrigins__1=http://localhost:5173" \
  "ASPNETCORE_ENVIRONMENT=Production" \
  "ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"   # trust App Service's X-Forwarded-Proto, so links are https://
echo "Connection string, Jwt__SigningKey and CORS ($WEB_URL) saved."

# The Azure SQL server must let Azure services in (the API runs inside Azure).
az sql server firewall-rule create -g "$RESOURCE_GROUP" -s "$SQL_SERVER" -n AllowAzureServices \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none 2>/dev/null || true

# ---------------------------------------------------------------- deployment credentials
step "Deployment credentials for GitHub Actions"
# Publish profiles need basic-auth publishing, which new App Services have switched off.
az resource update -g "$RESOURCE_GROUP" --namespace Microsoft.Web --resource-type basicPublishingCredentialsPolicies \
  --parent "sites/$API_APP" --name scm --set properties.allow=true --output none
PUBLISH_PROFILE="$(az webapp deployment list-publishing-profiles -g "$RESOURCE_GROUP" -n "$API_APP" --xml)"
SWA_TOKEN="$(az staticwebapp secrets list -g "$RESOURCE_GROUP" -n "$SWA_NAME" --query properties.apiKey -o tsv)"

if command -v gh >/dev/null && gh auth status >/dev/null 2>&1; then
  gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body "$PUBLISH_PROFILE"
  gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$SWA_TOKEN"
  gh variable set AZURE_WEBAPP_NAME --body "$API_APP"
  gh variable set API_URL --body "$API_URL"
  echo "GitHub secrets and variables set for $(gh repo view --json nameWithOwner -q .nameWithOwner)."
else
  OUT_DIR="$SCRIPT_DIR/.deploy-secrets"         # git-ignored
  mkdir -p "$OUT_DIR"
  printf '%s' "$PUBLISH_PROFILE" > "$OUT_DIR/AZURE_WEBAPP_PUBLISH_PROFILE.xml"
  printf '%s' "$SWA_TOKEN" > "$OUT_DIR/AZURE_STATIC_WEB_APPS_API_TOKEN.txt"
  chmod 600 "$OUT_DIR"/*
  warn "GitHub CLI not available - add these in GitHub: repo -> Settings -> Secrets and variables -> Actions"
  warn "  Secret   AZURE_WEBAPP_PUBLISH_PROFILE     = contents of infra/.deploy-secrets/AZURE_WEBAPP_PUBLISH_PROFILE.xml"
  warn "  Secret   AZURE_STATIC_WEB_APPS_API_TOKEN  = contents of infra/.deploy-secrets/AZURE_STATIC_WEB_APPS_API_TOKEN.txt"
  warn "  Variable AZURE_WEBAPP_NAME                = $API_APP"
  warn "  Variable API_URL                          = $API_URL"
fi

step "Done"
echo "API:      $API_URL   (docs: $API_URL/docs)"
echo "Frontend: $WEB_URL"
echo "Push to main (or run the workflows under GitHub -> Actions) to deploy both."
