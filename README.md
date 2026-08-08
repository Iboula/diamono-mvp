# Diamono MVP v0.1

Prototype de réservation du Stade Diamono de Cambérène.

## Ce qui est déjà inclus
- Homepage responsive utilisant des photos réelles fournies pour le prototype
- Formulaire de réservation
- Calcul tarif standard / ASC / éclairage / caution
- Création de demande `PendingApproval`
- Détection d'overlap applicative
- Backoffice de démonstration listant les demandes
- PostgreSQL 17 via Docker Compose
- Documentation BMAD légère (brief, règles, architecture, stories)

## Prérequis
- .NET SDK 10
- Docker Desktop / Docker Engine

## Lancer
```bash
docker compose up -d
dotnet restore Diamono.slnx
dotnet run --project src/Diamono.Web
```

Le schéma est créé automatiquement pour accélérer la démo (`EnsureCreated`). Avant production, remplacer ce mécanisme par des migrations EF Core versionnées.

## Important
Les tarifs et horaires sont des hypothèses MVP. Ils ne constituent pas des tarifs officiels du Stade Diamono.

## Limite connue volontaire
La protection contre les doubles réservations est aujourd'hui applicative. Avant déploiement avec plusieurs pods/instances, implémenter une contrainte PostgreSQL de non-chevauchement et le workflow transactionnel correspondant.
