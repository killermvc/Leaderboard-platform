# Leaderboard Backend

This project is a backend system for a real-time leaderboard service. The service allows users to compete in various games or activities, track their scores, and view their rankings on a leaderboard. The system features user authentication, score submission, real-time leaderboard updates, and score history tracking. Redis sorted sets are used to manage and query the leaderboards efficiently.

## Features

- User authentication is handled by Clerk.
- Score submission: Users can submit their scores for various games or activities.
- Real-time leaderboard updates: The leaderboard is updated in real-time to reflect the latest scores submitted by users.
- Score history tracking: Users can view their score history for a specific game or activity.
- Top Players Report: Generate reports on the top players for a specific period.

## Endpoints

The API provides the following endpoints:

Authentication and identity:
- GET `api/auth/me`: Get the authenticated local user ID and roles. Authentication and profile management are handled by Clerk.
- GET `api/users/search?q={query}&limit={limit}`: Search Clerk users and return active local user links. Requires `Clerk:SecretKey`.
- POST `api/webhooks/clerk`: Receive signed Clerk lifecycle webhooks.

Configure the Clerk dashboard webhook with these events:
- `user.created`: Provision the local application user.
- `user.deleted`: Disable access, remove roles/moderator assignments, release game ownership, and preserve score history.
- `user.updated`: Accepted for lifecycle compatibility; profile fields remain owned by Clerk.

Set `ConnectionStrings__DefaultConnection`, `ConnectionStrings__Redis`, `Clerk__Issuer`, `Clerk__SecretKey`, and `Clerk__WebhookSecret` through deployment configuration. Copy `backend/.env.example` to `backend/.env` for the local variable template, then load those values into the process environment before starting the API. Never commit real Clerk or database secrets.


Game:
- POST `api/games`: Create a new game. Requires 'Admin' role.
- GET  `api/games/{gameId}`: Get a specific game by ID.

Score:
- POST `api/scores`: Submit a score for a user in a specific game.
- GET  `api/leaderboard/{gameId}`: Get the leaderboard for a specific game.
- GET  `api/leaderboard/{gameId}/rank/{userId}`: Get the rank of a specific user in a specific game.
- GET  `api/leaderboard/{gameId}/top/{limit}`: Get the top N players in a specific game.

Reports:
- GET  `api/reports/top-players/{gameId}/{limit}`: Get the top players for a specific game.