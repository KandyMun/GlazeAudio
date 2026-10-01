# GlazeAudio
A webpage for sharing your thoughts about music albums and their songs.

This repository currently contains the **GlazeAudio REST API**: ASP.NET Core 10 (Minimal APIs), EF Core and an Azure SQL (MSSQL) database.

```
GlazeAudio/
├── src/GlazeAudio.Api/     ASP.NET Core API (endpoints, EF Core model, seed data)
├── infra/                  azure-sql-setup.sh – creates the Azure SQL database
├── postman/                Postman v3 collection + environments (demo & tests)
└── docs/                   openapi.json (exported spec), postman/ (JSON collection for Import)
```

## Resources and API methods

Resources are hierarchical: **Album → Song → Review**. Each resource has 5 methods, for **15** in total.

| # | Method | Route | Success | Errors |
|---|--------|-------|---------|--------|
| 1 | GET    | `/api/albums` | 200 | – |
| 2 | GET    | `/api/albums/{albumId}` | 200 | 404 |
| 3 | POST   | `/api/albums` | 201 + `Location` | 400, 422 |
| 4 | PUT    | `/api/albums/{albumId}` | 200 | 400, 404, 422 |
| 5 | DELETE | `/api/albums/{albumId}` | 204 | 404 |
| 6 | GET    | `/api/albums/{albumId}/songs` | 200 | 404 |
| 7 | GET    | `/api/albums/{albumId}/songs/{songId}` | 200 | 404 |
| 8 | POST   | `/api/albums/{albumId}/songs` | 201 + `Location` | 400, 404, 422 |
| 9 | PUT    | `/api/albums/{albumId}/songs/{songId}` | 200 | 400, 404, 422 |
| 10 | DELETE | `/api/albums/{albumId}/songs/{songId}` | 204 | 404 |
| 11 | GET    | `/api/albums/{albumId}/songs/{songId}/reviews` | 200 | 404 |
| 12 | GET    | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | 200 | 404 |
| 13 | POST   | `/api/albums/{albumId}/songs/{songId}/reviews` | 201 + `Location` | 400, 404, 422 |
| 14 | PUT    | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | 200 | 400, 404, 422 |
| 15 | DELETE | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | 204 | 404 |

**How response codes are chosen**

- **404 Not Found:** the resource doesn't exist, *or* the URL hierarchy is wrong (e.g. a song requested under an album it doesn't belong to).
- **400 Bad Request:** the server can't read the body: malformed JSON, a wrong value type (`"trackNumber": "first"`) or a missing body.
- **422 Unprocessable Entity:** the JSON is valid but breaks a rule: empty title, year outside 1900–2100, a rating outside 0–5, and so on. The response lists every invalid field under `errors`.
- **201 Created:** returns the created object plus a `Location` header.
- **204 No Content:** returned after a delete; the body is empty.

All errors use the `application/problem+json` format (RFC 9457). Successful responses are `application/json`.

**Reviews** rate four aspects from 0 to 5: `lyricsRating`, `melodyRating`, `moodRating` and `expressivenessRating`. They also include a `comment`. `overallRating` is the average of the four. Songs and albums show `reviewCount` and `averageRating`, so an album's rating is built from the reviews of its songs.

## OpenAPI specification

- Live spec: `GET /openapi/v1.json`
- Swagger UI: `http://localhost:5080/docs`. The root URL `/` redirects here.
- Exported copy: [`docs/openapi.json`](docs/openapi.json)

## Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download). On Arch: `sudo pacman -S dotnet-sdk aspnet-runtime`
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
├── collections/GlazeAudio API/     one .request.yaml per request, grouped into 5 folders
└── environments/                   GlazeAudio - Local / GlazeAudio - Azure
```

**In the Postman app (import)**
1. Click **Import** and select the two files in `docs/postman/`: `GlazeAudio.postman_collection.json` and `Local.postman_environment.json`. If Postman asks how to import them, choose **Postman Collection**, not OpenAPI.
2. Select the **GlazeAudio - Local** environment.
3. On the **GlazeAudio API** collection (5 numbered folders), choose **⋯ → Run collection → Run**.

Don't import `docs/openapi.json` as a collection. Postman would generate requests with placeholder IDs and no tests.

**In the Postman app (open folder):** Files icon → **Open folder** → select the repo root → **Local View**. Postman 12 then reads `postman/collections` and `postman/environments` directly.

**From the terminal** (Postman CLI: `npm i -g postman-cli`):

```bash
postman collection run "postman/collections/GlazeAudio API" -e "postman/environments/GlazeAudio - Local.environment.yaml"
```

The collection runs 25 requests with 56 assertions in about a second:

| Folder | What it shows |
|---|---|
| 1. Albums | GET list, POST (201 + Location), GET one, PUT |
| 2. Songs | POST (201), GET list, GET one, PUT |
| 3. Reviews | POST (201), GET list, GET one, PUT |
| 4. Error cases | 404 missing album/review, 404 song under the wrong album, 400 malformed JSON, 400 wrong type, 422 invalid fields, 422 rating out of range |
| 5. Delete | DELETE review / song / album (204), then 404 for the deleted album |

The collection creates its own album, song and review and deletes them at the end, so you can run it as many times as you like.

## Project structure (API)

```
src/GlazeAudio.Api/
├── Program.cs                 DI, EF Core, OpenAPI, error handling, endpoint mapping
├── Models/                    Album, Song, Review (EF Core entities)
├── Data/                      GlazeAudioDbContext, SeedData
├── Contracts/                 Request/response records + validation rules
├── Endpoints/                 AlbumEndpoints, SongEndpoints, ReviewEndpoints
└── Infrastructure/            422 validation filter, 400 handler, Swagger UI page
```
