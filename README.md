# GlazeAudio
A webpage for sharing your thoughts about music albums and their songs.

This repository currently contains the **GlazeAudio REST API**: ASP.NET Core 10 (Minimal APIs), EF Core and an Azure SQL (MSSQL) database.

```
GlazeAudio/
├── src/GlazeAudio.Api/     ASP.NET Core API (endpoints, EF Core model, seed data)
├── infra/                  azure-sql-setup.sh / .ps1 – connect a device to the Azure SQL database
├── postman/                Postman v3 collection + environments (demo & tests)
└── docs/                   openapi.json (exported spec), postman/ (JSON collection for Import)
```

## Resources and API methods

Resources are hierarchical: **Album → Song → Review**. Each resource has 5 methods, for **15** in total.

| # | Method | Route | Success | Errors |
|---|--------|-------|---------|--------|
| 1 | GET    | `/api/albums` | 200 | 400 |
| 2 | GET    | `/api/albums/{albumId}` | 200 | 404 |
| 3 | POST   | `/api/albums` | 201 + `Location` | 400, 422 |
| 4 | PUT    | `/api/albums/{albumId}` | 200 | 400, 404, 422 |
| 5 | DELETE | `/api/albums/{albumId}` | 204 | 404 |
| 6 | GET    | `/api/albums/{albumId}/songs` | 200 | 400, 404 |
| 7 | GET    | `/api/albums/{albumId}/songs/{songId}` | 200 | 404 |
| 8 | POST   | `/api/albums/{albumId}/songs` | 201 + `Location` | 400, 404, 422 |
| 9 | PUT    | `/api/albums/{albumId}/songs/{songId}` | 200 | 400, 404, 422 |
| 10 | DELETE | `/api/albums/{albumId}/songs/{songId}` | 204 | 404 |
| 11 | GET    | `/api/albums/{albumId}/songs/{songId}/reviews` | 200 | 400, 404 |
| 12 | GET    | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | 200 | 404 |
| 13 | POST   | `/api/albums/{albumId}/songs/{songId}/reviews` | 201 + `Location` | 400, 404, 422 |
| 14 | PUT    | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | 200 | 400, 404, 422 |
| 15 | DELETE | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | 204 | 404 |

Two more endpoints:

| Method | Route | What it returns |
|--------|-------|-----------------|
| GET | `/api` | API entry point with links to the main resources |
| GET | `/api/albums/{albumId}/overview` | Album overview built from the album, its songs and their reviews (see below) |

## User accounts

| Method | Route | Success | Errors |
|--------|-------|---------|--------|
| GET    | `/api/users` (paged; filters `search`, `role`) | 200 | 400 |
| GET    | `/api/users/{userId}` | 200 | 404 |
| GET    | `/api/users/{userId}/reviews` (paged; filter `minRating`) | 200 | 400, 404 |
| POST   | `/api/users` (register) | 201 + `Location` | 400, 409, 422 |
| PUT    | `/api/users/{userId}` | 200 | 400, 404, 409, 422 |
| DELETE | `/api/users/{userId}` | 204 | 404 |

- **Fields:** `username` (unique; letters, digits, `_ . -`), `email` (unique), `role` (`User` or `Admin`), `bio`, `createdAt`, `reviewCount`. The password is stored as a salted PBKDF2 hash (ASP.NET Core Identity's `PasswordHasher`) and is never returned.
- **Registration** always creates a `User`. An admin can change the role with `PUT`.
- **409 Conflict** is returned when the username or email is already taken (case-insensitive), or when a user tries to review the same song twice.
- **Reviews are linked to users.** Creating a review takes a `userId`, which will come from the login token once JWT is added, and a review's author can't be changed. Each review includes `userId`, `username` and an `author` link. A user can review a song only once. Deleting a user also deletes their reviews.
- **`GET /api/users/{userId}/reviews`** is the user's profile view: each review together with its song and album.

**Demo accounts** (seeded): `admin` (Admin), `mantas`, `vinyl_owl`, `bassline_ben`, `quietstorm` and `dj_lina` (User). They all have the password `GlazeAudio123!`, ready for when login is added.

**Response codes**

- **404 Not Found:** the resource doesn't exist, *or* the URL hierarchy is wrong (e.g. a song requested under an album it doesn't belong to).
- **400 Bad Request:** the server can't read the request: malformed JSON, a wrong value type (`"trackNumber": "first"`), a missing body, or invalid query parameters (`?page=0`, `?pageSize=500`, `?page=abc`, `?minRating=9`).
- **409 Conflict** the server refuses to handle the request because of a conflict with the current state of the target resource. Happens when trying to concurrently work the same resource.
- **422 Unprocessable Entity:** the JSON is valid but breaks a rule: empty title, year outside 1900–2100, a rating outside 0–5, and so on. The response lists every invalid field under `errors`.
- **201 Created:** returns the created object plus a `Location` header.
- **204 No Content:** returned after a delete; the body is empty.

All errors use the `application/problem+json` format (RFC 9457). Successful responses are `application/json`.

**Reviews** are written by a user and rate four aspects from 0 to 5: `lyricsRating`, `melodyRating`, `moodRating` and `expressivenessRating`. They also include a `comment`. `overallRating` is the average of the four. Songs and albums show `reviewCount` and `averageRating`, so an album's rating is built from the reviews of its songs.

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

- Live spec: `GET /openapi/v1.json`
- Swagger UI: `http://localhost:5080/docs`. The root URL `/` redirects here.
- Exported copy: [`docs/openapi.json`](docs/openapi.json)

## Getting started

### Prerequisites (Linux)

On Windows, see [Setup on Windows](#setup-on-windows) instead.

- [.NET 10 SDK](https://dotnet.microsoft.com/download). On Arch: `sudo pacman -S dotnet-sdk aspnet-runtime dotnet-targeting-pack aspnet-targeting-pack`
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli). On Arch: `yay -S azure-cli`
- Postman 12 (`yay -S postman-bin`), or the Postman CLI (`npm i -g postman-cli`) to run the tests from the terminal

### 1. Connect to the Azure SQL database

The database already exists in Azure: server `glazeaudio-sql.database.windows.net`, database `glazeaudio-db`, resource group `glazeaudio-rg`, region Sweden Central. On each new device, run:

```bash
./infra/azure-sql-setup.sh
```

The script:
1. Asks for the SQL admin login and password. The password input is hidden.
2. Stores the connection string in **.NET user-secrets**, outside the repo, so no password ends up in git.
3. Adds a firewall rule for this device's IP (`dev-<hostname>`). This needs the Azure CLI and `az login`; without them, the script tells you how to add the IP in the portal.
4. Restores packages, builds, and runs `dotnet ef database update`. That applies any new migrations and seeds the database if it's empty. Running it on a database that's already set up does nothing harmful.

<details>
<summary>Manual setup (what the script does)</summary>

```bash
dotnet user-secrets set "ConnectionStrings:GlazeAudioDb" "Server=tcp:glazeaudio-sql.database.windows.net,1433;Initial Catalog=glazeaudio-db;User ID=<user>;Password=<password>;Encrypt=True;Connection Timeout=60;" --project src/GlazeAudio.Api
dotnet build src/GlazeAudio.Api
dotnet ef database update --project src/GlazeAudio.Api
```

Then allow your IP: Azure Portal → SQL server `glazeaudio-sql` → Security → Networking → **Add your client IPv4 address** → Save.
</details>

### 2. Run the API

```bash
dotnet run --project src/GlazeAudio.Api
```

The API listens on **http://localhost:5080**. On startup it also applies any pending migrations and seeds an empty database: 5 albums, 19 songs and 25 reviews.

> The free Azure SQL database pauses when idle. The first request after a pause can take up to a minute while it resumes, and EF Core retries automatically. Make one request before a demo to wake it up.

### 3. Run the Postman demo

The collection uses the **Postman v3 (YAML) format** that Postman 12 works with: one file per request, in this layout:

```
postman/
├── collections/GlazeAudio API/     one .request.yaml per request, grouped into 7 folders
└── environments/                   GlazeAudio - Local / GlazeAudio - Azure
```

**In the Postman app (import)**
1. Click **Import** and select the two files in `docs/postman/`: `GlazeAudio.postman_collection.json` and `Local.postman_environment.json`. If Postman asks how to import them, choose **Postman Collection**, not OpenAPI.
2. Select the **GlazeAudio - Local** environment.
3. On the **GlazeAudio API** collection (7 numbered folders, 0–6), choose **⋯ → Run collection → Run**.

Don't import `docs/openapi.json` as a collection. Postman would generate requests with placeholder IDs and no tests.

**In the Postman app (open folder):** Files icon → **Open folder** → select the repo root → **Local View**. Postman 12 then reads `postman/collections` and `postman/environments` directly.

**From the terminal** (Postman CLI: `npm i -g postman-cli`):

```bash
postman collection run "postman/collections/GlazeAudio API" -e "postman/environments/GlazeAudio - Local.environment.yaml"
```

The collection runs 47 requests with 119 assertions in a couple of seconds:

| Folder | What it shows |
|---|---|
| 0. Users | POST register (201 + Location; a unique username per run), GET list, GET one, PUT |
| 1. Albums | GET list, POST (201 + Location), GET one, PUT |
| 2. Songs | POST (201), GET list, GET one, PUT |
| 3. Reviews | POST (201), GET list, GET one, PUT |
| 4. Paging, filtering, hypermedia, overview | API root, page 1 of 2 → follow the `next` link, filters on albums / songs / reviews / users, follow an album's `songs` link, album overview, a user's reviews, 400 for invalid paging |
| 5. Error cases | 404 missing album/review, 404 song under the wrong album, 400 malformed JSON, 400 wrong type, 422 invalid fields, 409 username taken, 422 invalid user, 422 review by a missing user, 409 duplicate review, 422 rating out of range |
| 6. Delete | DELETE review / song / album / user (204), then 404 for the deleted album and user |

The collection creates its own user, album, song and review and deletes them at the end, so you can run it as many times as you like.

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

### 3. Run the Postman demo

The steps are the same as on Linux (see [Run the Postman demo](#3-run-the-postman-demo)). To run it from the terminal instead, install Node.js first (`winget install OpenJS.NodeJS.LTS`), then:

```powershell
npm i -g postman-cli
postman collection run "postman\collections\GlazeAudio API" -e "postman\environments\GlazeAudio - Local.environment.yaml"
```

### Windows troubleshooting

- **`dotnet` or `dotnet ef` is not recognized:** reopen the terminal after installing. dotnet-ef lives in `%USERPROFILE%\.dotnet\tools`.
- **"Client with IP address … is not allowed":** the firewall rule wasn't added. Run `az login` and re-run the script, or add your IP in the portal (SQL server → Security → Networking).
- **Port 5080 already in use:** stop the other `dotnet` process, or change `applicationUrl` in `src\GlazeAudio.Api\Properties\launchSettings.json` and update `baseUrl` in the Postman environment to match.

## Project structure (API)

```
src/GlazeAudio.Api/
├── Program.cs                 DI, EF Core, OpenAPI, error handling, endpoint mapping
├── Models/                    Album, Song, Review, User (EF Core entities)
├── Data/                      GlazeAudioDbContext, SeedData (demo users + albums)
├── Migrations/                InitialCreate, AddUsers
├── Contracts/                 Request/response records, query parameters, overview + validation rules
├── Endpoints/                 AlbumEndpoints, SongEndpoints, ReviewEndpoints, UserEndpoints
└── Infrastructure/            hypermedia links, paging, 422 validation filter, 400 handler, Swagger UI page
```
