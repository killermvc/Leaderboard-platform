# Leaderboard

A real-time leaderboard platform with an ASP.NET Core 9.0 backend and Angular 21 frontend. Players can submit scores, view rankings, and compete across multiple games, with a moderation system for score approval.

## Features

- **Authentication & Users** — Registration, login, JWT-based auth, profile management
- **Game Management** — Create and browse games (admin-only creation)
- **Score Submission** — Submit scores with moderator approval workflow
- **Game Client Scores** — API-key endpoint for name-only players, approved on submission
- **Real-time Leaderboards** — Redis-backed sorted sets for fast ranking queries
- **Moderation System** — Per-game moderators can approve/reject score submissions
- **API Keys** — Per-game, permission-scoped keys for game clients (header or query string)
- **Role-based Access** — Three tiers: User, Moderator, Admin
- **Reports** — Admin top-players report with date filtering
- **Search** — Combined game and user search
- **User Profiles** — Public profiles with rankings and score history

## Tech Stack

| Layer | Technology |
|---|---|
| Backend Runtime | .NET 9.0 |
| Backend Framework | ASP.NET Core Web API |
| Frontend | Angular 21 (Standalone Components, Signals) |
| Database | MySQL 8.0 (EF Core + Pomelo) |
| Cache | Redis (StackExchange.Redis) |
| Auth | JWT Bearer + API Keys + BCrypt |
| API Docs | Scalar + OpenAPI |

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
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

2. **Apply migrations:**

   ```bash
   cd backend
   dotnet ef database update
   ```

3. **Run the backend:**

   ```bash
   dotnet run
   ```

   The API starts at `http://localhost:5048`.

4. **API documentation** is available at `http://localhost:5048/scalar/v1` (Development mode).

### Frontend

1. **Install dependencies:**

   ```bash
   cd leaderboard-fe
   npm install
   ```

2. **Start the dev server:**

   ```bash
   npm start
   ```

   The frontend starts at `http://localhost:4200` and connects to the backend at `http://localhost:5048`.

## Configuration

Key settings in `backend/appsettings.json`:

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | MySQL connection string | `Server=localhost;Database=LeaderBoardDB;...` |
| `ConnectionStrings:Redis` | Redis connection string | `localhost` |
| `Jwt:Key` | HMAC-SHA256 signing key | _(hardcoded — change for production)_ |
| `Jwt:Issuer` | JWT issuer | `YourIssuer` |
| `Jwt:Audience` | JWT audience | `YourAudience` |
| `Cors:Origins` | Comma-separated allowed origins | `http://localhost:4200` |

## API Endpoints

### Auth (`/api/auth`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/register` | — | Register a new user |
| POST | `/login` | — | Login, returns JWT |
| GET | `/me` | Yes | Get current user |
| GET | `/search?q=` | — | Search users by username |
| GET | `/user/{userId}` | — | Public user profile |
| PUT | `/` | Yes | Change password |
| PUT | `/username` | Yes | Update username |
| PUT | `/promote/{userId}` | Admin | Promote user to Admin |

### Game (`/api/game`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/` | Admin | Create a game |
| GET | `/{id}` | — | Get a game |
| GET | `?limit=&offset=` | — | List games (paginated) |
| GET | `/player/{playerId}` | — | Games a player has participated in |

### Score (`/api/score`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/submit` | Yes | Submit a score |
| GET | `/leaderboard/{gameId}` | — | Leaderboard for a game |
| GET | `/leaderboard/{gameId}/rank/{userId}` | — | User's rank in a game |
| GET | `/leaderboard/{gameId}/top/{limit}` | — | Top N players |
| GET | `/scores/user/{userId}` | — | User's approved scores |
| GET | `/scores/recent` | — | Recent approved scores |
| GET | `/scores/{scoreId}` | — | Score details |
| GET | `/scores/lookup?gameId=&userId=` | — | Lookup top approved score |
| GET | `/scores/submissions` | — | All submissions (any status) |
| GET | `/scores/my-submissions` | Yes | Current user's submissions |

### Score V1 (`/api/v1/scores`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/submit` | API key | Submit a score for a player, approved immediately |

### Moderation (`/api/moderation`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/scores/{scoreId}/approve` | Moderator | Approve a score |
| POST | `/scores/{scoreId}/reject` | Moderator | Reject a score |
| GET | `/games/{gameId}/pending-scores` | Moderator | Pending scores for a game |
| GET | `/pending-scores` | Moderator | All pending scores user can moderate |
| POST | `/games/{gameId}/moderators/{userId}` | Admin | Assign game moderator |
| DELETE | `/games/{gameId}/moderators/{userId}` | Admin | Remove game moderator |
| GET | `/games/{gameId}/moderators` | — | List game moderators |
| GET | `/games/{gameId}/can-moderate` | Yes | Check if user can moderate |
| GET | `/my-games` | Yes | Games the user moderates |

### Reports (`/reports`)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/top-players?start_date=&end_date=&limit=` | Admin | Top players report |

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
| Query string | `GET /api/score/leaderboard/1?apiKey=<key>` (also accepts `api_key`) |

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

- Credential and role based endpoints stay JWT-only (`PUT /api/auth`, `PUT /api/auth/username`, anything requiring `Admin`/`Moderator`), so a key can never change a password, rename an account, or moderate/manage anything.
- `LastUsedAt` is written at most once every 5 minutes to avoid a database write per request.

> A key passed in the query string can end up in access logs and proxy logs. Prefer the header when the client allows it.

## Game Client Scores

`POST /api/v1/scores/submit` is the endpoint a game integration calls to report a score for one of its
players. It requires an API key with the `SubmitScores` permission, and the key's game — there is no
`gameId` in the URL, a key is already scoped to one game.

```http
POST /api/v1/scores/submit
X-API-Key: <key>
Content-Type: application/json

{
  "name": "Ryu",
  "score": 9000,
  "submissionId": "match-2026-09-28-0001",
  "title": "New record",
  "description": "optional"
}
```

| Field | Type | Rules |
|---|---|---|
| `name` | string, required | Player name, max 64 characters, trimmed. Identifies the player. |
| `score` | int, required | The score value. |
| `submissionId` | string, required | Client-generated idempotency key, max 128 characters. Retrying with the same ID returns the original submission. |
| `title` | string, optional | Defaults to `<game name> - <score>`. |
| `description` | string, optional | Free text. |
| `gameId` | int, optional | May only be sent as the key's own game, anything else → `403`. |

Behaviour:

- The score is stored with `Status = Approved` and no `UserId`: the player is a name, not an account.
  The name is kept in `Scores.PlayerName`, and the API returns it as `playerName` with `user: null`.
- One leaderboard entry per player name, compared case-insensitively after trimming
  (`"  ryu "` and `"Ryu"` are the same player). Only a higher score moves the entry; the display name
  shown is the one from the player's best approved score.
- A name that matches an account's username is still a separate entry, so a game client can never
  write to somebody's account leaderboard entry.
- The submission is added to the cached leaderboard immediately and the board is completed from the
  database when its cache entry is missing, so the first submission of a game never yields a partial
  leaderboard.
- `201` with the stored score, `400` for a validation error or when the game has submissions
  disabled, `401` without a valid key, `403` for a key without the permission or for another game.
- Guest entries have no score post to open, so the frontend shows them without a profile link.

Leaderboard cache layout in Redis, `leaderboard:v2:<gameId>`:

| Member | Meaning |
|---|---|
| `u:<userId>` | Best approved score of an account |
| `g:<normalized player name>` | Best approved score of a name-only player, lowercased and trimmed |

## Database Schema

| Table | Purpose |
|---|---|
| `Users` | User accounts |
| `Roles` | Role definitions (User, Moderator, Admin) |
| `UserRoles` | User-role assignments (many-to-many) |
| `Games` | Game definitions |
| `Scores` | Score submissions with approval status, nullable `UserId` and optional `PlayerName` for name-only players |
| `GameModerators` | Per-game moderator assignments |
| `ApiKeys` | Per-game API keys, stored as SHA-256 hashes |

## Project Structure

```
Leaderboard/
├── backend/               # ASP.NET Core backend
│   ├── Controllers/       # API controllers (Auth, Game, Score, Moderation, Reports)
│   ├── Models/            # Entities and DbContext
│   ├── Dtos/              # Data Transfer Objects
│   ├── Repositories/      # Repository pattern (interface + implementation)
│   ├── Services/          # Business logic (JWT, API keys)
│   ├── Middleware/        # API key authentication middleware
│   ├── Migrations/        # EF Core migrations
│   └── Program.cs         # Application entry point
│
└── leaderboard-fe/        # Angular 21 frontend
    └── src/
        ├── app/
        │   ├── core/              # Services and HTTP interceptors
        │   ├── auth/              # Login, register, account settings
        │   ├── games/             # Games list page
        │   ├── game-detail/       # Game detail + score submission
        │   ├── game-card/         # Reusable game card component
        │   ├── game-moderators/   # Manage game moderators (Admin)
        │   ├── pending-scores/    # Review pending scores (Moderator)
        │   ├── my-games/          # User's played games
        │   ├── my-submissions/    # User's score submissions
        │   ├── search-results/    # Combined game+user search
        │   ├── user-profile/      # Public user profile
        │   ├── score-post/        # Individual score detail
        │   ├── home-page/         # Dashboard
        │   └── header/            # Navigation header
        └── environments/          # Environment configs (dev/prod)
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
| `/games/:id` | Game Detail | Leaderboard + score submission |
| `/games/:id/moderators` | Game Moderators | Manage moderators (Admin) |
| `/games/:id/pending-scores` | Pending Scores | Approve/reject scores (Moderator) |
| `/mygames` | My Games | Games the user has played |
| `/my-submissions` | My Submissions | User's score submissions |
| `/scores/:id` | Score Post | Individual score detail |
| `/search` | Search Results | Game and user search |
| `/user/:id` | User Profile | Public profile with rankings |

### Frontend Scripts

| Script | Description |
|---|---|
| `npm start` | Start dev server (`http://localhost:4200`) |
| `npm run build` | Production build to `dist/` |
| `npm run test` | Run Karma unit tests |
