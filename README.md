# Weather Forecast API

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Vue](https://img.shields.io/badge/Vue-3-42b883?logo=vue.js)
![Vuetify](https://img.shields.io/badge/Vuetify-4-1867C0?logo=vuetify)
![License](https://img.shields.io/badge/license-MIT-blue)

A full-stack weather forecast application that wraps the [Visual Crossing Weather API](https://www.visualcrossing.com/). The backend is an ASP.NET Core 10.0 REST API with in-memory caching and layered rate limiting; the frontend is a Vue 3 + Vuetify 4 SPA with support for single and multi-location queries.

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
- **In-memory caching** — responses cached for 30 minutes; cache keys are order-independent so `days,hours` and `hours,days` share the same entry
- **Cache status indicator** — result cards show a green "Cached" chip when served from cache (0 API records consumed) vs. the actual record cost on a miss
- **Layered rate limiting** — global server cap (120 req/min) chained with per-IP limits (30 req/min general, 3 req/min on multi-location due to upstream fan-out cost)
- **Country flags** — location cards display the country's flag sourced from flagcdn.com, making multi-location results easier to scan at a glance
- **Dark / light mode** — system preference respected on first load; iOS-style toggle switch; deep night-sky gradient in dark mode
- **Responsive UI** — expandable day panels, hourly breakdown table, paginated multi-location results (3 per page), and a raw JSON viewer

---

## Tech Stack

### Server

| Technology | Purpose |
| --- | --- |
| ASP.NET Core 10.0 | REST API framework |
| `IHttpClientFactory` | Managed HTTP client for upstream requests |
| `IMemoryCache` | Response caching |
| `System.Threading.RateLimiting` | Chained global + per-IP + per-endpoint rate limiters |
| Scalar | OpenAPI UI (replaces Swashbuckle, incompatible with .NET 10) |
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

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
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

The server is containerized via the `Dockerfile` in `server/`. On every push to `main` that touches `server/`, GitHub Actions builds a new Docker image, pushes it to Azure Container Registry, and updates the Container App automatically (`.github/workflows/deploy-backend.yml`).

Required environment variables on the Container App:

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

## API Documentation (Scalar)

The API ships with an interactive documentation UI powered by [Scalar](https://scalar.com/), the modern replacement for Swagger UI. It lets you read endpoint specs, fill in parameters, and fire real requests directly from the browser — no Postman or curl needed.

| Environment | URL |
| --- | --- |
| Local development | `http://localhost:5239/scalar/v1` |
| Production | `https://weather-api-app.whitefield-3aaa90f0.eastus.azurecontainerapps.io/scalar/v1` |

### How to use it

1. Open the URL above while the server is running
2. Expand an endpoint (e.g. `GET /api/weather`) in the left sidebar
3. Click **Test Request** — a form appears for each query parameter
4. Fill in at least `location` (e.g. `London,UK`) and click **Send**
5. The response body, status code, and headers (including `X-Cache-Status`) appear inline

The raw OpenAPI schema is also available at `/openapi/v1.json` if you want to import it into an external tool.

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

Cache keys are built from all query parameters, with multi-value fields sorted before hashing:

```text
weather:{location}:{date1}:{date2}:{include_sorted}:{elements_sorted}
weather:multi:{locations_sorted}:{datestart}:{dateend}:{include_sorted}:{elements_sorted}
```

Cache TTL: **30 minutes**. A cache hit skips the upstream Visual Crossing call entirely, reducing latency and preserving your API quota. The server sets an `X-Cache-Status: HIT|MISS` response header so the client can reflect cache state in the UI.

**Why sort?** Without sorting, `days,hours` and `hours,days` produce different keys despite requesting identical data — every unique ordering wastes a cache slot and burns an upstream API record. Sorting collapses all orderings to a single canonical key.

---

## Design Decisions

**Vite proxy in development, direct URL in production**
`client/.env.development` sets `VITE_API_BASE_URL` to empty, so all `/api` requests go to Vite's dev server proxy (`vite.config.ts`) which forwards them to `localhost:5239`. `client/.env.production` sets it to the Azure Container App URL. This means the client never needs to know which environment it's in — Vite handles the split at build time. CORS issues don't arise in development because the browser sees a same-origin request; in production, the server explicitly allows the Static Web App origin via the `FRONTEND_URL` environment variable.

**Layered rate limiting: global cap + per-IP + per-endpoint**
A single per-IP limiter is insufficient because a coordinated burst from many IPs could still overwhelm the server and drain the upstream Visual Crossing quota. The solution is a chained limiter: a global fixed-window cap (120 req/min across all traffic) sits in front of the per-IP cap (30 req/min). The per-IP limit was set at 30 rather than a lower value because normal interactive use — expanding hourly panels, re-querying with different date ranges — can generate legitimate bursts that a 10 req/min ceiling incorrectly throttles. The multi-location endpoint carries its own stricter per-IP policy (3 req/min) because each call fans out to multiple upstream records — a 5-location query over 8 days with hourly data can consume 960 records in a single request.

**Order-independent cache keys**
Cache keys built from raw query strings treat `days,hours` and `hours,days` as different entries, wasting cache slots and burning upstream quota for identical data. All multi-value parameters (`include`, `elements`, `locations`) are sorted before being incorporated into the cache key, collapsing all orderings to one canonical entry. The same sorted values are forwarded to the upstream API, which doesn't care about order.

**`X-Cache-Status` response header**
The cache hit/miss distinction exists only on the server — the JSON payload is identical in both cases. Rather than wrapping the response body in a metadata envelope (which would break the Visual Crossing response shape), the server sets an `X-Cache-Status: HIT|MISS` header. The client reads this via `response.headers.get()` and renders a green "Cached" chip instead of the record cost chip, giving the user transparent feedback at zero payload cost. The header is explicitly exposed in the CORS policy so browsers can read it cross-origin.

**Country flags via flagcdn.com**
Multi-location results benefit from a fast visual differentiator between cards. Country flags serve this purpose better than color-coding because they carry inherent meaning. `flagcdn.com` is a free, CDN-backed service that serves flag images by ISO 3166-1 alpha-2 code — no npm dependency, no bundled assets. The country name is extracted from Visual Crossing's `resolvedAddress` field (e.g. `"Phoenix, Arizona, United States"`) and looked up in a client-side mapping table. A white border with a drop shadow ensures visibility regardless of whether the flag's colors match the card header background.

**Scalar over Swashbuckle for OpenAPI UI**
Swashbuckle 6.x throws a `TypeLoadException` at startup on .NET 10 due to an internal interface change. Rather than waiting for a Swashbuckle patch, the project migrated to the built-in `Microsoft.AspNetCore.OpenApi` (`AddOpenApi` / `MapOpenApi`) paired with Scalar for the UI — the approach Microsoft recommends for .NET 9+ projects. Scalar has a significantly better UI than Swagger UI and is actively maintained.

**`icon` and core fields always included in elements**
The `icon` field drives weather icons throughout the UI; `temp`, `tempmax`, `tempmin`, `conditions`, and `description` are the minimum fields needed for a meaningful forecast display. When a user selects custom elements, these are silently merged in via `Set` deduplication. Without this, a user selecting only "Humidity" would get cards with no temperature or icons.

**`IMemoryCache` over Redis**
Single-instance deployment with no load balancer — a distributed cache adds operational overhead with no benefit. `IMemoryCache` is zero-config and sufficient. Cache is lost on restart but rebuilt on first request, which is acceptable. If the app ever scales to multiple instances, swapping to `IDistributedCache` requires no controller changes.

**Single vs multi response normalization**
The multi-location endpoint returns `{ locations: { name: data } }` while the single endpoint returns the location data directly. `WeatherResults.vue` normalizes both shapes into a flat array before rendering, keeping the template logic uniform.

---

## What I Learned

- **CORS is browser-enforced** — applied to *responses*, not requests. A proxy sidesteps it by making the call server-to-server. In production, the server must explicitly allow the frontend's origin.
- **ASP.NET Core endpoint routing and CORS** — `AddDefaultPolicy` + `UseCors()` is not sufficient for controller endpoints in .NET 5+; you must also call `RequireCors()` or use `[EnableCors]`.
- **Rate limiting in ASP.NET Core** — `System.Threading.RateLimiting` (added in .NET 7) provides built-in fixed-window, sliding-window, token-bucket, and concurrency limiters registered via `AddRateLimiter`.
- **Vuetify 4 component registration** — components must be explicitly imported and passed to `createVuetify()`, unlike Vuetify 2/3 which supported auto-registration.
- **`IMemoryCache` vs `IDistributedCache`** — in-process cache is zero-config for single-instance deployments; `IDistributedCache` is warranted when sharing cache across multiple instances or needing persistence across restarts.
- **Azure Container Apps environment variables** — .NET reads these via `IConfiguration` using `__` as the hierarchy separator (e.g. `WeatherApi__ApiKey` maps to `WeatherApi:ApiKey` in config).
- **Chained rate limiters in ASP.NET Core** — `PartitionedRateLimiter.CreateChained()` composes multiple limiters; the request must pass all of them. This enables a global server cap layered over per-IP and per-endpoint policies without duplicating logic.
- **Custom response headers and CORS** — headers added via `Response.Headers` are not automatically readable cross-origin. They must be explicitly listed in `.WithExposedHeaders()` in the CORS policy, otherwise the browser silently blocks access to them.
- **Vite environment files** — `.env.development` and `.env.production` are loaded automatically based on the build mode. Variables must be prefixed `VITE_` to be exposed to client code. This provides a clean dev/prod split without conditional logic in application code.
- **`PartitionedRateLimiter.CreateChained` vs `AddPolicy`** — `AddFixedWindowLimiter` and similar helpers are extension methods in a separate namespace; when they're unavailable, `AddPolicy` with `RateLimitPartition.GetFixedWindowLimiter` achieves the same result using only `System.Threading.RateLimiting` types.

---

## Acknowledgments

- [Visual Crossing Weather API](https://www.visualcrossing.com/) for the weather data
- [roadmap.sh](https://roadmap.sh) for the project prompt
- [Vuetify](https://vuetifyjs.com/) for the component library
- [Material Design Icons](https://materialdesignicons.com/) for the weather icon set
