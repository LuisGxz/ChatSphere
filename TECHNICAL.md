# ChatSphere — technical overview

A deep-dive into the architecture and the decisions worth explaining. For setup and a feature tour see
[`README.md`](README.md).

## Solution shape

```
chatsphere/
├── backend/                          .NET 9 solution (Clean Architecture)
│   └── src/
│       ├── ChatSphere.Domain         entities, enums (no dependencies)
│       ├── ChatSphere.Application    interfaces, DTOs, services, validators
│       ├── ChatSphere.Infrastructure EF Core, auth primitives, Redis presence
│       └── ChatSphere.Api            controllers, SignalR hub, middleware, DI, demo simulator
│   └── tests/ChatSphere.Tests        xUnit (auth, seed, chat read/write, presence)
├── frontend/                         Angular 20 (standalone + signals, dark-first)
│   └── src/app/{core, shared, features}
└── docker-compose.yml                Redis
```

Dependencies point inward (`Api → Infrastructure → Application → Domain`). Application talks to persistence
only through `IAppDbContext`, so services are unit-testable against the EF in-memory provider.

## Real-time (the core)

- **Hub** — `ChatHub` (`/hubs/chat`, `[Authorize]`). On connect, a connection joins a group per channel it
  belongs to (`channel:{id}`), per server (`server:{id}`, for presence fan-out) and its own (`user:{id}`,
  for mentions). Methods: `SendMessage`, `ToggleReaction`, `StartTyping`, `MarkRead`, `WhoIsOnline`.
- **Writes then fan-out** — hub methods persist via the Application services (`ChatWriteService`,
  `ChatService`) and then broadcast typed DTOs (`MessageDto`, `ReactionStateDto`, `TypingDto`, …) to the
  relevant groups. Reaction payloads include the reacting user ids so each client derives its own “mine”.
- **JWT over WebSockets** — browsers can’t set Authorization headers on the WS handshake, so the bearer is
  read from the `access_token` query string for `/hubs` paths (`JwtBearerEvents.OnMessageReceived`).
- **Presence** — `IPresenceTracker` counts a user’s live connections. `RedisPresenceTracker` stores a SET
  of connection ids per user (`presence:{userId}`), so a user is “online” if **any** API instance holds a
  connection — correct in a scaled-out deployment. An `InMemoryPresenceTracker` is the single-instance dev
  fallback, selected automatically when Redis isn’t reachable (`TryConnectRedis`, `AbortOnConnectFail=false`).
- **Backplane** — `AddStackExchangeRedis` is wired only when Redis is reachable, so SignalR fans messages
  across instances in production while dev still works without it.

## Data model

Everything is a **Channel**: server channels have a `ServerId` + name; direct messages are channels with
`IsDirect = true`, no server and exactly two members. Unifying both keeps messaging, membership and
read-state identical for channels and DMs. `ChannelMember.LastReadMessageId/LastReadAt` drive unread counts
(messages after the cursor, excluding your own). Cascade deletes follow the hierarchy
(Server → Channel → Message → Reaction/Mention/Attachment, Channel → ChannelMember); FKs to users and the
read-cursor use `NoAction` to avoid SQL Server’s multiple-cascade-paths error.

## Auth & authorization

- Passwords hashed with ASP.NET Identity’s `PasswordHasher<User>` behind an `IPasswordHasher` adapter.
- Short-lived JWT access tokens (HMAC-SHA256, `MapInboundClaims=false` keeps `sub`).
- Refresh tokens are opaque, stored only as a SHA-256 hash; refreshing **rotates** (old revoked + linked).
- Lockout after 5 failed logins (domain logic on `User`).
- **RBAC is per-resource, not header-based:** a user can belong to several servers, so each request is
  authorized against the user’s `ServerMember`/`ChannelMember` for the resource being touched
  (membership-gated reads; role checks for structural actions).

## Demo activity simulator

`DemoActivityService` (a hosted `BackgroundService`, toggled by the `DemoActivity` flag) periodically picks
a seeded “teammate” and a public channel, broadcasts a `UserTyping` then a `ReceiveMessage` with an
**ephemeral, non-persisted** `MessageDto` via `IHubContext<ChatHub>`. The demo feels alive for a solo
visitor without polluting the database.

## Frontend

- **State** — Angular signals throughout; `AuthService` holds the session and tokens; an HTTP interceptor
  attaches the bearer and refreshes once on 401.
- **`RealtimeService`** — wraps the SignalR connection: a `state` signal, RxJS `Subject` streams per event
  (`message$`, `reaction$`, `typing$`, `presence$`, `read$`, `mention$`), invokable methods, and
  `withAutomaticReconnect`. The shell subscribes to drive live UI updates.
- **Chat UX** — message grouping by author (5-min window), typing indicator with TTL, optimistic reaction
  toggling, infinite-scroll history (cursor `before`, viewport anchored), and live unread badges.
- **Styling** — Tailwind v4, dark-first (`.dark` applied pre-paint), class-based theme toggle, EN/ES i18n.

## Testing

- **Backend (25 xUnit tests)** — auth domain (lockout, refresh rotation), deterministic seed (structure,
  idempotency, conversation + reactions, unreads + mention), chat reads (servers/unread, history paging,
  DMs, search, access control), chat writes (send + mention resolution, reaction toggle) and presence.
- **E2E (Playwright)** — auth (login, bad credentials), chat (send & see a message, switch channel) and the
  guided demo (explore panel + tour). Run with `npx playwright test` (API + Redis must be running).

## CI

`.github/workflows/ci.yml` builds & tests the backend and builds the frontend on every push / PR.

## Deferred

Cloud deployment (Azure App Service + SignalR Service / Redis + SQL) is intentionally batched for later,
with the rest of the portfolio. Everything else runs end-to-end locally.
