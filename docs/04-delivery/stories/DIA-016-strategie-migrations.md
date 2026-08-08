# DIA-016 — Passer d'`EnsureCreatedAsync` aux migrations EF Core

> Story ouverte pendant la préparation de la release v0.1-demo.
> **Non traitée dans cette release** : la modification minimale n'est pas sûre (voir §2).

## 1. État constaté

`SeedData.InitializeAsync` (src/Diamono.Infrastructure/Persistence/SeedData.cs, ligne 13)
appelle `db.Database.EnsureCreatedAsync(...)`, invoqué depuis `Program.cs` (ligne 28)
au démarrage de `Diamono.Web`.

Les deux mécanismes coexistent aujourd'hui et sont **mutuellement incompatibles** :

| Scénario | Résultat |
|---|---|
| Base neuve + `EnsureCreatedAsync` | Schéma complet créé, **sans** `__EFMigrationsHistory`. Un `dotnet ef database update` ultérieur échoue (`stadium_booking_settings` existe déjà). |
| Base neuve + `MigrateAsync` seul | **Échec** : la première migration `AddBookingWorkflowFields` fait `AddColumn` sur `bookings`, table qu'aucune migration ne crée. |

## 2. Cause racine : il manque la migration initiale

Les trois migrations existantes sont **incrémentales uniquement** :

- `20260808161152_AddBookingWorkflowFields` → `AddColumn` sur `bookings`
- `20260808163408_AddBookingBlockOperations` → `AddColumn` sur `booking_blocks`
- `20260808164720_AddStadiumBookingSettings` → `CreateTable` **stadium_booking_settings** (seul `CreateTable` du dossier)

Aucune migration ne crée `resources`, `bookings` ni `booking_blocks` : ces tables ont été
produites par `EnsureCreatedAsync`, et les migrations ont été générées par-dessus cet état.

Remplacer `EnsureCreatedAsync` par `MigrateAsync` sans corriger ce trou rendrait
l'application **incapable d'initialiser un environnement neuf**. C'est pourquoi la
substitution n'a pas été faite dans la release v0.1-demo.

## 3. Correctif attendu

1. Écrire une migration `InitialCreate` **antérieure** à `20260808161152`, contenant
   `CreateTable` pour `resources`, `bookings` et `booking_blocks` conformes à l'état
   du modèle **avant** DIA-011 (colonnes et index de `DiamonoDbContext.OnModelCreating`
   d'origine, sans les champs workflow).
2. Ne supprimer ni renuméroter aucune migration existante.
3. Sur les bases déjà créées par `EnsureCreatedAsync`, marquer `InitialCreate` comme
   déjà appliquée (insertion dans `__EFMigrationsHistory`) plutôt que de la rejouer.
4. Remplacer alors `EnsureCreatedAsync` par `MigrateAsync` dans `SeedData.InitializeAsync`,
   ou mieux : sortir la migration du chemin de démarrage applicatif et l'exécuter comme
   étape de déploiement explicite.
5. Ne pas utiliser `EnsureDeletedAsync` ni recréer la base automatiquement.

## 4. État de la base de démo au moment de la release

La base locale `diamono` était exactement à l'état baseline pré-migrations (tables
`resources` / `bookings` / `booking_blocks` d'origine, 0 réservation). Les 3 migrations
y ont donc pu être appliquées telles quelles et `__EFMigrationsHistory` les recense.
Le schéma est correct et la démo fonctionne ; c'est la **reproductibilité sur un
environnement neuf** qui reste cassée tant que DIA-016 n'est pas traitée.

## 5. Point connexe corrigé dans la release

`DiamonoDbContextFactory` ciblait une base fantôme (`diamono_design_time`,
utilisateur `postgres`) qui n'existe nulle part dans le projet. Comme
`IDesignTimeDbContextFactory` est **prioritaire sur le projet de démarrage**,
`dotnet ef database update` visait cette base au lieu de `diamono`. La chaîne de
connexion a été alignée sur `docker-compose.yml` / `appsettings.json`, avec surcharge
possible via la variable d'environnement `DIAMONO_CONNECTION`.

À terme, cette fabrique devrait lire la configuration plutôt que porter une chaîne
en dur — à traiter avec DIA-016.
