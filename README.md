# GlazeAudio

**Saityno taikomųjų programų projektavimas (P175B124).** Projekto ataskaita.
Autorius: Mantas Maščinskas, IFF-3/3.

GlazeAudio yra muzikos atsiliepimų platforma. Kiekviena daina vertinama pagal keturis aspektus: žodžius, melodiją, nuotaiką ir išraiškingumą. Projektą sudaro REST API (ASP.NET Core), naudotojo sąsaja (React) ir Azure SQL duomenų bazė. Visa sistema talpinama Microsoft Azure debesyje ir diegiama iš šios Git saugyklos.

<p align="center"><img src="docs/report/screenshots/01-home.jpg" alt="GlazeAudio pagrindinis puslapis" width="720"></p>

Paleidimo, testavimo ir diegimo instrukcijos (anglų kalba) yra faile [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md).

## Turinys

1. [Sprendžiamo uždavinio aprašymas](#1-sprendžiamo-uždavinio-aprašymas)
2. [Sistemos architektūra](#2-sistemos-architektūra)
3. [Naudotojo sąsajos projektas](#3-naudotojo-sąsajos-projektas)
4. [API specifikacija](#4-api-specifikacija)
5. [Projekto išvados](#5-projekto-išvados)

## 1. Sprendžiamo uždavinio aprašymas

### 1.1. Sistemos paskirtis

Projekto tikslas yra suteikti patogią platformą, kurioje galima greitai pamatyti, kaip kiti žmonės vertina muziką ir kodėl.

Platformą sudaro dvi dalys: internetinė aplikacija, kuria naudojasi svečiai, registruoti naudotojai ir administratorius, bei aplikacijų programavimo sąsaja (API). Naudotojas be paskyros gali peržiūrėti visus albumus, jų dainas ir atsiliepimus. Užsiregistravęs naudotojas gali palikti atsiliepimą apie dainą: įvertinti keturis aspektus skalėje nuo 0 iki 5 ir parašyti komentarą. Dainos ir albumo įvertinimai skaičiuojami iš visų atsiliepimų, todėl iš karto matyti, kuo daina ar albumas stiprūs. Administratorius pildo ir tvarko albumų bei dainų katalogą, gali pašalinti netinkamus atsiliepimus ir naudotojų paskyras.

Dalykinės srities objektai yra hierarchiniai: **albumas → daina → atsiliepimas**. Atsiliepimus rašo **naudotojai**.

### 1.2. Funkciniai reikalavimai

**Svečias (neprisijungęs naudotojas) gali:**
- peržiūrėti albumų sąrašą, jį filtruoti (pavadinimas ar atlikėjas, žanras, metai) ir puslapiuoti;
- peržiūrėti albumo apžvalgą: dainas, bendrą ir aspektų vidurkius, geriausiai įvertintą dainą, naujausius atsiliepimus;
- peržiūrėti dainos atsiliepimus ir filtruoti juos pagal įvertinimą;
- peržiūrėti viešus naudotojų profilius ir jų atsiliepimus;
- užsiregistruoti ir prisijungti.

**Registruotas naudotojas papildomai gali:**
- parašyti po vieną atsiliepimą kiekvienai dainai, jį redaguoti ir ištrinti;
- redaguoti savo paskyros duomenis ir pakeisti slaptažodį;
- ištrinti savo paskyrą (kartu su savo atsiliepimais);
- atsijungti. Sesija pratęsiama automatiškai, kol galioja atnaujinimo žetonas.

**Administratorius papildomai gali:**
- pridėti, redaguoti ir ištrinti albumus (su viršelio paveikslėliu) ir dainas;
- ištrinti bet kurį atsiliepimą;
- peržiūrėti naudotojų sąrašą, keisti naudotojų roles ir trinti paskyras.

**Nefunkciniai reikalavimai:**
- autentifikacija ir autorizacija naudojant JWT: naudotojo rolė (`User` arba `Admin`) saugoma žetone;
- prieigos žetonas galioja 15 minučių, atnaujinimo žetonas galioja 7 dienas ir kiekvieną kartą pakeičiamas nauju (rotacija);
- slaptažodžiai saugomi kaip druskos turinčios PBKDF2 maišos;
- API grąžina duomenis JSON formatu, o klaidas `application/problem+json` (RFC 9457) formatu;
- sąsaja prisitaiko prie ekrano dydžio (lūžio taškas ties 768 px).

## 2. Sistemos architektūra

Sistemos sudedamosios dalys:

| Dalis | Technologijos | Talpinimas |
|---|---|---|
| Kliento pusė (Front-End) | React 19, TypeScript, Vite, React Router, lucide-react ikonėlės, Google Fonts | Azure Static Web Apps |
| Serverio pusė (Back-End) | ASP.NET Core 10 Minimal API, EF Core 10, JWT (HS256) | Azure App Service (Linux) |
| Duomenų bazė | Azure SQL Database (MSSQL) | Azure SQL serveris `glazeaudio-sql` |
| Diegimas | GitHub Actions (`deploy-api.yml`, `deploy-web.yml`) | GitHub |

2.1 paveikslėlyje pavaizduota sistemos diegimo diagrama. Naudotojo naršyklė iš Azure Static Web Apps parsisiunčia React aplikaciją (HTML, JavaScript, CSS ir albumų viršelius). Aplikacija kreipiasi į API, talpinamą Azure App Service, per HTTPS. Užklausos ir atsakymai perduodami JSON formatu, o apsaugotoms užklausoms siunčiamas JWT prieigos žetonas (`Authorization: Bearer`). API su duomenų baze bendrauja per EF Core ORM (TDS protokolas, 1433 prievadas). Kai į `main` šaką įkeliami pakeitimai, GitHub Actions automatiškai sukompiliuoja ir įdiegia pasikeitusią dalį: API arba naudotojo sąsają.

<p align="center"><img src="docs/report/deployment-diagram.svg" alt="UML diegimo diagrama" width="900"></p>
<p align="center"><em>2.1 pav. GlazeAudio sistemos diegimo diagrama</em></p>

## 3. Naudotojo sąsajos projektas

Sąsaja suprojektuota laikantis vientisos stilistikos. Pavadinimas „GlazeAudio“ kilo iš keramikos glazūros, todėl spalvos ir albumų viršeliai primena glazūruotą keramiką:

- **Spalvos:** kobalto mėlyna antraštė (`#23408e`), porceliano spalvos turinys (`#f5f7fa`), rašalo spalvos poraštė (`#1a2233`). Kiekvienas vertinimo aspektas turi savo glazūros spalvą: žodžiai kobaltinė, melodija seladoninė (`#4e9a7c`), nuotaika ochros (`#c8961e`), išraiškingumas slyvinė (`#7a4e8c`).
- **Šriftai (Google Fonts):** antraštėms Bricolage Grotesque, tekstui Atkinson Hyperlegible.
- **Ikonėlės:** vektorinės (SVG) lucide-react ikonėlės meniu punktuose ir mygtukuose.
- **Paveikslėliai:** albumų viršeliai yra originalūs, šiam projektui sukurti paveikslėliai (`web/public/covers`). Jų dydis kinta pagal lango plotį.
- **Išdėstymas:** 8 px tinklelis, `header`, `main` ir `footer` elementai, vienas lūžio taškas ties 768 px. Mažesniame ekrane meniu sutraukiamas į „hamburgerį“.
- **Animacijos:** įvertinimų juostos animuotai užsipildo atidarius puslapį, kraunant duomenis rodomas sukimosi indikatorius, kortelės ir mygtukai reaguoja į pelės užvedimą, modaliniai langai ir pranešimai įslenka. Jei naršyklėje įjungtas sumažintas judėjimas (`prefers-reduced-motion`), animacijos išjungiamos.
- **Grįžtamasis ryšys:** formų klaidos rodomos prie atitinkamų laukų, o išsaugojus ar ištrynus duomenis apačioje parodomas pranešimas. Naršyklės `alert` langai nenaudojami.
- **Modaliniai langai:** albumo ir dainos kūrimo bei redagavimo formos, ištrynimo patvirtinimai, įvertinimų paaiškinimas.
- **Formų elementai:** teksto, el. pašto, slaptažodžio, skaičiaus, nuorodos (url) ir paieškos laukai, `textarea`, `select`, slankikliai (`range`), žymimasis langelis (`checkbox`), pasirinkimo mygtukai su paveikslėliais (`radio`) ir pasiūlymų sąrašas (`datalist`).

Toliau pateikti langų wireframe'ai ir juos atitinkančios realizacijos iškarpos.

### W1. Pagrindinis puslapis (albumų sąrašas)

<img src="docs/report/wireframes/01-home-desktop.svg" alt="W1 wireframe" width="720">

Realizacija (svečias) ir modalinis langas „How ratings work“:

<img src="docs/report/screenshots/01-home.jpg" alt="Pagrindinis puslapis" width="440"> <img src="docs/report/screenshots/02-ratings-modal.jpg" alt="Įvertinimų paaiškinimo langas" width="440">

### W2. Pagrindinis puslapis telefone ir „hamburgerio“ meniu

<img src="docs/report/wireframes/02-home-mobile.svg" alt="W2 wireframe" width="560">

<img src="docs/report/screenshots/15-home-mobile.jpg" alt="Telefonas" width="220"> <img src="docs/report/screenshots/16-menu-mobile.jpg" alt="Atidarytas meniu" width="220"> <img src="docs/report/screenshots/17-album-mobile.jpg" alt="Albumas telefone" width="220">

### W3. Albumo puslapis

<img src="docs/report/wireframes/03-album.svg" alt="W3 wireframe" width="720">

Svečio ir administratoriaus vaizdas (administratorius mato albumo ir dainų valdymo mygtukus):

<img src="docs/report/screenshots/03-album.jpg" alt="Albumo puslapis" width="440"> <img src="docs/report/screenshots/11-album-admin.jpg" alt="Albumo puslapis administratoriui" width="440">

### W4. Dainos puslapis ir atsiliepimo forma

<img src="docs/report/wireframes/04-song.svg" alt="W4 wireframe" width="720">

<img src="docs/report/screenshots/06-song-review-form.jpg" alt="Dainos puslapis" width="720">

### W5. Prisijungimas ir registracija

<img src="docs/report/wireframes/05-login-register.svg" alt="W5 wireframe" width="560">

<img src="docs/report/screenshots/04-login.jpg" alt="Prisijungimas" width="440"> <img src="docs/report/screenshots/05-register.jpg" alt="Registracija" width="440">

### W6. Profilio puslapis

<img src="docs/report/wireframes/06-profile.svg" alt="W6 wireframe" width="720">

<img src="docs/report/screenshots/07-profile.jpg" alt="Profilis" width="440"> <img src="docs/report/screenshots/08-delete-account-modal.jpg" alt="Paskyros ištrynimo patvirtinimas" width="440">

### W7. Administratoriaus formos (modaliniai langai)

<img src="docs/report/wireframes/07-admin-modals.svg" alt="W7 wireframe" width="720">

Albumo forma, dainos forma, albumo ištrynimo patvirtinimas ir pranešimas po išsaugojimo:

<img src="docs/report/screenshots/10-album-form-modal.jpg" alt="Albumo forma" width="440"> <img src="docs/report/screenshots/12-song-form-modal.jpg" alt="Dainos forma" width="440">
<img src="docs/report/screenshots/14-delete-album-modal.jpg" alt="Albumo ištrynimas" width="440"> <img src="docs/report/screenshots/13-toast.jpg" alt="Pranešimas" width="440">

Albumo forma telefone:

<img src="docs/report/screenshots/18-album-form-mobile.jpg" alt="Albumo forma telefone" width="220">

## 4. API specifikacija

API aprašytas OpenAPI 3.0 formatu atskirame faile [`docs/api-spec.yaml`](docs/api-spec.yaml). Jame aprašyti visi metodai, parametrai, duomenų schemos, atsako kodai ir užklausų bei atsakymų pavyzdžiai. Pavyzdžiai yra tikros užklausos ir atsakymai, įrašyti iš veikiančio API su demonstraciniais duomenimis. Specifikaciją galima peržiūrėti [Swagger Editor](https://editor.swagger.io) įrankiu arba importuoti į Postman.

**Autentifikacija.** `POST /api/auth/login` (arba `/register`) grąžina prieigos žetoną (JWT, galioja 15 min.) ir atnaujinimo žetoną (galioja 7 d.). Prieigos žetone yra naudotojo id (`sub`), vardas (`name`) ir rolė (`role`). Apsaugotoms užklausoms siunčiama antraštė `Authorization: Bearer <žetonas>`. Kai prieigos žetono galiojimas baigiasi, naujas gaunamas su `POST /api/auth/refresh`.

**Puslapiavimas, filtravimas ir hipermedija.** Sąrašai puslapiuojami parametrais `page` ir `pageSize` (iki 50) ir filtruojami užklausos parametrais. Kiekvienas resursas ir puslapis turi `_links` objektą su nuorodomis į susijusius veiksmus.

**Atsako kodai:**

| Kodas | Reikšmė |
|---|---|
| `200 OK` | Užklausa įvykdyta, grąžinamas resursas arba sąrašo puslapis. |
| `201 Created` | Resursas sukurtas. Antraštė `Location` nurodo jo adresą. |
| `204 No Content` | Veiksmas (ištrynimas, atsijungimas, slaptažodžio keitimas) įvykdytas, atsakymas be turinio. |
| `400 Bad Request` | Neįmanoma perskaityti JSON arba neteisingas užklausos parametras (pvz. `pageSize=500`). |
| `401 Unauthorized` | Nėra prieigos žetono, jis neteisingas arba nebegalioja (prisijungiant: neteisingas vardas ar slaptažodis). |
| `403 Forbidden` | Prisijungta, bet veiksmas neleidžiamas: tik administratoriui arba resursas priklauso kitam naudotojui. |
| `404 Not Found` | Resursas (arba jo albumas ar daina) neegzistuoja. |
| `409 Conflict` | Dublikatas: vardas ar el. paštas užimtas, arba daina jau įvertinta. |
| `422 Unprocessable Entity` | Duomenys nepraėjo validacijos. Laukas `errors` nurodo klaidas kiekvienam laukui. |

**Metodų sąrašas:**

| Metodas | Kelias | Kas gali | Atsako kodai |
|---|---|---|---|
| GET | `/api` | visi | 200 |
| POST | `/api/auth/register` | visi | 201, 400, 409, 422 |
| POST | `/api/auth/login` | visi | 200, 400, 401, 422 |
| POST | `/api/auth/refresh` | visi (su atnaujinimo žetonu) | 200, 400, 401, 422 |
| POST | `/api/auth/logout` | visi (su atnaujinimo žetonu) | 204, 400, 422 |
| GET | `/api/auth/me` | prisijungęs naudotojas | 200, 401 |
| PUT | `/api/auth/password` | prisijungęs naudotojas | 204, 400, 401, 422 |
| GET | `/api/albums` | visi | 200, 400 |
| GET | `/api/albums/{albumId}` | visi | 200, 404 |
| GET | `/api/albums/{albumId}/overview` | visi | 200, 404 |
| POST | `/api/albums` | administratorius | 201, 400, 401, 403, 422 |
| PUT | `/api/albums/{albumId}` | administratorius | 200, 400, 401, 403, 404, 422 |
| DELETE | `/api/albums/{albumId}` | administratorius | 204, 401, 403, 404 |
| GET | `/api/albums/{albumId}/songs` | visi | 200, 400, 404 |
| GET | `/api/albums/{albumId}/songs/{songId}` | visi | 200, 404 |
| POST | `/api/albums/{albumId}/songs` | administratorius | 201, 400, 401, 403, 404, 422 |
| PUT | `/api/albums/{albumId}/songs/{songId}` | administratorius | 200, 400, 401, 403, 404, 422 |
| DELETE | `/api/albums/{albumId}/songs/{songId}` | administratorius | 204, 401, 403, 404 |
| GET | `/api/albums/{albumId}/songs/{songId}/reviews` | visi | 200, 400, 404 |
| GET | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | visi | 200, 404 |
| POST | `/api/albums/{albumId}/songs/{songId}/reviews` | prisijungęs naudotojas | 201, 400, 401, 404, 409, 422 |
| PUT | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | autorius arba administratorius | 200, 400, 401, 403, 404, 422 |
| DELETE | `/api/albums/{albumId}/songs/{songId}/reviews/{reviewId}` | autorius arba administratorius | 204, 401, 403, 404 |
| GET | `/api/users` | administratorius | 200, 400, 401, 403 |
| GET | `/api/users/{userId}` | visi | 200, 404 |
| GET | `/api/users/{userId}/reviews` | visi | 200, 400, 404 |
| POST | `/api/users` | administratorius | 201, 400, 401, 403, 409, 422 |
| PUT | `/api/users/{userId}` | pats naudotojas arba administratorius | 200, 400, 401, 403, 404, 409, 422 |
| DELETE | `/api/users/{userId}` | pats naudotojas arba administratorius | 204, 401, 403, 404 |

**Kiekvieno metodo pavyzdžiai** (užklausa ir atsakymas, įskaitant klaidų atvejus). Ilgi sąrašai sutrumpinti iki dviejų elementų, žetonai sutrumpinti. Visi pavyzdžiai su visais laukais yra [`docs/api-spec.yaml`](docs/api-spec.yaml).

### 4.1. Šaknis

#### `GET /api`

API entry point with links to the main resources. Kas gali: visi. Atsako kodai: `200`.

<details><summary>API root → <code>200</code></summary>

```http
GET /api
```

```http
HTTP/1.1 200 OK

{
  "name": "GlazeAudio API",
  "version": "v1",
  "_links": {
    "self": {
      "href": "http://localhost:5080/api",
      "method": "GET"
    },
    "albums": {
      "href": "http://localhost:5080/api/albums",
      "method": "GET"
    },
    "createAlbum": {
      "href": "http://localhost:5080/api/albums",
      "method": "POST"
    },
    "register": {
      "href": "http://localhost:5080/api/auth/register",
      "method": "POST"
    },
    "login": {
      "href": "http://localhost:5080/api/auth/login",
      "method": "POST"
    },
    "refresh": {
      "href": "http://localhost:5080/api/auth/refresh",
      "method": "POST"
    },
    "me": {
      "href": "http://localhost:5080/api/auth/me",
      "method": "GET"
    },
    "docs": {
      "href": "http://localhost:5080/openapi/v1.json",
      "method": "GET"
    }
  }
}
```

</details>


### 4.2. Autentifikacija

#### `POST /api/auth/register`

Create an account and log in. Kas gali: visi. Atsako kodai: `201`, `400`, `409`, `422`.

<details><summary>Register → <code>201</code></summary>

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "new_listener",
  "email": "new_listener@example.com",
  "password": "Listening123!",
  "bio": "Mostly jazz."
}
```

```http
HTTP/1.1 201 Created
Location: /api/users/7

{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5…",
  "accessTokenExpiresAt": "2026-11-12T21:32:16.1207792Z",
  "refreshToken": "Kdc25aUgiuhew4N6EuCfD1ra…",
  "refreshTokenExpiresAt": "2026-11-19T21:17:16.1210174Z",
  "user": {
    "id": 7,
    "username": "new_listener",
    "email": "new_listener@example.com",
    "role": "User",
    "bio": "Mostly jazz.",
    "createdAt": "2026-11-12T21:17:16.0567799Z",
    "reviewCount": 0
  },
  "tokenType": "Bearer"
}
```

</details>

<details><summary>Username taken → <code>409</code></summary>

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "mantas",
  "email": "other@example.com",
  "password": "Listening123!"
}
```

```http
HTTP/1.1 409 Conflict

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Username already taken.",
  "status": 409,
  "detail": "The username 'mantas' is already in use."
}
```

</details>

<details><summary>Validation error → <code>422</code></summary>

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "x",
  "email": "not-an-email",
  "password": "123"
}
```

```http
HTTP/1.1 422 Unprocessable Entity

{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "username": [
      "The field Username must be a string with a minimum length of 3 and a maximum length of 50."
    ],
    "email": [
      "The Email field is not a valid e-mail address."
    ],
    "password": [
      "The field Password must be a string with a minimum length of 8 and a maximum length of 100."
    ]
  }
}
```

</details>


#### `POST /api/auth/login`

Log in with username (or email) and password. Kas gali: visi. Atsako kodai: `200`, `400`, `401`, `422`.

<details><summary>Log in → <code>200</code></summary>

```http
POST /api/auth/login
Content-Type: application/json

{
  "login": "new_listener",
  "password": "Listening123!"
}
```

```http
HTTP/1.1 200 OK

{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5…",
  "accessTokenExpiresAt": "2026-11-12T21:32:16.2570528Z",
  "refreshToken": "7HQXjLg5IpYjzxswC_4u2ozv…",
  "refreshTokenExpiresAt": "2026-11-19T21:17:16.2573293Z",
  "user": {
    "id": 7,
    "username": "new_listener",
    "email": "new_listener@example.com",
    "role": "User",
    "bio": "Mostly jazz.",
    "createdAt": "2026-11-12T21:17:16.056779Z",
    "reviewCount": 0
  },
  "tokenType": "Bearer"
}
```

</details>

<details><summary>Wrong password → <code>401</code></summary>

```http
POST /api/auth/login
Content-Type: application/json

{
  "login": "new_listener",
  "password": "wrong-password"
}
```

```http
HTTP/1.1 401 Unauthorized

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Invalid login or password.",
  "status": 401,
  "detail": "Check your username/email and password."
}
```

</details>


#### `POST /api/auth/refresh`

Get a new access token. Kas gali: visi (su atnaujinimo žetonu). Atsako kodai: `200`, `400`, `401`, `422`.

<details><summary>Refresh → <code>200</code></summary>

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "7HQXjLg5IpYjzxswC_4u2ozv…"
}
```

```http
HTTP/1.1 200 OK

{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5…",
  "accessTokenExpiresAt": "2026-11-12T21:32:16.3814925Z",
  "refreshToken": "eZMrM0Dbf9JC-TsqOyDn4jOi…",
  "refreshTokenExpiresAt": "2026-11-19T21:17:16.3817441Z",
  "user": {
    "id": 7,
    "username": "new_listener",
    "email": "new_listener@example.com",
    "role": "User",
    "bio": "Mostly jazz.",
    "createdAt": "2026-11-12T21:17:16.056779Z",
    "reviewCount": 0
  },
  "tokenType": "Bearer"
}
```

</details>

<details><summary>Old refresh token reused → <code>401</code></summary>

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "7HQXjLg5IpYjzxswC_4u2ozv…"
}
```

```http
HTTP/1.1 401 Unauthorized

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Refresh token was already used.",
  "status": 401,
  "detail": "For safety all sessions were logged out. Log in again."
}
```

</details>


#### `POST /api/auth/logout`

Log out (revoke a refresh token). Kas gali: visi (su atnaujinimo žetonu). Atsako kodai: `204`, `400`, `422`.

<details><summary>Log out → <code>204</code></summary>

```http
POST /api/auth/logout
Content-Type: application/json

{
  "refreshToken": "_qQpEsZTLXwAYPv6lgWekg5E…"
}
```

```http
HTTP/1.1 204 No Content
```

</details>


#### `GET /api/auth/me`

The logged-in user. Kas gali: prisijungęs naudotojas. Atsako kodai: `200`, `401`.

<details><summary>Current user → <code>200</code></summary>

```http
GET /api/auth/me
Authorization: Bearer <naudotojo žetonas>
```

```http
HTTP/1.1 200 OK

{
  "id": 7,
  "username": "new_listener",
  "email": "new_listener@example.com",
  "role": "User",
  "bio": "Mostly jazz.",
  "createdAt": "2026-11-12T21:17:16.056779Z",
  "reviewCount": 0,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/users/7",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/users/7",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/users/7",
      "method": "DELETE"
    },
    "reviews": {
      "href": "http://localhost:5080/api/users/7/reviews",
      "method": "GET"
    },
    "users": {
      "href": "http://localhost:5080/api/users",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>No token → <code>401</code></summary>

```http
GET /api/auth/me
```

```http
HTTP/1.1 401 Unauthorized

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Authentication required.",
  "status": 401,
  "detail": "Log in and send the access token as 'Authorization: Bearer <token>'."
}
```

</details>


#### `PUT /api/auth/password`

Change your password. Kas gali: prisijungęs naudotojas. Atsako kodai: `204`, `400`, `401`, `422`.

<details><summary>Change password → <code>204</code></summary>

```http
PUT /api/auth/password
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "currentPassword": "Listening123!",
  "newPassword": "Listening456!"
}
```

```http
HTTP/1.1 204 No Content
```

</details>

<details><summary>Wrong current password → <code>422</code></summary>

```http
PUT /api/auth/password
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "currentPassword": "nope-nope",
  "newPassword": "Listening789!"
}
```

```http
HTTP/1.1 422 Unprocessable Entity

{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "currentPassword": [
      "The current password is wrong."
    ]
  }
}
```

</details>


### 4.3. Albumai

#### `GET /api/albums`

List albums (paged, filterable). Kas gali: visi. Atsako kodai: `200`, `400`.

<details><summary>Paged, filtered list → <code>200</code></summary>

```http
GET /api/albums?page=1&pageSize=2&fromYear=2010
```

```http
HTTP/1.1 200 OK

{
  "items": [
    {
      "id": 2,
      "title": "Random Access Memories",
      "artist": "Daft Punk",
      "releaseYear": 2013,
      "genre": "Electronic / Disco",
      "coverUrl": "/covers/ink-speckle.jpg",
      "songCount": 4,
      "reviewCount": 5,
      "averageRating": 3.95
    },
    {
      "id": 3,
      "title": "To Pimp a Butterfly",
      "artist": "Kendrick Lamar",
      "releaseYear": 2015,
      "genre": "Hip-Hop / Jazz Rap",
      "coverUrl": "/covers/ochre-crackle.jpg",
      "songCount": 4,
      "reviewCount": 5,
      "averageRating": 4.45
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 3,
  "totalPages": 2,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums?fromYear=2010&page=1&pageSize=2",
      "method": "GET"
    },
    "first": {
      "href": "http://localhost:5080/api/albums?fromYear=2010&page=1&pageSize=2",
      "method": "GET"
    },
    "last": {
      "href": "http://localhost:5080/api/albums?fromYear=2010&page=2&pageSize=2",
      "method": "GET"
    },
    "next": {
      "href": "http://localhost:5080/api/albums?fromYear=2010&page=2&pageSize=2",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Invalid paging → <code>400</code></summary>

```http
GET /api/albums?pageSize=500
```

```http
HTTP/1.1 400 Bad Request

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid query parameters.",
  "status": 400,
  "errors": {
    "pageSize": [
      "pageSize must be between 1 and 50."
    ]
  }
}
```

</details>


#### `GET /api/albums/{albumId}`

Get one album. Kas gali: visi. Atsako kodai: `200`, `404`.

<details><summary>One album → <code>200</code></summary>

```http
GET /api/albums/1
```

```http
HTTP/1.1 200 OK

{
  "id": 1,
  "title": "OK Computer",
  "artist": "Radiohead",
  "releaseYear": 1997,
  "genre": "Alternative Rock",
  "coverUrl": "/covers/cobalt-drip.jpg",
  "songCount": 4,
  "reviewCount": 8,
  "averageRating": 4.3125,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/1",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/1",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/1",
      "method": "DELETE"
    },
    "songs": {
      "href": "http://localhost:5080/api/albums/1/songs",
      "method": "GET"
    },
    "createSong": {
      "href": "http://localhost:5080/api/albums/1/songs",
      "method": "POST"
    },
    "overview": {
      "href": "http://localhost:5080/api/albums/1/overview",
      "method": "GET"
    },
    "albums": {
      "href": "http://localhost:5080/api/albums",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Not found → <code>404</code></summary>

```http
GET /api/albums/999
```

```http
HTTP/1.1 404 Not Found

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Album not found.",
  "status": 404,
  "detail": "Album with id 999 does not exist."
}
```

</details>


#### `GET /api/albums/{albumId}/overview`

Album overview (album + songs + reviews). Kas gali: visi. Atsako kodai: `200`, `404`.

<details><summary>Album overview → <code>200</code></summary>

```http
GET /api/albums/5/overview
```

```http
HTTP/1.1 200 OK

{
  "album": {
    "id": 5,
    "title": "Currents",
    "artist": "Tame Impala",
    "releaseYear": 2015,
    "genre": "Psychedelic Pop",
    "coverUrl": "/covers/plum-flow.jpg"
  },
  "stats": {
    "songCount": 3,
    "reviewCount": 3,
    "totalDurationSeconds": 1001,
    "averageRating": 4,
    "aspectAverages": {
      "lyrics": 3.33,
      "melody": 4.67,
      "mood": 4.33,
      "expressiveness": 3.67
    }
  },
  "songs": [
    {
      "id": 17,
      "trackNumber": 1,
      "title": "Let It Happen",
      "durationSeconds": 467,
      "reviewCount": 1,
      "averageRating": 4.5
    },
    {
      "id": 18,
      "trackNumber": 2,
      "title": "Eventually",
      "durationSeconds": 318,
      "reviewCount": 0,
      "averageRating": null
    }
  ],
  "topRatedSong": {
    "id": 17,
    "trackNumber": 1,
    "title": "Let It Happen",
    "durationSeconds": 467,
    "reviewCount": 1,
    "averageRating": 4.5
  },
  "latestReviews": [
    {
      "id": 25,
      "songId": 19,
      "songTitle": "The Less I Know the Better",
      "userId": 3,
      "username": "vinyl_owl",
      "overallRating": 3.5,
      "comment": "Catchy, but overplayed. Other tracks on the album deserve more attention.",
      "createdAt": "2026-11-12T21:17:14.610153Z"
    },
    {
      "id": 24,
      "songId": 19,
      "songTitle": "The Less I Know the Better",
      "userId": 2,
      "username": "mantas",
      "overallRating": 4,
      "comment": "That bassline carries everything. The story is silly, which fits the song.",
      "createdAt": "2026-11-12T21:17:14.610153Z"
    }
  ],
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/5/overview",
      "method": "GET"
    },
    "album": {
      "href": "http://localhost:5080/api/albums/5",
      "method": "GET"
    },
    "songs": {
      "href": "http://localhost:5080/api/albums/5/songs",
      "method": "GET"
    }
  }
}
```

</details>


#### `POST /api/albums`

Create an album (admin). Kas gali: administratorius. Atsako kodai: `201`, `400`, `401`, `403`, `422`.

<details><summary>Create (admin) → <code>201</code></summary>

```http
POST /api/albums
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "title": "In Rainbows",
  "artist": "Radiohead",
  "releaseYear": 2007,
  "genre": "Alternative Rock",
  "coverUrl": "/covers/teal-bloom.jpg"
}
```

```http
HTTP/1.1 201 Created
Location: /api/albums/6

{
  "id": 6,
  "title": "In Rainbows",
  "artist": "Radiohead",
  "releaseYear": 2007,
  "genre": "Alternative Rock",
  "coverUrl": "/covers/teal-bloom.jpg",
  "songCount": 0,
  "reviewCount": 0,
  "averageRating": null,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "DELETE"
    },
    "songs": {
      "href": "http://localhost:5080/api/albums/6/songs",
      "method": "GET"
    },
    "createSong": {
      "href": "http://localhost:5080/api/albums/6/songs",
      "method": "POST"
    },
    "overview": {
      "href": "http://localhost:5080/api/albums/6/overview",
      "method": "GET"
    },
    "albums": {
      "href": "http://localhost:5080/api/albums",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Not logged in → <code>401</code></summary>

```http
POST /api/albums
Content-Type: application/json

{
  "title": "In Rainbows",
  "artist": "Radiohead",
  "releaseYear": 2007,
  "genre": "Alternative Rock",
  "coverUrl": "/covers/teal-bloom.jpg"
}
```

```http
HTTP/1.1 401 Unauthorized

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Authentication required.",
  "status": 401,
  "detail": "Log in and send the access token as 'Authorization: Bearer <token>'."
}
```

</details>

<details><summary>Not an admin → <code>403</code></summary>

```http
POST /api/albums
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "title": "In Rainbows",
  "artist": "Radiohead",
  "releaseYear": 2007,
  "genre": "Alternative Rock",
  "coverUrl": "/covers/teal-bloom.jpg"
}
```

```http
HTTP/1.1 403 Forbidden

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden.",
  "status": 403,
  "detail": "Your role does not allow this action."
}
```

</details>

<details><summary>Validation error → <code>422</code></summary>

```http
POST /api/albums
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "title": "",
  "artist": "Radiohead",
  "releaseYear": 1800,
  "genre": "Rock",
  "coverUrl": "not a link"
}
```

```http
HTTP/1.1 422 Unprocessable Entity

{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "title": [
      "The Title field is required."
    ],
    "releaseYear": [
      "The field ReleaseYear must be between 1900 and 2100."
    ],
    "coverUrl": [
      "Cover URL must be an http(s) address or a path starting with '/'."
    ]
  }
}
```

</details>


#### `PUT /api/albums/{albumId}`

Update an album (admin). Kas gali: administratorius. Atsako kodai: `200`, `400`, `401`, `403`, `404`, `422`.

<details><summary>Replace (admin) → <code>200</code></summary>

```http
PUT /api/albums/6
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "title": "In Rainbows",
  "artist": "Radiohead",
  "releaseYear": 2007,
  "genre": "Art Rock",
  "coverUrl": "/covers/teal-bloom.jpg"
}
```

```http
HTTP/1.1 200 OK

{
  "id": 6,
  "title": "In Rainbows",
  "artist": "Radiohead",
  "releaseYear": 2007,
  "genre": "Art Rock",
  "coverUrl": "/covers/teal-bloom.jpg",
  "songCount": 0,
  "reviewCount": 0,
  "averageRating": null,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "DELETE"
    },
    "songs": {
      "href": "http://localhost:5080/api/albums/6/songs",
      "method": "GET"
    },
    "createSong": {
      "href": "http://localhost:5080/api/albums/6/songs",
      "method": "POST"
    },
    "overview": {
      "href": "http://localhost:5080/api/albums/6/overview",
      "method": "GET"
    },
    "albums": {
      "href": "http://localhost:5080/api/albums",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Not found → <code>404</code></summary>

```http
PUT /api/albums/999
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "title": "In Rainbows",
  "artist": "Radiohead",
  "releaseYear": 2007,
  "genre": "Alternative Rock",
  "coverUrl": "/covers/teal-bloom.jpg"
}
```

```http
HTTP/1.1 404 Not Found

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Album not found.",
  "status": 404,
  "detail": "Album with id 999 does not exist."
}
```

</details>


#### `DELETE /api/albums/{albumId}`

Delete an album (admin). Kas gali: administratorius. Atsako kodai: `204`, `401`, `403`, `404`.

<details><summary>Delete (admin) → <code>204</code></summary>

```http
DELETE /api/albums/6
Authorization: Bearer <administratoriaus žetonas>
```

```http
HTTP/1.1 204 No Content
```

</details>

<details><summary>Not an admin → <code>403</code></summary>

```http
DELETE /api/albums/1
Authorization: Bearer <naudotojo žetonas>
```

```http
HTTP/1.1 403 Forbidden

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden.",
  "status": 403,
  "detail": "Your role does not allow this action."
}
```

</details>


### 4.4. Dainos

#### `GET /api/albums/{albumId}/songs`

List songs of an album (paged, filterable). Kas gali: visi. Atsako kodai: `200`, `400`, `404`.

<details><summary>Songs of an album → <code>200</code></summary>

```http
GET /api/albums/1/songs?pageSize=2&search=a
```

```http
HTTP/1.1 200 OK

{
  "items": [
    {
      "id": 1,
      "albumId": 1,
      "title": "Airbag",
      "trackNumber": 1,
      "durationSeconds": 284,
      "reviewCount": 2,
      "averageRating": 3.875
    },
    {
      "id": 2,
      "albumId": 1,
      "title": "Paranoid Android",
      "trackNumber": 2,
      "durationSeconds": 383,
      "reviewCount": 3,
      "averageRating": 4.5
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 3,
  "totalPages": 2,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/1/songs?search=a&page=1&pageSize=2",
      "method": "GET"
    },
    "first": {
      "href": "http://localhost:5080/api/albums/1/songs?search=a&page=1&pageSize=2",
      "method": "GET"
    },
    "last": {
      "href": "http://localhost:5080/api/albums/1/songs?search=a&page=2&pageSize=2",
      "method": "GET"
    },
    "next": {
      "href": "http://localhost:5080/api/albums/1/songs?search=a&page=2&pageSize=2",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Album not found → <code>404</code></summary>

```http
GET /api/albums/999/songs
```

```http
HTTP/1.1 404 Not Found

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Album not found.",
  "status": 404,
  "detail": "Album with id 999 does not exist."
}
```

</details>


#### `GET /api/albums/{albumId}/songs/{songId}`

Get one song. Kas gali: visi. Atsako kodai: `200`, `404`.

<details><summary>One song → <code>200</code></summary>

```http
GET /api/albums/1/songs/2
```

```http
HTTP/1.1 200 OK

{
  "id": 2,
  "albumId": 1,
  "title": "Paranoid Android",
  "trackNumber": 2,
  "durationSeconds": 383,
  "reviewCount": 3,
  "averageRating": 4.5,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/1/songs/2",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/1/songs/2",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/1/songs/2",
      "method": "DELETE"
    },
    "reviews": {
      "href": "http://localhost:5080/api/albums/1/songs/2/reviews",
      "method": "GET"
    },
    "createReview": {
      "href": "http://localhost:5080/api/albums/1/songs/2/reviews",
      "method": "POST"
    },
    "album": {
      "href": "http://localhost:5080/api/albums/1",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Song not on this album → <code>404</code></summary>

```http
GET /api/albums/2/songs/2
```

```http
HTTP/1.1 404 Not Found

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Song not found.",
  "status": 404,
  "detail": "Song with id 2 does not exist."
}
```

</details>


#### `POST /api/albums/{albumId}/songs`

Add a song to an album (admin). Kas gali: administratorius. Atsako kodai: `201`, `400`, `401`, `403`, `404`, `422`.

<details><summary>Create (admin) → <code>201</code></summary>

```http
POST /api/albums/6/songs
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "title": "Nude",
  "trackNumber": 3,
  "durationSeconds": 255
}
```

```http
HTTP/1.1 201 Created
Location: /api/albums/6/songs/20

{
  "id": 20,
  "albumId": 6,
  "title": "Nude",
  "trackNumber": 3,
  "durationSeconds": 255,
  "reviewCount": 0,
  "averageRating": null,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "DELETE"
    },
    "reviews": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews",
      "method": "GET"
    },
    "createReview": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews",
      "method": "POST"
    },
    "album": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Validation error → <code>422</code></summary>

```http
POST /api/albums/6/songs
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "title": "Nude",
  "trackNumber": 0,
  "durationSeconds": 0
}
```

```http
HTTP/1.1 422 Unprocessable Entity

{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "trackNumber": [
      "The field TrackNumber must be between 1 and 99."
    ],
    "durationSeconds": [
      "The field DurationSeconds must be between 1 and 7200."
    ]
  }
}
```

</details>


#### `PUT /api/albums/{albumId}/songs/{songId}`

Update a song (admin). Kas gali: administratorius. Atsako kodai: `200`, `400`, `401`, `403`, `404`, `422`.

<details><summary>Replace (admin) → <code>200</code></summary>

```http
PUT /api/albums/6/songs/20
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "title": "Nude",
  "trackNumber": 3,
  "durationSeconds": 256
}
```

```http
HTTP/1.1 200 OK

{
  "id": 20,
  "albumId": 6,
  "title": "Nude",
  "trackNumber": 3,
  "durationSeconds": 256,
  "reviewCount": 0,
  "averageRating": null,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "DELETE"
    },
    "reviews": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews",
      "method": "GET"
    },
    "createReview": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews",
      "method": "POST"
    },
    "album": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Not an admin → <code>403</code></summary>

```http
PUT /api/albums/6/songs/20
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "title": "Nude",
  "trackNumber": 3,
  "durationSeconds": 255
}
```

```http
HTTP/1.1 403 Forbidden

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden.",
  "status": 403,
  "detail": "Your role does not allow this action."
}
```

</details>


#### `DELETE /api/albums/{albumId}/songs/{songId}`

Delete a song (admin). Kas gali: administratorius. Atsako kodai: `204`, `401`, `403`, `404`.

<details><summary>Delete (admin) → <code>204</code></summary>

```http
DELETE /api/albums/6/songs/20
Authorization: Bearer <administratoriaus žetonas>
```

```http
HTTP/1.1 204 No Content
```

</details>

<details><summary>Not logged in → <code>401</code></summary>

```http
DELETE /api/albums/6/songs/20
```

```http
HTTP/1.1 401 Unauthorized

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Authentication required.",
  "status": 401,
  "detail": "Log in and send the access token as 'Authorization: Bearer <token>'."
}
```

</details>


### 4.5. Atsiliepimai

#### `GET /api/albums/{albumId}/songs/{songId}/reviews`

List reviews of a song (paged, filterable). Kas gali: visi. Atsako kodai: `200`, `400`, `404`.

<details><summary>Reviews of a song → <code>200</code></summary>

```http
GET /api/albums/1/songs/2/reviews?pageSize=2&minRating=4
```

```http
HTTP/1.1 200 OK

{
  "items": [
    {
      "id": 5,
      "songId": 2,
      "userId": 5,
      "username": "quietstorm",
      "lyricsRating": 4,
      "melodyRating": 4,
      "moodRating": 3,
      "expressivenessRating": 5,
      "overallRating": 4,
      "comment": "Brilliant but exhausting. Not something I put on in the background.",
      "createdAt": "2026-11-12T21:17:14.610088Z",
      "updatedAt": null
    },
    {
      "id": 4,
      "songId": 2,
      "userId": 4,
      "username": "bassline_ben",
      "lyricsRating": 4,
      "melodyRating": 5,
      "moodRating": 4,
      "expressivenessRating": 5,
      "overallRating": 4.5,
      "comment": "Too long for casual listening, but you notice something new every time.",
      "createdAt": "2026-11-12T21:17:14.610088Z",
      "updatedAt": null
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 3,
  "totalPages": 2,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/1/songs/2/reviews?minRating=4&page=1&pageSize=2",
      "method": "GET"
    },
    "first": {
      "href": "http://localhost:5080/api/albums/1/songs/2/reviews?minRating=4&page=1&pageSize=2",
      "method": "GET"
    },
    "last": {
      "href": "http://localhost:5080/api/albums/1/songs/2/reviews?minRating=4&page=2&pageSize=2",
      "method": "GET"
    },
    "next": {
      "href": "http://localhost:5080/api/albums/1/songs/2/reviews?minRating=4&page=2&pageSize=2",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Invalid filter → <code>400</code></summary>

```http
GET /api/albums/1/songs/2/reviews?minRating=9
```

```http
HTTP/1.1 400 Bad Request

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid query parameters.",
  "status": 400,
  "errors": {
    "rating": [
      "minRating and maxRating must be between 0 and 5."
    ]
  }
}
```

</details>


#### `GET /api/albums/{albumId}/songs/{songId}/reviews/{reviewId}`

Get one review. Kas gali: visi. Atsako kodai: `200`, `404`.

<details><summary>One review → <code>200</code></summary>

```http
GET /api/albums/6/songs/20/reviews/26
```

```http
HTTP/1.1 200 OK

{
  "id": 26,
  "songId": 20,
  "userId": 7,
  "username": "new_listener",
  "lyricsRating": 4,
  "melodyRating": 5,
  "moodRating": 5,
  "expressivenessRating": 4,
  "overallRating": 4.5,
  "comment": "Warm, slow and beautiful. The guitars sound like water.",
  "createdAt": "2026-11-12T21:17:17.379987Z",
  "updatedAt": null,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "DELETE"
    },
    "song": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "GET"
    },
    "album": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "GET"
    },
    "author": {
      "href": "http://localhost:5080/api/users/7",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Not found → <code>404</code></summary>

```http
GET /api/albums/6/songs/20/reviews/999
```

```http
HTTP/1.1 404 Not Found

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Review not found.",
  "status": 404,
  "detail": "Review with id 999 does not exist."
}
```

</details>


#### `POST /api/albums/{albumId}/songs/{songId}/reviews`

Review a song. Kas gali: prisijungęs naudotojas. Atsako kodai: `201`, `400`, `401`, `404`, `409`, `422`.

<details><summary>Create (logged in) → <code>201</code></summary>

```http
POST /api/albums/6/songs/20/reviews
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "lyricsRating": 4,
  "melodyRating": 5,
  "moodRating": 5,
  "expressivenessRating": 4,
  "comment": "Warm, slow and beautiful. The guitars sound like water."
}
```

```http
HTTP/1.1 201 Created
Location: /api/albums/6/songs/20/reviews/26

{
  "id": 26,
  "songId": 20,
  "userId": 7,
  "username": "new_listener",
  "lyricsRating": 4,
  "melodyRating": 5,
  "moodRating": 5,
  "expressivenessRating": 4,
  "overallRating": 4.5,
  "comment": "Warm, slow and beautiful. The guitars sound like water.",
  "createdAt": "2026-11-12T21:17:17.3799871Z",
  "updatedAt": null,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "DELETE"
    },
    "song": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "GET"
    },
    "album": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "GET"
    },
    "author": {
      "href": "http://localhost:5080/api/users/7",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Already reviewed → <code>409</code></summary>

```http
POST /api/albums/6/songs/20/reviews
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "lyricsRating": 4,
  "melodyRating": 5,
  "moodRating": 5,
  "expressivenessRating": 4,
  "comment": "Warm, slow and beautiful. The guitars sound like water."
}
```

```http
HTTP/1.1 409 Conflict

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Review already exists.",
  "status": 409,
  "detail": "You have already reviewed this song. Edit your existing review instead."
}
```

</details>

<details><summary>Validation error → <code>422</code></summary>

```http
POST /api/albums/6/songs/20/reviews
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "lyricsRating": 4,
  "melodyRating": 7,
  "moodRating": 5,
  "expressivenessRating": 4,
  "comment": ""
}
```

```http
HTTP/1.1 422 Unprocessable Entity

{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "melodyRating": [
      "The field MelodyRating must be between 0 and 5."
    ],
    "comment": [
      "The Comment field is required."
    ]
  }
}
```

</details>


#### `PUT /api/albums/{albumId}/songs/{songId}/reviews/{reviewId}`

Edit a review. Kas gali: atsiliepimo autorius arba administratorius. Atsako kodai: `200`, `400`, `401`, `403`, `404`, `422`.

<details><summary>Edit own review → <code>200</code></summary>

```http
PUT /api/albums/6/songs/20/reviews/26
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "lyricsRating": 4,
  "melodyRating": 5,
  "moodRating": 4,
  "expressivenessRating": 4,
  "comment": "Warm, slow and beautiful. The guitars sound like water."
}
```

```http
HTTP/1.1 200 OK

{
  "id": 26,
  "songId": 20,
  "userId": 7,
  "username": "new_listener",
  "lyricsRating": 4,
  "melodyRating": 5,
  "moodRating": 4,
  "expressivenessRating": 4,
  "overallRating": 4.25,
  "comment": "Warm, slow and beautiful. The guitars sound like water.",
  "createdAt": "2026-11-12T21:17:17.379987Z",
  "updatedAt": "2026-11-12T21:17:17.4427322Z",
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/albums/6/songs/20/reviews/26",
      "method": "DELETE"
    },
    "song": {
      "href": "http://localhost:5080/api/albums/6/songs/20",
      "method": "GET"
    },
    "album": {
      "href": "http://localhost:5080/api/albums/6",
      "method": "GET"
    },
    "author": {
      "href": "http://localhost:5080/api/users/7",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Someone else's review → <code>403</code></summary>

```http
PUT /api/albums/6/songs/20/reviews/26
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "lyricsRating": 4,
  "melodyRating": 5,
  "moodRating": 5,
  "expressivenessRating": 4,
  "comment": "Warm, slow and beautiful. The guitars sound like water."
}
```

```http
HTTP/1.1 403 Forbidden

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden.",
  "status": 403,
  "detail": "You can only edit your own reviews."
}
```

</details>


#### `DELETE /api/albums/{albumId}/songs/{songId}/reviews/{reviewId}`

Delete a review. Kas gali: atsiliepimo autorius arba administratorius. Atsako kodai: `204`, `401`, `403`, `404`.

<details><summary>Delete own review → <code>204</code></summary>

```http
DELETE /api/albums/6/songs/20/reviews/26
Authorization: Bearer <naudotojo žetonas>
```

```http
HTTP/1.1 204 No Content
```

</details>

<details><summary>Not found → <code>404</code></summary>

```http
DELETE /api/albums/6/songs/20/reviews/26
Authorization: Bearer <naudotojo žetonas>
```

```http
HTTP/1.1 404 Not Found

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Review not found.",
  "status": 404,
  "detail": "Review with id 26 does not exist."
}
```

</details>


### 4.6. Naudotojai

#### `GET /api/users`

List users (paged, filterable). Kas gali: administratorius. Atsako kodai: `200`, `400`, `401`, `403`.

<details><summary>List users (admin) → <code>200</code></summary>

```http
GET /api/users?pageSize=2&role=User
Authorization: Bearer <administratoriaus žetonas>
```

```http
HTTP/1.1 200 OK

{
  "items": [
    {
      "id": 2,
      "username": "mantas",
      "email": "mantas@glazeaudio.test",
      "role": "User",
      "bio": "Listens to everything from Radiohead to Kendrick.",
      "createdAt": "2026-11-12T21:17:14.04918Z",
      "reviewCount": 6
    },
    {
      "id": 3,
      "username": "vinyl_owl",
      "email": "vinyl_owl@glazeaudio.test",
      "role": "User",
      "bio": "Collects records, mostly 70s and 90s.",
      "createdAt": "2026-11-12T21:17:14.112777Z",
      "reviewCount": 6
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 6,
  "totalPages": 3,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/users?role=User&page=1&pageSize=2",
      "method": "GET"
    },
    "first": {
      "href": "http://localhost:5080/api/users?role=User&page=1&pageSize=2",
      "method": "GET"
    },
    "last": {
      "href": "http://localhost:5080/api/users?role=User&page=3&pageSize=2",
      "method": "GET"
    },
    "next": {
      "href": "http://localhost:5080/api/users?role=User&page=2&pageSize=2",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Not an admin → <code>403</code></summary>

```http
GET /api/users
Authorization: Bearer <naudotojo žetonas>
```

```http
HTTP/1.1 403 Forbidden

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden.",
  "status": 403,
  "detail": "Your role does not allow this action."
}
```

</details>


#### `GET /api/users/{userId}`

Get one user. Kas gali: visi. Atsako kodai: `200`, `404`.

<details><summary>Public profile → <code>200</code></summary>

```http
GET /api/users/2
```

```http
HTTP/1.1 200 OK

{
  "id": 2,
  "username": "mantas",
  "email": null,
  "role": "User",
  "bio": "Listens to everything from Radiohead to Kendrick.",
  "createdAt": "2026-11-12T21:17:14.04918Z",
  "reviewCount": 6,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/users/2",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/users/2",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/users/2",
      "method": "DELETE"
    },
    "reviews": {
      "href": "http://localhost:5080/api/users/2/reviews",
      "method": "GET"
    },
    "users": {
      "href": "http://localhost:5080/api/users",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Not found → <code>404</code></summary>

```http
GET /api/users/999
```

```http
HTTP/1.1 404 Not Found

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "User not found.",
  "status": 404,
  "detail": "User with id 999 does not exist."
}
```

</details>


#### `GET /api/users/{userId}/reviews`

List a user's reviews (paged). Kas gali: visi. Atsako kodai: `200`, `400`, `404`.

<details><summary>A user's reviews → <code>200</code></summary>

```http
GET /api/users/2/reviews?pageSize=2
```

```http
HTTP/1.1 200 OK

{
  "items": [
    {
      "id": 24,
      "albumId": 5,
      "albumTitle": "Currents",
      "albumArtist": "Tame Impala",
      "songId": 19,
      "songTitle": "The Less I Know the Better",
      "overallRating": 4,
      "comment": "That bassline carries everything. The story is silly, which fits the song.",
      "createdAt": "2026-11-12T21:17:14.610153Z",
      "updatedAt": null
    },
    {
      "id": 21,
      "albumId": 4,
      "albumTitle": "Rumours",
      "albumArtist": "Fleetwood Mac",
      "songId": 14,
      "songTitle": "Dreams",
      "overallRating": 4.25,
      "comment": "Relaxing without being boring. I play it on long drives.",
      "createdAt": "2026-11-12T21:17:14.610138Z",
      "updatedAt": null
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 6,
  "totalPages": 3,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/users/2/reviews?page=1&pageSize=2",
      "method": "GET"
    },
    "first": {
      "href": "http://localhost:5080/api/users/2/reviews?page=1&pageSize=2",
      "method": "GET"
    },
    "last": {
      "href": "http://localhost:5080/api/users/2/reviews?page=3&pageSize=2",
      "method": "GET"
    },
    "next": {
      "href": "http://localhost:5080/api/users/2/reviews?page=2&pageSize=2",
      "method": "GET"
    }
  }
}
```

</details>


#### `POST /api/users`

Create a user (admin). Kas gali: administratorius. Atsako kodai: `201`, `400`, `401`, `403`, `409`, `422`.

<details><summary>Create (admin) → <code>201</code></summary>

```http
POST /api/users
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "username": "guest_dj",
  "email": "guest_dj@example.com",
  "password": "Listening123!"
}
```

```http
HTTP/1.1 201 Created
Location: /api/users/8

{
  "id": 8,
  "username": "guest_dj",
  "email": "guest_dj@example.com",
  "role": "User",
  "bio": null,
  "createdAt": "2026-11-12T21:17:17.5370975Z",
  "reviewCount": 0,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/users/8",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/users/8",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/users/8",
      "method": "DELETE"
    },
    "reviews": {
      "href": "http://localhost:5080/api/users/8/reviews",
      "method": "GET"
    },
    "users": {
      "href": "http://localhost:5080/api/users",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Email taken → <code>409</code></summary>

```http
POST /api/users
Authorization: Bearer <administratoriaus žetonas>
Content-Type: application/json

{
  "username": "guest_dj2",
  "email": "guest_dj@example.com",
  "password": "Listening123!"
}
```

```http
HTTP/1.1 409 Conflict

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Email already registered.",
  "status": 409,
  "detail": "The email 'guest_dj@example.com' is already in use."
}
```

</details>


#### `PUT /api/users/{userId}`

Update a user. Kas gali: pats naudotojas arba administratorius. Atsako kodai: `200`, `400`, `401`, `403`, `404`, `409`, `422`.

<details><summary>Edit own account → <code>200</code></summary>

```http
PUT /api/users/7
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "username": "new_listener",
  "email": "new_listener@example.com",
  "bio": "Jazz and soul."
}
```

```http
HTTP/1.1 200 OK

{
  "id": 7,
  "username": "new_listener",
  "email": "new_listener@example.com",
  "role": "User",
  "bio": "Jazz and soul.",
  "createdAt": "2026-11-12T21:17:16.056779Z",
  "reviewCount": 1,
  "_links": {
    "self": {
      "href": "http://localhost:5080/api/users/7",
      "method": "GET"
    },
    "update": {
      "href": "http://localhost:5080/api/users/7",
      "method": "PUT"
    },
    "delete": {
      "href": "http://localhost:5080/api/users/7",
      "method": "DELETE"
    },
    "reviews": {
      "href": "http://localhost:5080/api/users/7/reviews",
      "method": "GET"
    },
    "users": {
      "href": "http://localhost:5080/api/users",
      "method": "GET"
    }
  }
}
```

</details>

<details><summary>Role change by a user → <code>403</code></summary>

```http
PUT /api/users/7
Authorization: Bearer <naudotojo žetonas>
Content-Type: application/json

{
  "username": "new_listener",
  "email": "new_listener@example.com",
  "role": "Admin"
}
```

```http
HTTP/1.1 403 Forbidden

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden.",
  "status": 403,
  "detail": "Only admins can change roles."
}
```

</details>


#### `DELETE /api/users/{userId}`

Delete a user. Kas gali: pats naudotojas arba administratorius. Atsako kodai: `204`, `401`, `403`, `404`.

<details><summary>Delete (admin) → <code>204</code></summary>

```http
DELETE /api/users/8
Authorization: Bearer <administratoriaus žetonas>
```

```http
HTTP/1.1 204 No Content
```

</details>

<details><summary>Someone else's account → <code>403</code></summary>

```http
DELETE /api/users/3
Authorization: Bearer <naudotojo žetonas>
```

```http
HTTP/1.1 403 Forbidden

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden.",
  "status": 403,
  "detail": "You can only delete your own account."
}
```

</details>


## 5. Projekto išvados

* Sukurta muzikos atsiliepimų sistema iš trijų dalių: REST API (ASP.NET Core 10, EF Core), naudotojo sąsaja (React, TypeScript) ir Azure SQL duomenų bazė. Visos dalys laikomos vienoje Git saugykloje.
* Realizuoti trys hierarchiniai dalykinės srities objektai (albumas → daina → atsiliepimas) su jiems atitinkamais CRUD veiksmais. Taip pat realizuotos naudotojų paskyros, puslapiavimas, filtravimas, failų nuorodos ir iš kelių objektų sudaroma albumo apžvalga.
* Autentifikacija ir autorizacija realizuotos su JWT. Rolė saugoma žetone, prieigos žetonas trumpalaikis, o atnaujinimo žetonai pakeičiami kiekvieno atnaujinimo metu. Jei panaudotas jau pakeistas žetonas, atjungiamos visos naudotojo sesijos.
* API aprašytas OpenAPI specifikacija ir patikrintas Postman testų rinkiniu: 63 užklausos ir 123 patikros, apimančios sėkmingus ir klaidų atvejus (400, 401, 403, 404, 409, 422).
* Naudotojo sąsaja prisitaiko prie ekrano dydžio, turi vientisą stilių, modalinius langus, animacijas ir grįžtamąjį ryšį be naršyklės `alert` langų. Prieš realizaciją langai suprojektuoti wireframe'ais.
* Sistema talpinama Azure debesyje: API Azure App Service, sąsaja Azure Static Web Apps, duomenys Azure SQL Database. Diegimas automatizuotas su GitHub Actions: *ėlus pakeitimus į `main` šaką, atnaujinama pasikeitusi dalis.
* Nemokami Azure planai turi apribojimų: neaktyvi App Service ir duomenų bazė „užmiega“, todėl pirma užklausa po pertraukos gali užtrukti iki minutės. Tikram naudojimui reikėtų mokamo plano.
