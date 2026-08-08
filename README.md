# Diamono MVP v0.1

Prototype de reservation du Stade Diamono de Camberene.

## Inclus

- Accueil responsive
- Disponibilites publiques
- Formulaire de reservation
- Workflow de validation et paiement
- Blocages operationnels du terrain
- Backoffice de gestion
- Parametrage metier persiste
- PostgreSQL 17 via Docker Compose

## Prerequis

- .NET SDK 10
- Docker Desktop / Docker Engine

## Fresh Install

```bash
docker compose up -d
dotnet restore Diamono.slnx
dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
dotnet run --project src/Diamono.Web
```

Les migrations EF Core sont la source de verite du schema. L'application ne lance pas `EnsureCreatedAsync` et ne migre pas automatiquement la base au demarrage.

## Upgrade

Appliquer les migrations sur la base existante :

```bash
dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
```

Puis lancer l'application :

```bash
dotnet run --project src/Diamono.Web
```

## Base de test dediee

Pour viser une autre base sans modifier `appsettings.json`, utiliser `DIAMONO_CONNECTION` :

```bash
DIAMONO_CONNECTION="Host=localhost;Port=5432;Database=diamono_fresh_test;Username=diamono;Password=diamono_dev" dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
```

Sous PowerShell :

```powershell
$env:DIAMONO_CONNECTION="Host=localhost;Port=5432;Database=diamono_fresh_test;Username=diamono;Password=diamono_dev"
dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
```

## Donnees minimales

Les migrations et le seed idempotent creent :

- le terrain principal
- les parametres metier MVP : 08:00-23:00, 2h-6h, 25 000 / 15 000 F CFA, eclairage 5 000 F CFA apres 19:00, caution 25 000 F CFA, 60 jours d'anticipation, paiement 24h, validation obligatoire.

## Production

Ne pas executer les migrations automatiquement depuis plusieurs instances web. Les migrations doivent etre une etape de deploiement explicite, executee par une identite autorisee a modifier le schema.
