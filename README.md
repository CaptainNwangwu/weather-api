# Weather Forecast API

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Vue](https://img.shields.io/badge/Vue-3-42b883?logo=vue.js)
![Vuetify](https://img.shields.io/badge/Vuetify-4-1867C0?logo=vuetify)
![Redis](https://img.shields.io/badge/Redis-caching-DC382D?logo=redis)
![License](https://img.shields.io/badge/license-MIT-blue)

A full-stack weather forecast application that wraps the [Visual Crossing Weather API](https://www.visualcrossing.com/). The backend is an ASP.NET Core 8.0 REST API with Redis caching and rate limiting; the frontend is a Vue 3 + Vuetify 4 SPA with support for single and multi-location queries.

> Built as part of the [roadmap.sh](https://roadmap.sh/projects/weather-api-wrapper-service) Weather API Wrapper project.

---

## Quick Start

```bash
# 1. Clone
git clone https://github.com/your-username/weather-api.git
cd weather-api

# 2. Set your Visual Crossing API key
cd server
dotnet user-secrets set "VisualCrossing:ApiKey" "YOUR_KEY_HERE"

# 3. Start Redis (Docker)
docker run -d -p 6379:6379 redis:alpine

# 4. Run the backend
dotnet run

# 5. In a separate terminal, run the frontend
cd ../client
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173) in your browser.

---

## Features

- **Single & multi-location forecasts** — look up weather for one place or compare several at once
- **Flexible date ranges** — query current conditions, historical data, or future forecasts
- **Granular field selection** — choose exactly which weather elements and data sections to include
- **Redis caching** — responses cached for 30 minutes; identical queries are served instantly without hitting the upstream API
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
| `IDistributedCache` (Redis) | Response caching |
| `System.Threading.RateLimiting` | Fixed-window rate limiter |
| Swashbuckle | Swagger / OpenAPI docs |

### Client

| Technology | Purpose |
| --- | --- |
| Vue 3 | Reactive UI framework (Composition API) |
| Vuetify 4 | Material Design component library |
| Vite | Dev server and build tool |
| TypeScript | Type checking |

### External

| Service | Purpose |
| --- | --- |
| [Visual Crossing](https://www.visualcrossing.com/) | Weather data source |
| Redis | Distributed cache backend |

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Redis](https://redis.io/) (or Docker)
- A free [Visual Crossing API key](https://www.visualcrossing.com/sign-up)

### Configuration

**API key** (stored securely via .NET user secrets):

```bash
cd server
dotnet user-secrets set "VisualCrossing:ApiKey" "YOUR_KEY_HERE"
```

**Redis connection** (`server/appsettings.json`):

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### Running in Development

```bash
# Terminal 1 — backend (http://localhost:5239)
cd server && dotnet run

# Terminal 2 — frontend (http://localhost:5173)
cd client && npm run dev
```

The Vite dev server proxies all `/api` requests to the backend, so there are no CORS issues during development.

---

## API Endpoints

### `GET /api/weather` — Single Location

| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| `location` | string | &#9635; | City, address, ZIP code, or `lat,long` |
| `date1` | string | &#9634; | Start date (`yyyy-MM-dd`); omit for current conditions |
| `date2` | string | &#9634; | End date (`yyyy-MM-dd`); requires `date1` |
| `include` | string | &#9634; | Comma-separated sections: `days`, `hours`, `current`, `alerts`, etc. |
| `elements` | string | &#9634; | Comma-separated fields: `temp`, `humidity`, `windspeed`, etc. |

**Example:**

```text
GET /api/weather?location=Phoenix,USA&date1=2025-07-01&date2=2025-07-07&include=days,hours
```

**Response:** Visual Crossing weather object with `resolvedAddress`, `timezone`, `days[]`, `currentConditions`, etc.

---

### `GET /api/weather/multi` — Multiple Locations

| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| `locations` | string | &#9635; | Pipe-separated list: `Phoenix,USA\|London,UK\|Hamburg,Germany` |
| `datestart` | string | &#9634; | Start date (`yyyy-MM-dd`) |
| `dateend` | string | &#9634; | End date (`yyyy-MM-dd`); requires `datestart` |
| `include` | string | &#9634; | Same as single endpoint |
| `elements` | string | &#9634; | Same as single endpoint |

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
│   ├── Services/
│   │   └── WeatherService.cs      # HTTP client + Redis cache logic
│   ├── appsettings.json           # Redis connection string
│   └── Program.cs                 # Middleware pipeline + DI setup
│
└── client/
    ├── src/
    │   ├── App.vue                # Tab-based form (single / multi)
    │   ├── components/
    │   │   └── WeatherResults.vue # Results display (days, hours, raw JSON)
    │   ├── config.js              # API base URL (empty → Vite proxy)
    │   └── main.ts                # Vuetify setup, theme, component registration
    └── vite.config.ts             # Dev server proxy config
```

---

## Middleware Pipeline

```text
UseCors → UseRateLimiter → UseHttpsRedirection → UseAuthorization → MapControllers
```

CORS is declared first so preflight `OPTIONS` requests are handled before any other middleware can short-circuit them.

---

## Caching

Cache keys are built from all query parameters **except the API key**:

```text
weather:{location}:{date1}:{date2}:{include}:{elements}
weather:multi:{locations}:{datestart}:{dateend}:{include}:{elements}
```

Cache TTL: **30 minutes**. A cache hit skips the upstream Visual Crossing call entirely, reducing latency and preserving your API quota.

---

## Design Decisions

**Vite proxy instead of CORS headers**
In development, the Vite dev server proxies `/api` requests to the backend. This avoids browser CORS enforcement entirely — the browser sends same-origin requests to Vite, and Vite performs server-to-server forwarding (no browser policy involved). In production, the same pattern is replicated with a reverse proxy (nginx / Caddy).

**`icon` always included in elements**
The Visual Crossing `icon` field drives all weather icons in the UI. If the user selects custom elements, `icon` is silently appended to the request so the icons always render correctly.

**Single vs multi response normalization**
The multi-location endpoint returns `{ locations: { name: data } }` while the single endpoint returns the location data directly. `WeatherResults.vue` normalizes both shapes into a flat array before rendering, keeping the template logic uniform.

---

## What I Learned

- **CORS is browser-enforced** — it is applied to *responses*, not requests. The server must include the correct headers on the response; a client-side flag cannot bypass it. A proxy sidesteps it by making the call server-to-server.
- **ASP.NET Core endpoint routing and CORS** — `AddDefaultPolicy` + `UseCors()` is not sufficient for controller endpoints in .NET 5+; you must also call `RequireCors()` or use `[EnableCors]`. The Vite proxy made this moot in dev.
- **Rate limiting in ASP.NET Core** — `System.Threading.RateLimiting` (added in .NET 7) provides built-in fixed-window, sliding-window, token-bucket, and concurrency limiters. Policies are registered in DI via `AddRateLimiter`, applied globally with `UseRateLimiter`, and can be scoped per-endpoint or globally via `options.GlobalLimiter`.
- **Vuetify 4 component registration** — components must be explicitly imported (`* as components from 'vuetify/components'`) and passed to `createVuetify()`, unlike Vuetify 2/3 which supported auto-registration via a separate plugin.
- **Redis `IDistributedCache`** — stores serialized JSON strings; cache keys need to be deterministic and exclude secrets like API keys.

---

## Acknowledgments

- [Visual Crossing Weather API](https://www.visualcrossing.com/) for the weather data
- [roadmap.sh](https://roadmap.sh) for the project prompt
- [Vuetify](https://vuetifyjs.com/) for the component library
- [Material Design Icons](https://materialdesignicons.com/) for the weather icon set
