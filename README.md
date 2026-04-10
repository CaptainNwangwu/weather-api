# Weather Forecast API

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Vue](https://img.shields.io/badge/Vue-3-42b883?logo=vue.js)
![Vuetify](https://img.shields.io/badge/Vuetify-4-1867C0?logo=vuetify)
![License](https://img.shields.io/badge/license-MIT-blue)

A full-stack weather forecast application that wraps the [Visual Crossing Weather API](https://www.visualcrossing.com/). The backend is an ASP.NET Core 8.0 REST API with in-memory caching and rate limiting; the frontend is a Vue 3 + Vuetify 4 SPA with support for single and multi-location queries.

> Built as part of the [roadmap.sh](https://roadmap.sh/projects/weather-api-wrapper-service) Weather API Wrapper project.

**Live:** [https://nice-ground-02fe3ec0f.1.azurestaticapps.net](https://nice-ground-02fe3ec0f.1.azurestaticapps.net)

---

## Quick Start

```bash
# 1. Clone
git clone https://github.com/CaptainNwangwu/weather-api.git
cd weather-api

# 2. Set your Visual Crossing API key in appsettings.Development.json
#    (see Configuration section below)

# 3. Run the backend
cd server && dotnet run

# 4. In a separate terminal, run the frontend
cd client && npm install && npm run dev
```

Open [http://localhost:5173](http://localhost:5173) in your browser.

---

## Features

- **Single & multi-location forecasts** — look up weather for one place or compare several at once
- **Flexible date ranges** — query current conditions, historical data, or future forecasts
- **Granular field selection** — choose exactly which weather elements and data sections to include
- **In-memory caching** — responses cached for 30 minutes; identical queries are served instantly without hitting the upstream API
- **Rate limiting** — fixed-window policy (10 requests / minute per IP) to protect against abuse
- **Dark / light mode** — toggle between themes; sky-blue gradient in light mode, deep navy in dark mode
- **Responsive UI** — expandable day panels, hourly breakdown table, and a raw JSON viewer

---

## Tech Stack

### Server

| Technology | Purpose |
| --- | --- |
| ASP.NET Core 8.0 | REST API framework |
| `IHttpClientFactory` | Managed HTTP client for upstream requests |
| `IMemoryCache` | Response caching |
| `System.Threading.RateLimiting` | Fixed-window rate limiter |
| Swashbuckle | Swagger / OpenAPI docs |
| Docker | Containerized deployment |

### Client

| Technology | Purpose |
| --- | --- |
| Vue 3 | Reactive UI framework (Composition API) |
| Vuetify 4 | Material Design component library |
| Vite | Dev server and build tool |
| TypeScript | Type checking |

### Infrastructure

| Service | Purpose |
| --- | --- |
| [Azure Container Apps](https://azure.microsoft.com/en-us/products/container-apps) | Hosts the backend API |
| [Azure Static Web Apps](https://azure.microsoft.com/en-us/products/app-service/static) | Hosts the frontend SPA |
| [Visual Crossing](https://www.visualcrossing.com/) | Weather data source |

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- A free [Visual Crossing API key](https://www.visualcrossing.com/sign-up)

### Configuration

The server reads its API key from `appsettings.json` via the `WeatherApi:ApiKey` field. For local development, override it in `server/appsettings.Development.json` (this file is gitignored):

```json
{
  "WeatherApi": {
    "ApiKey": "YOUR_KEY_HERE"
  }
}
```

Alternatively, set it as an environment variable (useful in CI or containers):

```bash
export WeatherApi__ApiKey="YOUR_KEY_HERE"
```

Note the double underscore `__` — .NET uses this as the hierarchy separator for environment variables.

### Running in Development

```bash
# Terminal 1 — backend (http://localhost:5239)
cd server && dotnet run

# Terminal 2 — frontend (http://localhost:5173)
cd client && npm run dev
```

The Vite dev server proxies all `/api` requests to the backend, so there are no CORS issues during development.

---

## Deployment

The app is deployed as two separate Azure resources:

| Resource | Type | URL |
| --- | --- | --- |
| `weather-api-app` | Azure Container Apps | `https://weather-api-app.whitefield-3aaa90f0.eastus.azurecontainerapps.io` |
| `weather-api-client` | Azure Static Web Apps | `https://nice-ground-02fe3ec0f.1.azurestaticapps.net` |

### Backend (Azure Container Apps)

The server is containerized via the `Dockerfile` in `server/`. Required environment variables on the Container App:

| Variable | Description |
| --- | --- |
| `WeatherApi__ApiKey` | Your Visual Crossing API key |
| `FRONTEND_URL` | The Static Web App URL (added to CORS allowed origins) |

### Frontend (Azure Static Web Apps)

Deployments are triggered automatically on every push to `main` via GitHub Actions (`.github/workflows/`). No manual steps required.

---

## API Endpoints

### `GET /api/weather` — Single Location

| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| `location` | string | ✓ | City, address, ZIP code, or `lat,long` |
| `date1` | string | | Start date (`yyyy-MM-dd`); omit for current conditions |
| `date2` | string | | End date (`yyyy-MM-dd`); requires `date1` |
| `include` | string | | Comma-separated sections: `days`, `hours`, `current`, `alerts`, etc. |
| `elements` | string | | Comma-separated fields: `temp`, `humidity`, `windspeed`, etc. |

**Example:**

```text
GET /api/weather?location=Phoenix,USA&date1=2025-07-01&date2=2025-07-07&include=days,hours
```

---

### `GET /api/weather/multi` — Multiple Locations

| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| `locations` | string | ✓ | Pipe-separated list: `Phoenix,USA\|London,UK\|Hamburg,Germany` |
| `datestart` | string | | Start date (`yyyy-MM-dd`) |
| `dateend` | string | | End date (`yyyy-MM-dd`); requires `datestart` |
| `include` | string | | Same as single endpoint |
| `elements` | string | | Same as single endpoint |

**Example:**

```text
GET /api/weather/multi?locations=Phoenix,USA|London,UK&include=days,current
```

**Response:**

```json
{
  "locations": {
    "Phoenix,USA": { ... },
    "London,UK": { ... }
  }
}
```

---

## Project Structure

```text
weather-api/
├── server/
│   ├── Controllers/
│   │   └── WeatherController.cs   # Single + multi-location endpoints
│   ├── Dockerfile                 # Container image definition
│   ├── appsettings.json           # App configuration (API key placeholder)
│   └── Program.cs                 # Middleware pipeline + DI setup
│
└── client/
    ├── src/
    │   ├── App.vue                # Tab-based form (single / multi)
    │   ├── components/
    │   │   └── WeatherResults.vue # Results display (days, hours, raw JSON)
    │   ├── config.js              # API base URL (points to Azure in production)
    │   └── main.ts                # Vuetify setup, theme, component registration
    └── vite.config.ts             # Dev server proxy config (localhost only)
```

---

## Middleware Pipeline

```text
UseCors → UseRateLimiter → MapControllers
```

CORS is declared first so preflight `OPTIONS` requests are handled before any other middleware can short-circuit them. HTTPS redirection is handled at the Azure ingress level and is not applied in the app itself.

---

## Caching

Cache keys are built from all query parameters:

```text
weather:{location}:{date1}:{date2}:{include}:{elements}
weather:multi:{locations}:{datestart}:{dateend}:{include}:{elements}
```

Cache TTL: **30 minutes**. A cache hit skips the upstream Visual Crossing call entirely, reducing latency and preserving your API quota.

---

## Design Decisions

**Vite proxy in development, direct URL in production**
In development, the Vite dev server proxies `/api` requests to the backend — no CORS headers needed since the browser sees a same-origin request. In production, the client calls the Azure Container App URL directly and CORS is handled server-side, with the `FRONTEND_URL` environment variable controlling which origin is allowed.

**`icon` always included in elements**
The Visual Crossing `icon` field drives all weather icons in the UI. If the user selects custom elements, `icon` is silently appended so the icons always render correctly.

**Single vs multi response normalization**
The multi-location endpoint returns `{ locations: { name: data } }` while the single endpoint returns the location data directly. `WeatherResults.vue` normalizes both shapes into a flat array before rendering, keeping the template logic uniform.

**`IMemoryCache` over Redis**
Single-instance deployment with no load balancer — a distributed cache adds operational overhead with no benefit. `IMemoryCache` is zero-config and sufficient. Cache is lost on restart but rebuilt on first request, which is acceptable. If the app ever scales to multiple instances, swapping to `IDistributedCache` requires no controller changes.

---

## What I Learned

- **CORS is browser-enforced** — applied to *responses*, not requests. A proxy sidesteps it by making the call server-to-server. In production, the server must explicitly allow the frontend's origin.
- **ASP.NET Core endpoint routing and CORS** — `AddDefaultPolicy` + `UseCors()` is not sufficient for controller endpoints in .NET 5+; you must also call `RequireCors()` or use `[EnableCors]`.
- **Rate limiting in ASP.NET Core** — `System.Threading.RateLimiting` (added in .NET 7) provides built-in fixed-window, sliding-window, token-bucket, and concurrency limiters registered via `AddRateLimiter`.
- **Vuetify 4 component registration** — components must be explicitly imported and passed to `createVuetify()`, unlike Vuetify 2/3 which supported auto-registration.
- **`IMemoryCache` vs `IDistributedCache`** — in-process cache is zero-config for single-instance deployments; `IDistributedCache` is warranted when sharing cache across multiple instances or needing persistence across restarts.
- **Azure Container Apps environment variables** — .NET reads these via `IConfiguration` using `__` as the hierarchy separator (e.g. `WeatherApi__ApiKey` maps to `WeatherApi:ApiKey` in config).

---

## Acknowledgments

- [Visual Crossing Weather API](https://www.visualcrossing.com/) for the weather data
- [roadmap.sh](https://roadmap.sh) for the project prompt
- [Vuetify](https://vuetifyjs.com/) for the component library
- [Material Design Icons](https://materialdesignicons.com/) for the weather icon set
