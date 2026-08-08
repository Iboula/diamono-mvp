# Architecture — MVP v0.1

## Choix
- .NET 10 / ASP.NET Core / Blazor Web App (Interactive Server)
- PostgreSQL 17
- EF Core 10 + Npgsql
- Radzen Blazor pour les composants de backoffice
- Modular monolith / séparation Domain, Application, Infrastructure, Web

## Pourquoi pas des microservices ?
Le produit et ses règles métier ne sont pas stabilisés. Un monolithe modulaire réduit la complexité opérationnelle et permet d'extraire des modules plus tard si une vraie contrainte l'exige.

## Modules initiaux
- Facilities
- Bookings
- Pricing
- Administration (à compléter)

## Concurrence
La vérification d'overlap applicative est présente pour le prototype. Avant production multi-instance, ajouter une protection PostgreSQL transactionnelle/exclusion constraint afin de garantir l'absence de collision même sous concurrence simultanée.
