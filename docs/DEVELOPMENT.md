# GlazeAudio: developer guide

Technical documentation for running, testing and deploying GlazeAudio. The project report (in Lithuanian) is the main [README](../README.md).

One repository holds the whole application:

- **API:** ASP.NET Core 10 (Minimal APIs), EF Core and an Azure SQL (MSSQL) database, with JWT authentication. Deployed to **Azure App Service**.
- **Frontend:** React + TypeScript (Vite). Deployed to **Azure Static Web Apps**.

```
GlazeAudio/
├── src/GlazeAudio.Api/     ASP.NET Core API (endpoints, auth, EF Core model, migrations, seed data)
├── web/                    React + TypeScript frontend (public/covers: album cover images)
├── infra/                  azure-sql-setup.sh / .ps1 (connect a device to the database),
│                           azure-deploy-setup.sh (create the App Service + Static Web App)
├── .github/workflows/      deploy-api.yml, deploy-web.yml (GitHub Actions → Azure)
└── docs/                   api-spec.yaml (OpenAPI), postman/ (collection + environments),
                            report/ (deployment diagram, wireframes, screenshots), DEVELOPMENT.md
```

## Resources and API methods

Resources are hierarchical: **Album → Song → Review**. Each resource has 5 methods, for **15** in total.

| # | Method | Route | Who | Success | Errors |
|---|--------|-------|-----|---------|--------|
| 1 | GET    | `/api/albums` | anyone | 200 | 400 |
| 2 | GET    | `/api/albums/{albumId}` | anyone | 200 | 404 |
| 3 | POST   | `/api/albums` | admin | 201 + `Location` | 400, 401, 403, 422 |
| 4 | PUT    | `/api/albums/{albumId}` | admin | 200 | 400, 401, 403, 404, 422 |
| 5 | DELETE | `/api/albums/{albumId}` | admin | 204 | 401, 403, 404 |
| 6 | GET    | `/api/albums/{albumId}/songs` | anyone | 200 | 400, 404 |
| 7 | GET    | `/api/albums/{albumId}/songs/{songId}` | anyone | 200 | 404 |
| 8 | POST   | `/api/albums/{albumId}/songs` | admin | 201 + `Location` | 400, 401, 403, 404, 422 |
| 9 | PUT    | `/api/albums/{albumId}/songs/{songId}` | admin | 200 | 400, 401, 403, 404, 422 |
| 10 | DELETE | `/api/albums/{albumId}/songs/{songId}` | admin | 204 | 401, 403, 404 |
| 11 | GET    | `/api/albums/{albumId}/songs/{songId}/reviews` | anyone | 200 | 400, 404 |
| 12 | GET    | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | anyone | 200 | 404 |
| 13 | POST   | `/api/albums/{albumId}/songs/{songId}/reviews` | logged in | 201 + `Location` | 400, 401, 404, 409, 422 |
| 14 | PUT    | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | author or admin | 200 | 400, 401, 403, 404, 422 |
| 15 | DELETE | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | author or admin | 204 | 401, 403, 404 |

An album's `coverUrl` is either an `http(s)://` address or a path on the website such as `/covers/cobalt-drip.jpg`. The frontend ships eight cover images in `web/public/covers/`, and admins pick one in the album form.

Two more endpoints:

| Method | Route | What it returns |
|--------|-------|-----------------|
| GET | `/api` | API entry point with links to the main resources |
| GET | `/api/albums/{albumId}/overview` | Album overview built from the album, its songs and their reviews (see below) |

**401 Unauthorized** means there's no access token, or it's invalid or expired; the response has a `WWW-Authenticate: Bearer` header. **403 Forbidden** means you're logged in, but your role or ownership doesn't allow the action, e.g. a regular user creating an album or editing someone else's review. All errors use `application/problem+json`.

## Authentication (JWT)

Logging in returns two tokens:

- **Access token:** a signed JWT, valid **15 minutes**. Send it with every request as `Authorization: Bearer <accessToken>`. It contains the user's id (`sub`), username (`name`) and **role (`role`)**. The API decides what you may do from the role inside the token.
- **Refresh token:** a random string, valid **7 days**, stored in the database (as a SHA-256 hash). Exchange it for a new pair when the access token expires. Each refresh token works **once**: refreshing replaces it with a new one. If an already used token is presented again (a sign it was stolen), all of that user's sessions are logged out.

| Method | Route | Who | Success | Errors |
|--------|-------|-----|---------|--------|
| POST | `/api/auth/register` | anyone | 201 + tokens | 400, 409, 422 |
| POST | `/api/auth/login` (`login` = username or email) | anyone | 200 + tokens | 400, 401, 422 |
| POST | `/api/auth/refresh` | anyone with a refresh token | 200 + new tokens | 401 |
| POST | `/api/auth/logout` (revokes the refresh token) | anyone | 204 | |
| GET  | `/api/auth/me` | logged in | 200 | 401 |
| PUT  | `/api/auth/password` (logs out all other sessions) | logged in | 204 | 401, 422 |

Roles:
- **Guest** (no token): browse albums, songs, reviews and public profiles.
- **User:** also write reviews, and edit or delete their own reviews and account.
- **Admin:** also manage albums and songs, remove any review, list users, change roles and delete accounts.

The token signing secret is `Jwt:SigningKey`. It's never committed: locally it's in user-secrets (the setup scripts create it), and in Azure it's the App Service setting `Jwt__SigningKey`.

In Swagger UI (`/docs`), click **Authorize** and paste an access token to try the protected endpoints.

## User accounts

Users aren't a domain object (the requirements exclude the users table from the 3 objects). They own reviews and decide what each person may do.

| Method | Route | Who | Success | Errors |
|--------|-------|-----|---------|--------|
| GET    | `/api/users` (paged; filters `search`, `role`) | admin | 200 | 400, 401, 403 |
| GET    | `/api/users/{userId}` | anyone (email only for the user and admins) | 200 | 404 |
| GET    | `/api/users/{userId}/reviews` (paged; filter `minRating`) | anyone | 200 | 400, 404 |
| POST   | `/api/users` (people register via `/api/auth/register`) | admin | 201 + `Location` | 400, 401, 403, 409, 422 |
| PUT    | `/api/users/{userId}` (`role` optional, admins only) | the user or admin | 200 | 400, 401, 403, 404, 409, 422 |
| DELETE | `/api/users/{userId}` | the user or admin | 204 | 401, 403, 404 |

- **Fields:** `username` (unique; letters, digits, `_ . -`), `email` (unique), `role` (`User` or `Admin`), `bio`, `createdAt`, `reviewCount`. The password is stored as a salted PBKDF2 hash (ASP.NET Core Identity's `PasswordHasher`) and is never returned.
- **409 Conflict** is returned when the username or email is already taken (case-insensitive), or when a user tries to review the same song twice.
- **Reviews are linked to users.** The author of a new review is the logged-in user from the token, and it can't be changed. Each review includes `userId`, `username` and an `author` link. A user can review a song only once. Deleting a user also deletes their reviews.
- **`GET /api/users/{userId}/reviews`** is the user's profile view: each review together with its song and album.

**Demo accounts** (seeded): `admin` (Admin), `mantas`, `vinyl_owl`, `bassline_ben`, `quietstorm` and `dj_lina` (User). They all have the password `GlazeAudio123!`. **Change the admin password** (`PUT /api/auth/password`, or the profile page) before sharing a deployed site, because this README is public.

## Pagination, filtering and hypermedia

### Pagination

All three list endpoints are paged with `page` (default 1) and `pageSize` (default 10, max 50). The response wraps the items:

```
GET /api/albums?page=1&pageSize=2
```
```json
{
  "items": [ { "id": 1, "title": "OK Computer", ... }, { "id": 2, ... } ],
  "page": 1, "pageSize": 2, "totalCount": 5, "totalPages": 3,
  "_links": {
    "self":  { "href": "http://localhost:5080/api/albums?page=1&pageSize=2", "method": "GET" },
    "first": { "href": "...?page=1&pageSize=2", "method": "GET" },
    "last":  { "href": "...?page=3&pageSize=2", "method": "GET" },
    "next":  { "href": "...?page=2&pageSize=2", "method": "GET" }
  }
}
```

### Filtering

Filters are query parameters on the list endpoints. They can be combined with each other and with paging, and the paging links keep them.

| Endpoint | Filters |
|---|---|
| `GET /api/albums` | `search` (title or artist), `artist`, `genre`, `fromYear`, `toYear` |
| `GET /api/albums/{albumId}/songs` | `search` (title), `minDuration`, `maxDuration` (seconds) |
| `GET /api/albums/{albumId}/songs/{songId}/reviews` | `author`, `minRating`, `maxRating` (overall rating, 0–5) |

Text filters are case-insensitive, e.g. `GET /api/albums?genre=rock&fromYear=1990` or `GET .../reviews?author=mantas&minRating=4`.

### Hypermedia

Every album, song and review, every page, and the API root include a `_links` object. It tells the client what it can do next and which HTTP method to use, so a client can start at `GET /api` and navigate by following links instead of building URLs:

| Resource | Links |
|---|---|
| Album | `self`, `update` (PUT), `delete` (DELETE), `songs`, `createSong` (POST), `overview`, `albums` |
| Song | `self`, `update`, `delete`, `reviews`, `createReview`, `album` |
| Review | `self`, `update`, `delete`, `song`, `album` |
| Page | `self`, `first`, `last`, and `prev` / `next` when they exist |

### Album overview (resource built from several entities)

`GET /api/albums/{albumId}/overview` is a dashboard-style resource assembled from **three entities**: Album, Song and Review.

- `album`: the album's details
- `stats`: song count, review count, total duration, average rating, and the average of each aspect (lyrics, melody, mood, expressiveness) across all reviews
- `songs`: every song with its review count and average rating
- `topRatedSong`: the highest-rated song
- `latestReviews`: the 5 newest reviews on the album, with the song title of each

## OpenAPI specification

- Specification file: [`docs/api-spec.yaml`](api-spec.yaml) (OpenAPI 3.0), with the response codes and a recorded request/response example for every method. Open it in [Swagger Editor](https://editor.swagger.io) or import it into Postman.
- Live spec from the running API: `GET /openapi/v1.json`
- Swagger UI for trying requests locally: `http://localhost:5080/docs`. The root URL `/` redirects here.

## Getting started

### Prerequisites (Linux)

On Windows, see [Setup on Windows](#setup-on-windows) instead.

- [.NET 10 SDK](https://dotnet.microsoft.com/download). On Arch: `sudo pacman -S dotnet-sdk aspnet-runtime dotnet-targeting-pack aspnet-targeting-pack`
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli). On Arch: `yay -S azure-cli`
- [Node.js 22+](https://nodejs.org) for the frontend. On Arch: `sudo pacman -S nodejs npm`
- Postman 12 (`yay -S postman-bin`), or the Postman CLI (`npm i -g postman-cli`) to run the tests from the terminal

### 1. Connect to the Azure SQL database

The database already exists in Azure: server `glazeaudio-sql.database.windows.net`, database `glazeaudio-db`, resource group `glazeaudio-rg`, region Sweden Central. On each new device, run:

```bash
./infra/azure-sql-setup.sh
```

The script:
1. Asks for the SQL admin login and password. The password input is hidden.
2. Stores the connection string, and a JWT signing secret if there isn't one yet, in **.NET user-secrets**, outside the repo, so no secret ends up in git.
3. Adds a firewall rule for this device's IP (`dev-<hostname>`). This needs the Azure CLI and `az login`; without them, the script tells you how to add the IP in the portal.
4. Restores packages, builds, and runs `dotnet ef database update`. That applies any new migrations and seeds the database if it's empty. Running it on a database that's already set up does nothing harmful.

<details>
<summary>Manual setup (what the script does)</summary>

```bash
dotnet user-secrets set "ConnectionStrings:GlazeAudioDb" "Server=tcp:glazeaudio-sql.database.windows.net,1433;Initial Catalog=glazeaudio-db;User ID=<user>;Password=<password>;Encrypt=True;Connection Timeout=60;" --project src/GlazeAudio.Api
dotnet user-secrets set "Jwt:SigningKey" "$(head -c 48 /dev/urandom | base64)" --project src/GlazeAudio.Api
dotnet build src/GlazeAudio.Api
dotnet ef database update --project src/GlazeAudio.Api
```

Then allow your IP: Azure Portal → SQL server `glazeaudio-sql` → Security → Networking → **Add your client IPv4 address** → Save.
</details>

### 2. Run the API

```bash
dotnet run --project src/GlazeAudio.Api
```

> **Already set up before login was added?** The API now needs a JWT signing secret. Re-run the setup script (it adds one if it's missing), or set one yourself:
> `dotnet user-secrets set "Jwt:SigningKey" "$(head -c 48 /dev/urandom | base64)" --project src/GlazeAudio.Api`

The API listens on **http://localhost:5080**. On startup it also applies any pending migrations and seeds an empty database: 5 albums (with cover images), 19 songs and 25 reviews.

> The free Azure SQL database pauses when idle. The first request after a pause can take up to a minute while it resumes, and EF Core retries automatically. Make one request before a demo to wake it up.

### 3. Run the frontend

In a second terminal, with the API running:

```bash
cd web
npm install        # first time only
npm run dev
```

Open **http://localhost:5173**. Vite forwards `/api` requests to the API on port 5080. Log in with a demo account, e.g. `mantas` / `GlazeAudio123!`.

### 4. Run the Postman demo

The collection and environments are in `docs/postman/`.

**In the Postman app**
1. Click **Import** and select `GlazeAudio.postman_collection.json` and `Local.postman_environment.json` from `docs/postman/`. If Postman asks how to import them, choose **Postman Collection**, not OpenAPI.
2. Select the **GlazeAudio - Local** environment.
3. On the **GlazeAudio API** collection, choose **⋯ → Run collection → Run**.

Don't import `docs/api-spec.yaml` as a collection. Postman would generate requests with placeholder IDs and no tests.

**From the terminal** (Postman CLI: `npm i -g postman-cli`):

```bash
postman collection run docs/postman/GlazeAudio.postman_collection.json -e docs/postman/Local.postman_environment.json
```

The collection runs 63 requests with 123 assertions in a few seconds:

| Folder | What it shows |
|---|---|
| 0. Auth (JWT) | register (201 + tokens), admin login with the role checked inside the token, 401 wrong password, `me`, refresh (token rotation), 401 for a reused refresh token, login by email, 401 without or with an invalid token |
| 1. Users | list as admin (200) and as a user (403), own profile with email, public profile without it, update own profile, 403 for a user making themself admin |
| 2. Albums | public list, 401 without a token, 403 as a user, POST as admin (201 + Location), GET one, PUT |
| 3. Songs | POST (admin), list, GET one, PUT |
| 4. Reviews | 401 when logged out, POST as the user (author from the token), list, GET one, edit own review |
| 5. Paging, filtering, hypermedia, overview | API root, page 1 → follow the `next` link, filters on albums / songs / reviews / users, follow an album's `songs` link, a user's reviews, album overview, 400 for invalid paging |
| 6. Error cases | 404s, 400 malformed JSON and wrong type, 422 invalid fields, 409 username taken, 422 invalid registration, 409 duplicate review, 422 rating out of range |
| 7. Delete, logout | 403 for a user deleting a song, delete own review, delete song and album as admin, 404 afterwards, logout and 401 when refreshing after it, delete own account |

The collection registers its own user, creates its own album, song and review, and deletes them at the end, so you can run it as many times as you like. If you change the admin password, update `adminPassword` in the environment.

## Deploying to Azure

Everything deploys from this one repository, as three Azure resources:

| Part | Azure service | Deployed by |
|---|---|---|
| Database | Azure SQL Database (free offer) | already set up; migrations run when the API starts |
| API (`src/`) | App Service (Linux, .NET 10, F1 free plan) | `.github/workflows/deploy-api.yml` on every push to `main` that changes `src/` |
| Frontend (`web/`) | Static Web Apps (Free) | `.github/workflows/deploy-web.yml` on every push to `main` that changes `web/` |

**One-time setup:**

```bash
az login
gh auth login            # optional: lets the script add the GitHub secrets for you (Arch: sudo pacman -S github-cli)
./infra/azure-deploy-setup.sh
```

The script:
1. Creates the App Service plan and App Service (API) and the Static Web App (frontend) in `glazeaudio-rg`.
2. Sets the API's connection string, a new JWT signing secret, and CORS so that only the frontend's address (and `localhost:5173`) may call it.
3. Adds the GitHub Actions secrets and variables (`AZURE_WEBAPP_PUBLISH_PROFILE`, `AZURE_STATIC_WEB_APPS_API_TOKEN`, `AZURE_WEBAPP_NAME`, `API_URL`). Without the GitHub CLI it saves them to `infra/.deploy-secrets/` (git-ignored) and prints where to paste them: GitHub → repo **Settings → Secrets and variables → Actions**.

Then push to `main`, or run both workflows by hand under GitHub → **Actions**. The script prints the two addresses: the frontend (`https://<name>.azurestaticapps.net`) and the API (`https://glazeaudio-api-<suffix>.azurewebsites.net`, with docs at `/docs`).

Notes:
- **F1 (free) App Service** sleeps when idle and has a daily CPU limit, so the first request can be slow. If F1 isn't allowed in your subscription, run the script with `SKU=B1`.
- The script can be re-run safely. It reuses the resources and keeps the existing JWT secret, so nobody is logged out.
- For Postman against Azure, import `docs/postman/Azure.postman_environment.json` and set `baseUrl` to the API address.

## Setup on Windows

Everything works the same on Windows. Only the install commands and the setup script differ. Run these in **PowerShell** or **Windows Terminal**.

### Prerequisites

```powershell
winget install Git.Git
winget install Microsoft.DotNet.SDK.10
winget install Microsoft.AzureCLI          # optional, lets the script add the firewall rule for you
winget install Postman.Postman
```

**Close and reopen the terminal** so the new tools are on your `PATH`, then:

```powershell
dotnet tool install --global dotnet-ef
az login                                   # only if you installed the Azure CLI
```

### 1. Connect to the database

From the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File infra\azure-sql-setup.ps1
```

This does the same as `azure-sql-setup.sh`: it asks for the SQL login, saves the connection string in user-secrets (`%APPDATA%\Microsoft\UserSecrets`), adds a firewall rule for this PC, builds, and applies the migrations. `-ExecutionPolicy Bypass` is needed because Windows blocks unsigned scripts by default; it applies only to this one run.

If you have Git Bash, `./infra/azure-sql-setup.sh` works there too.

### 2. Run the API

```powershell
dotnet run --project src\GlazeAudio.Api
```

Then open http://localhost:5080/docs. If you use **Visual Studio** or **Rider**, open `GlazeAudio.slnx` and start the **http** profile. It uses the same port, 5080.

### 3. Run the frontend

Install Node.js first (`winget install OpenJS.NodeJS.LTS`), then in a second terminal:

```powershell
cd web
npm install
npm run dev
```

Open http://localhost:5173.

### 4. Run the Postman demo

The steps are the same as on Linux (see [Run the Postman demo](#4-run-the-postman-demo)). From the terminal:

```powershell
npm i -g postman-cli
postman collection run docs\postman\GlazeAudio.postman_collection.json -e docs\postman\Local.postman_environment.json
```

To deploy from Windows, run `./infra/azure-deploy-setup.sh` in Git Bash or in [Azure Cloud Shell](https://shell.azure.com).

### Windows troubleshooting

- **`dotnet` or `dotnet ef` is not recognized:** reopen the terminal after installing. dotnet-ef lives in `%USERPROFILE%\.dotnet\tools`.
- **"Client with IP address … is not allowed":** the firewall rule wasn't added. Run `az login` and re-run the script, or add your IP in the portal (SQL server → Security → Networking).
- **Port 5080 already in use:** stop the other `dotnet` process, or change `applicationUrl` in `src\GlazeAudio.Api\Properties\launchSettings.json` and update `baseUrl` in the Postman environment to match.

## Project structure

```
src/GlazeAudio.Api/
├── Program.cs                 DI, EF Core, auth, CORS, OpenAPI, error handling, endpoint mapping
├── Models/                    Album, Song, Review, User, RefreshToken (EF Core entities)
├── Data/                      GlazeAudioDbContext, SeedData (demo users + albums)
├── Migrations/                InitialCreate, AddUsers, AddRefreshTokens, AddSeedAlbumCovers (data only)
├── Contracts/                 Request/response records, query parameters, overview, auth + validation rules
├── Endpoints/                 AuthEndpoints, AlbumEndpoints, SongEndpoints, ReviewEndpoints, UserEndpoints
└── Infrastructure/            Auth/ (JWT token service, bearer handler, roles), hypermedia links, paging,
                               422 validation filter, 400 handler, Swagger UI page

web/
├── index.html                 Google Fonts (Bricolage Grotesque, Atkinson Hyperlegible)
├── public/staticwebapp.config.json   SPA routing fallback for Azure Static Web Apps
├── public/covers/             album cover images (original artwork made for this project)
└── src/
    ├── api/                   fetch client (tokens, automatic refresh), auth context, API types
    ├── components/            layout (header, responsive menu, footer), modal, confirm dialog, toasts, rating bands,
    │                          review form, album and song forms (admin), pagination
    ├── pages/                 albums, album (admin: edit/delete album, add/edit/delete songs), song (reviews),
    │                          login, register, profile (account settings, delete account)
    └── styles.css             design tokens, layout and the 768px breakpoint
```
