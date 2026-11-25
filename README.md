# Leaderboard

A real-time leaderboard platform with an ASP.NET Core 10.0 backend and Angular 21 frontend. Players can submit scores, view rankings, and compete across multiple games and leaderboards, with a moderation system for score approval. Game clients can submit scores through scoped API keys.

## Features

- **Authentication & Users** — Clerk-managed registration/login, local role and profile mapping, webhook-driven user sync
- **Game Management** — Create, browse, and update games (admin-only creation; owners/admins can edit)
- **Multiple Leaderboards per Game** — Create and manage named boards under a game; submissions, rankings, moderation, reports, and cache are scoped to a board
- **Score Submission** — Manual score submission with moderator approval workflow
- **Game Client Scores** — API-key endpoint for name-only players, approved on submission, with idempotent retries
- **Real-time Leaderboards** — Redis-backed sorted sets for fast ranking queries
- **Moderation System** — Per-game moderators can approve/reject score submissions
- **API Keys** — Per-game, permission-scoped keys for game clients (header or query string)
- **Role-based Access** — Three tiers: User, Moderator, Admin, plus game-level moderator assignments
- **Reports** — Admin top-players report with date and leaderboard filtering
- **Search** — Combined game and user search
- **User Profiles** — Public profiles with per-board rankings and score history
- **Localization** — English and Spanish UI

## Tech Stack

| Layer | Technology |
|---|---|
| Backend Runtime | .NET 10.0 |
| Backend Framework | ASP.NET Core Web API |
| Frontend | Angular 21 (Standalone Components, Signals) |
| Database | MySQL 8.0 (EF Core + Microting Pomelo provider) |
| Cache | Redis (StackExchange.Redis) |
| Auth | Clerk JWT Bearer + API Keys |
| API Docs | Scalar + OpenAPI |

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (with npm)
- MySQL 8.0
- Redis

## Setup

### Backend

1. **Create the MySQL database and user:**

   ```sql
   CREATE DATABASE LeaderBoardDB;
   CREATE USER 'LeaderBoardAdmin'@'localhost' IDENTIFIED BY '12345';
   GRANT ALL PRIVILEGES ON LeaderBoardDB.* TO 'LeaderBoardAdmin'@'localhost';
   ```

2. **Configure secrets.** Copy `backend/.env.example` to `backend/.env` and fill in the values, or set the equivalent environment variables (`ConnectionStrings__DefaultConnection`, `ConnectionStrings__Redis`, `Clerk__Issuer`, `Clerk__SecretKey`, `Clerk__WebhookSecret`, `Cors__Origins`). See [Configuration](#configuration).

3. **Apply migrations:**

   ```bash
   cd backend
   dotnet ef database update
   ```

4. **Run the backend:**

   ```bash
   dotnet run
   ```

   The API starts at `http://localhost:5048`.

5. **API documentation** is available at `http://localhost:5048/scalar/v1` (Development mode).

6. **Clerk webhook (optional but recommended):** point a Clerk webhook at `POST /api/webhooks/clerk` and subscribe to `user.created`, `user.updated`, and `user.deleted`.

### Frontend

1. **Install dependencies:**

   ```bash
   cd leaderboard-fe
   npm install
   ```

2. Set `clerkPublishableKey` in the active Angular environment file (`src/environments/environment.ts`) to the publishable key from Clerk.

   The Angular client uses the community-maintained [`ngx-clerk`](https://github.com/anagstef/ngx-clerk) adapter for Clerk UI, session state, and tokens. It is compatible with Angular 20+ but is not an official Clerk package.

3. **Start the dev server:**

   ```bash
   npm start
   ```

   The frontend starts at `http://localhost:4200` and connects to the backend at `http://localhost:5048`.

## Configuration

Key settings in `backend/appsettings.json` (overridable through environment variables using the `__` separator):

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | MySQL connection string | _(empty)_ |
| `ConnectionStrings:Redis` | Redis connection string | _(empty)_ |
| `Clerk:Issuer` | Clerk token issuer; enables Clerk authentication when non-empty | _(empty)_ |
| `Clerk:SecretKey` | Clerk Backend API secret key for user search and directory lookups | _(empty)_ |
| `Clerk:WebhookSecret` | Clerk webhook signing secret | _(empty)_ |
| `Clerk:AdminSeedClerkIds` | Comma-separated Clerk user IDs granted Admin on first sign-in (bootstrap) | _(empty)_ |
| `Cors:Origins` | Comma-separated allowed origins | `http://localhost:4200` |
| `RateLimiting:ScoreSubmission:IpCapacity` | Token bucket capacity per IP for V1 score submissions | `60` |
| `RateLimiting:ScoreSubmission:IpRefillPerSecond` | Token refill rate per IP | `1` |
| `RateLimiting:ScoreSubmission:ApiKeyCapacity` | Token bucket capacity per API key | `120` |
| `RateLimiting:ScoreSubmission:ApiKeyRefillPerSecond` | Token refill rate per API key | `4` |
| `RateLimiting:ScoreSubmission:StateTtlSeconds` | TTL for rate-limit state in Redis | `300` |

## API Endpoints

Base URLs are shown relative to the API root (`http://localhost:5048`).

### Auth (`/api/auth`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/me` | Yes | Get current user and roles |
| GET | `/user/{userId}` | — | Public user profile with per-board rankings |
| PUT | `/promote/{userId}` | Yes* | Promote a user to Admin (first admin can self-bootstrap) |

### Users (`/api/users`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/search?q=&limit=` | Yes | Search Clerk users linked to local accounts |

### Webhooks (`/api/webhooks/clerk`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/` | — | Receive signed Clerk lifecycle webhooks |

### Game (`/api/game`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/` | Admin | Create a game |
| GET | `/{id}` | — | Get a game |
| GET | `?limit=&offset=` | — | List games (paginated) |
| GET | `/player/{playerId}` | — | Games a player has participated in |
| PUT | `/{id}` | Yes | Update a game (admin or owner) |

### Leaderboards (`/api/games/{gameId}/leaderboards`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/` | — | List leaderboards for a game |
| GET | `/{leaderboardId}` | — | Get a leaderboard |
| POST | `/` | Admin/Moderator | Create a leaderboard |
| PUT | `/{leaderboardId}` | Admin/Moderator | Update a leaderboard |
| DELETE | `/{leaderboardId}` | Admin | Delete an empty leaderboard |

### Score (`/api/score`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/submit` | Yes | Submit a score (requires `LeaderboardId`) |
| GET | `/leaderboard/{gameId}` | — | Leaderboard for a game's default board |
| GET | `/leaderboard/board/{leaderboardId}` | — | Leaderboard for a specific board |
| GET | `/leaderboard/{gameId}/rank/{userId}` | — | User's rank in a game's default board |
| GET | `/leaderboard/board/{leaderboardId}/rank/{userId}` | — | User's rank in a specific board |
| GET | `/leaderboard/{gameId}/top/{limit}` | — | Top N players in a game's default board |
| GET | `/scores/user/{userId}` | — | User's approved scores (`?leaderboardId=` optional) |
| GET | `/scores/recent` | — | Recent approved scores (`?leaderboardId=` optional) |
| GET | `/scores/{scoreId}` | — | Score details |
| GET | `/scores/lookup?gameId=&userId=` | — | Lookup top approved score (`&leaderboardId=` optional) |
| GET | `/scores/submissions` | — | All submissions, any status (`?leaderboardId=` optional) |
| GET | `/scores/my-submissions` | Yes | Current user's submissions (`?leaderboardId=` optional) |

### Score V1 (`/api/v1/scores`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/submit` | API key | Submit a score for a named player on a selected leaderboard, approved immediately |
| DELETE | `/game/{gameId}/leaderboard/{leaderboardId}/player/{playerName}` | API key | Delete a player's scores from a leaderboard |
| GET | `/game/{gameId}/leaderboard/{leaderboardId}/player/{playerName}` | API key | Get a player's approved score on a leaderboard |
| GET | `/game/{gameId}/leaderboard/{leaderboardId}` | API key | Get a leaderboard's approved entries |

### Moderation (`/api/moderation`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/scores/{scoreId}/approve` | Moderator | Approve a score |
| POST | `/scores/{scoreId}/reject` | Moderator | Reject a score |
| GET | `/games/{gameId}/pending-scores` | Moderator | Pending scores for a game (`?leaderboardId=` optional) |
| GET | `/pending-scores` | Moderator | All pending scores user can moderate (`?leaderboardId=` optional) |
| POST | `/games/{gameId}/moderators/{userId}` | Admin | Assign game moderator |
| DELETE | `/games/{gameId}/moderators/{userId}` | Admin | Remove game moderator |
| GET | `/games/{gameId}/moderators` | — | List game moderators |
| GET | `/games/{gameId}/can-moderate` | Yes | Check if user can moderate |
| GET | `/my-games` | Yes | Games the user moderates |

### Reports (`/reports`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/top-players?start_date=&end_date=&limit=` | Admin | Top players report (`&leaderboardId=` optional) |

### API Keys (`/api/apikey`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/` | Moderator/Admin | Create a key for a game |
| GET | `/game/{gameId}` | Moderator/Admin | List a game's keys |
| POST | `/{id}/revoke` | Moderator/Admin | Revoke a key |
| POST | `/{id}/regenerate` | Moderator/Admin | Revoke and replace a key |
| GET | `/game/{gameId}/can-manage` | Yes | Check if user can manage keys |

The full key is only returned once, on creation. Everything else exposes prefix/metadata only.

## API Key Authentication

Game integrations authenticate with a per-game API key instead of a JWT. The key is accepted in either place:

| Where | Example |
|---|---|
| Header | `X-API-Key: <key>` |
| Query string | `GET /api/v1/scores/game/1/leaderboard/2?apiKey=<key>` (also accepts `api_key`) |

The query string form exists for clients that cannot set headers when sending a request. The header wins when both are present.

Behaviour:

- Requests without a key are unaffected — still anonymous, or authenticated by JWT when an `Authorization` header is present. A validated JWT always takes precedence over an API key.
- The key is SHA-256 hashed and matched against `ApiKeys.KeyHash`; the raw value is never stored or logged.
- Unknown, revoked or expired key → `401`. Key used against a `gameId` other than the one it is scoped to (route value or `gameId` query) → `403`. If the lookup itself fails, the request is rejected with `503` rather than falling through to anonymous access.
- Successful keys are attached to the request as claims, readable from any controller via `Leaderboard.Middleware.ApiKeyClaims`:

  ```csharp
  User.GetApiKeyId();                                     // id of the key
  User.GetApiKeyGameId();                                 // game the key is scoped to
  User.GetApiKeyPermissions();                            // ApiKeyPermissions flags
  User.HasApiKeyPermission(ApiKeyPermissions.SubmitScores);
  ```

- Credential and role based endpoints stay JWT-only (anything requiring `Admin`/`Moderator`), so a key can never change a password, rename an account, or moderate/manage anything.
- `LastUsedAt` is written at most once every 5 minutes to avoid a database write per request.

> A key passed in the query string can end up in access logs and proxy logs. Prefer the header when the client allows it.

Permissions (`ApiKeyPermissions`): `None`, `SubmitScores`, `ReadScores`, `ReadLeaderboard`, `DeleteScores`.

## Game Client Scores

`POST /api/v1/scores/submit` is the endpoint a game integration calls to report a score for one of its players. It requires an API key with the `SubmitScores` permission and an explicit `leaderboardId`. The selected board must belong to the game scoped to the key.

```http
POST /api/v1/scores/submit
X-API-Key: <key>
Content-Type: application/json

{
  "leaderboardId": 4,
  "name": "Ryu",
  "score": 9000,
  "submissionId": "match-2026-09-28-0001",
  "title": "New record",
  "description": "optional"
}
```

| Field | Type | Rules |
|---|---|---|
| `leaderboardId` | int, required | Target leaderboard. It must belong to the API key's game and be active. |
| `name` | string, required | Player name, max 64 characters, trimmed. Identifies the player. |
| `score` | int, required | The score value. |
| `submissionId` | string, required | Client-generated idempotency key, max 128 characters. Retrying with the same ID returns the original submission. |
| `title` | string, optional | Defaults to `<game name> - <score>`. |
| `description` | string, optional | Free text. |
| `gameId` | int, optional | May only be sent as the key's own game, anything else → `403`. |

Behaviour:

- The score is stored with `Status = Approved` and no `UserId`: the player is a name, not an account. The name is kept in `Scores.PlayerName`, and the API returns it as `playerName` with `user: null`.
- One leaderboard entry per player name, compared case-insensitively after trimming (`"  ryu "` and `"Ryu"` are the same player). Only a higher score moves the entry; the display name shown is the one from the player's best approved score.
- A name that matches an account's username is still a separate entry, so a game client can never write to somebody's account leaderboard entry.
- The submission is added to the cached leaderboard immediately and the board is completed from the database when its cache entry is missing, so the first submission of a game never yields a partial leaderboard.
- `201` with the stored score, `400` for a validation error or inactive board, `401` without a valid key, `403` for a key without the permission or a board from another game, and `404` for a missing board.
- Guest entries have no score post to open, so the frontend shows them without a profile link.
- V1 submissions are rate-limited per IP and per API key (see Configuration); `429` includes a `Retry-After` header.

Leaderboard cache layout in Redis, `leaderboard:v3:<leaderboardId>`:

| Member | Meaning |
|---|---|
| `u:<userId>` | Best approved score of an account |
| `g:<normalized player name>` | Best approved score of a name-only player, lowercased and trimmed |

## Database Schema

| Table | Purpose |
|---|---|
| `Users` | User accounts linked to Clerk identities |
| `Roles` | Role definitions (User, Moderator, Admin) |
| `UserRoles` | User-role assignments (many-to-many) |
| `Games` | Game definitions |
| `Leaderboards` | Named boards belonging to a game |
| `Scores` | Score submissions with approval status, nullable `UserId` and optional `PlayerName` for name-only players; scoped to a `LeaderboardId` |
| `GameModerators` | Per-game moderator assignments |
| `ApiKeys` | Per-game API keys, stored as SHA-256 hashes |
| `ClerkWebhookEvents` | Processed Clerk webhook events |

## Project Structure

```
Leaderboard/
├── backend/               # ASP.NET Core backend
│   ├── Controllers/       # API controllers (Auth, Game, Leaderboard, Score, V1, Moderation, Reports, Users, ApiKey, ClerkWebhook)
│   ├── Models/            # Entities and DbContext
│   ├── Dtos/              # Data Transfer Objects
│   ├── Repositories/      # Repository pattern (interface + implementation)
│   ├── Services/          # Business logic (JWT/Clerk, API keys)
│   ├── Middleware/        # API key auth, Clerk user sync, rate limiting, error handling
│   ├── Options/           # Strongly-typed configuration
│   ├── Errors/            # Error codes and custom exceptions
│   ├── Migrations/        # EF Core migrations
│   └── Program.cs         # Application entry point
│
├── leaderboard-fe/        # Angular 21 frontend
│   └── src/
│       ├── app/
│       │   ├── core/              # Services and HTTP interceptors
│       │   ├── auth/              # Login, register, account settings
│       │   ├── games/             # Games list page
│       │   ├── game-detail/       # Game detail + score submission
│       │   ├── game-card/         # Reusable game card component
│       │   ├── game-moderators/   # Manage game moderators (Admin)
│       │   ├── game-configure/    # Game configuration
│       │   ├── new-game/          # Create game
│       │   ├── leaderboard-management/ # Manage leaderboards
│       │   ├── pending-scores/    # Review pending scores (Moderator)
│       │   ├── my-submissions/    # User's score submissions
│       │   ├── search-results/    # Combined game+user search
│       │   ├── user-profile/      # Public user profile
│       │   ├── score-post/        # Individual score detail
│       │   ├── api-keys/          # Manage API keys
│       │   ├── home-page/         # Dashboard
│       │   └── header/            # Navigation header
│       └── environments/          # Environment configs (dev/prod)
│
├── tests/                 # Backend xUnit tests
└── docs/                  # Implementation plans
```

## Frontend

The frontend is a standalone Angular 21 SPA using modern Angular features:

- **Standalone Components** — No NgModules, all components are standalone
- **Signals** — State management via `signal()`, `computed()`, and `toSignal()`
- **Control Flow** — `@if`, `@else`, `@for`, `@switch` template syntax
- **Reactive Forms** — Used for score submission and account settings
- **Custom SCSS Design** — Dark theme with Space Grotesk font, no UI library
- **Icons** — GitHub Octicons via `@ng-icons`

### Frontend Routes

| Route | Component | Description |
|---|---|---|
| `/` | Home | Dashboard with recent scores |
| `/auth/login` | Login | User login |
| `/auth/register` | Register | New user registration |
| `/auth/account` | Account Settings | Change username/password |
| `/games` | Games | Browse all games |
| `/games/new` | New Game | Create a game |
| `/games/:id` | Game Detail | Leaderboard + score submission |
| `/games/:id/configure` | Game Configure | Edit game settings |
| `/games/:id/leaderboards` | Leaderboard Management | Manage leaderboards |
| `/games/:id/moderators` | Game Moderators | Manage moderators (Admin) |
| `/games/:id/pending-scores` | Pending Scores | Approve/reject scores (Moderator) |
| `/games/:id/api-keys` | API Keys | Manage API keys |
| `/my-submissions` | My Submissions | User's score submissions |
| `/scores/:id` | Score Post | Individual score detail |
| `/search` | Search Results | Game and user search |
| `/user/:id` | User Profile | Public profile with rankings |

### Frontend Scripts

| Script | Description |
|---|---|
| `npm start` | Start dev server (`http://localhost:4200`) |
| `npm run build` | Production build to `dist/` (English) |
| `npm run build:es` | Production build to `dist/` (Spanish) |
| `npm run test` | Run Karma unit tests |
| `npm run extract-i18n` | Extract translatable strings to XLF |

## Localization

The application supports **English (en)** and **Spanish (es)** with full internationalization support:

### Features
- **Compile-time translation**: Angular's built-in i18n with XLF translation files
- **Runtime language switching**: Language selector in header with page reload
- **Automatic detection**: Browser language detection with localStorage persistence
- **Locale-aware formatting**: Dates, numbers, and currency adapt to current locale
- **Backend error codes**: Machine-readable error codes with frontend translation

### Supported Languages
- **English (en)** - Default
- **Spanish (es)** - Fully translated

### Adding Translations
1. Mark user-facing text in templates with `i18n="@@unique.id"` attributes
2. Run `npm run extract-i18n` to update `src/locale/messages.xlf`
3. Add Spanish translations in `src/locale/messages.es.xlf`
4. Build for both locales: `npm run build && npm run build:es`

### Language Selector
The header includes an EN/ES toggle that persists the user's preference in localStorage and reloads the page to load the appropriate locale build.

### Backend Error Codes
The API returns standardized error codes (e.g., `GAME_NOT_FOUND`, `VALIDATION_FAILED`) instead of localized messages. The frontend maps these to translated user-facing messages.

See [I18N_GUIDE.md](leaderboard-fe/I18N_GUIDE.md) for detailed documentation.