# ADR-008 - EF Core migrations as database source of truth

## Status

Accepted for v0.1-demo stabilization.

## Context

The application originally used `EnsureCreatedAsync` during startup to create the PostgreSQL schema quickly for the prototype. Later stories added EF Core migrations on top of a database that already existed.

That left a reproducibility gap: a fresh PostgreSQL database could not be initialized by migrations alone because the first migration expected `resources`, `bookings`, and `booking_blocks` to already exist.

## Decision

EF Core migrations are the source of truth for the database schema.

The application does not run `EnsureCreatedAsync`. For the Render MVP demo,
the web container runs `MigrateAsync` at startup before seed data. For future
multi-instance production, a separate controlled migration step remains
preferred:

```bash
dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
```

This avoids production startup races and avoids requiring the web runtime principal to have schema migration privileges.

## Fresh Install

Start PostgreSQL:

```bash
docker compose up -d
```

Apply migrations:

```bash
dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
```

Run the app:

```bash
dotnet run --project src/Diamono.Web
```

## Upgrade

Back up production data first. Then apply migrations with the same command:

```bash
dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
```

Existing v0.1-demo databases that already have migration history continue forward normally. Databases created manually before migration history may need a one-time baseline reconciliation before applying future migrations.

## Seed Strategy

Migrations create and seed the minimum baseline data needed by a fresh database:

- `resources` with the main pitch `7f099973-c811-4afe-beca-c9a1b8fcd001`
- `stadium_booking_settings` with MVP values

`SeedData.InitializeAsync` remains idempotent and only ensures minimum application data after the schema exists. It does not create schema.

## Production Policy

Do not run migrations automatically from web app startup in production. Execute migrations as a deployment step with an operator or CI/CD identity allowed to change schema.

Do not use `EnsureDeletedAsync`. Do not mix `EnsureCreatedAsync` with migrations.
