# Copilot instructions for Leaderboard

Do not run build commands at root directory, cd into leaderboard-fe or backend first.

## Repository shape

This repository contains two applications:

- `backend/` is an ASP.NET Core 9 Web API. Controllers handle HTTP concerns, repositories handle EF Core/MySQL queries and Redis leaderboard operations, and services/middleware handle authentication, API-key authorization, rate limiting, and JWT/Clerk integration.
- `leaderboard-fe/` is an Angular 21 standalone SPA. `src/app/core/` contains the HTTP-facing services and auth interceptor; feature folders contain routed pages and reusable UI.
- `tests/Leaderboard.Tests/` contains xUnit/Moq backend tests. Frontend unit tests live beside Angular services and components as `*.spec.ts`.

The backend and frontend communicate locally over `http://localhost:5048` and `http://localhost:4200`. MySQL and Redis are runtime dependencies for the backend. Development OpenAPI/Scalar is exposed at `/scalar/v1`.

## Build, test, and run commands

Run commands from the directory shown:

```bash
# Backend
dotnet restore Leaderboard.sln
dotnet build Leaderboard.sln
dotnet test Leaderboard.sln
dotnet test tests/Leaderboard.Tests/Leaderboard.Tests.csproj \
  --filter "FullyQualifiedName~Leaderboard.Tests.ScoreControllerTests"

# Apply EF Core migrations (requires configured MySQL)
cd backend
dotnet ef database update
dotnet run

# Frontend
cd leaderboard-fe
npm install
npm start
npm run build
npm test
npx ng test --include="src/app/core/score-service.spec.ts" --watch=false
```

There is no repository lint script configured. The frontend `npm test` command runs the Angular Karma suite; use `--include` to target a spec file. Backend test classes can be selected with `dotnet test --filter "FullyQualifiedName~..."`.

## Architecture and request flow

- `backend/Program.cs` registers `AppDbContext` (Pomelo MySQL), a singleton StackExchange.Redis connection, repository/service implementations, CORS, JWT bearer authentication, OpenAPI, and middleware. The middleware order is significant: JWT authentication runs first, then Clerk user synchronization, game-client rate limiting, API-key authentication, authorization, CORS, and controller mapping.
- Normal user score submissions go through `Controllers/ScoreController.cs` and `Repositories/ScoreRepository.cs`. They are persisted as `Pending` and only approved scores enter Redis leaderboards.
- Moderators approve/reject submissions through `ModerationController`; the repository updates the score status and keeps the Redis representation consistent. Admins manage games, game moderators, and API keys.
- Game-client endpoints are versioned under `Controllers/V1/`. `ApiKeyAuthenticationMiddleware` accepts `X-API-Key` (preferred) or `apiKey`/`api_key` query parameters, hashes the raw key, validates its game scope and expiry/revocation, and adds API-key claims. A valid JWT takes precedence over an API key; a presented key that cannot be validated must not fall through as anonymous.
- Game-client scores are name-only (`UserId == null`), approved immediately, and deduplicated by `(GameId, SubmissionId)`. Player names are trimmed and normalized case-insensitively for leaderboard members. Registered-user and guest members are distinct Redis members.
- Redis leaderboard keys use `leaderboard:v2:<gameId>`, with `u:<userId>` for account entries and `g:<normalized player name>` for game-client entries. Preserve this format or update all read/write and cache-rebuild paths together.
- EF Core schema changes are represented by a migration plus the checked-in `AppDbContextModelSnapshot.cs`. Update `Models/AppDbContext.cs` and then create/apply a migration rather than editing generated migration history manually.
- The Angular app is configured in `src/app/app.config.ts` with standalone router, HTTP client, functional Clerk token interceptor, and `ngx-clerk`. Routes are defined centrally in `src/app/app.routes.ts`; services call backend endpoint groups using URLs from `src/environments/`.

## Codebase-specific conventions

- Backend uses nullable reference types, file-scoped namespaces, primary constructors in newer classes, repository interfaces, and DTO mapping at controller boundaries. Keep persistence entities in `backend/Models`, transport types in `backend/Dtos`, and database access in repositories.
- Preserve the distinction between JWT identity and game-client API-key identity. Role-protected account/admin/moderation operations remain JWT-only; API keys are scoped to one game and limited by `ApiKeyPermissions`.
- Never persist or log raw API keys. API keys are stored as SHA-256 hashes; return the full key only at creation time. Header authentication takes precedence over query-string authentication.
- Keep score status semantics intact: user submissions begin pending, approved scores are leaderboard-visible, and API submissions are approved immediately. Game-client `SubmissionId` is the idempotency key and must remain unique per game.
- Use the existing pagination parameters (`limit`, `offset`) on list endpoints and keep the frontend service DTO shapes aligned with backend JSON, including nullable `user`/`playerName` for guest scores.
- The frontend uses strict TypeScript and Angular strict templates, standalone components, signals for local/derived state, functional `inject()`-style APIs where appropriate, reactive forms, native `@if`/`@for`/`@switch` control flow, and SCSS. The more specific rules in `leaderboard-fe/.github/copilot-instructions.md` apply to frontend files.
- Environment files hold frontend API base URLs and the Clerk publishable key. Do not commit new credentials or move secrets into source; use the existing configuration mechanism for local/deployment values.

## Related documentation

Use the root `README.md` for setup, endpoint behavior, API-key semantics, database entities, and frontend routes. `backend/bruno/` contains request examples for manually exercising the API.
