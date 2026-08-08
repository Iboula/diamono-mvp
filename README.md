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
- Authentification du backoffice et autorisations par permissions
- Administration des comptes backoffice
- Audit metier append-only
- Dashboard direction avec KPIs et graphiques
- PostgreSQL 17 via Docker Compose

## Prerequis

- .NET SDK 10
- Docker Desktop / Docker Engine

## Fresh Install

```bash
docker compose up -d
dotnet restore Diamono.slnx
dotnet ef database update --project src/Diamono.Infrastructure --startup-project src/Diamono.Web
dotnet user-secrets set DIAMONO_ADMIN_PASSWORD "<mot-de-passe-local>" --project src/Diamono.Web
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

## Backoffice : authentification et roles

Le site public (`/`, `/disponibilites`, `/reserve`) reste anonyme. `/admin`,
`/admin/parametres` et `/admin/utilisateurs` exigent une authentification : un
visiteur anonyme est redirige vers `/login`, un utilisateur connecte sans la
permission requise vers `/acces-refuse`.

Roles et permissions (detail et matrice complete : `docs/03-architecture/adr-009-authentication-rbac.md`) :

| Role | Peut |
|---|---|
| `SuperAdmin` | tout |
| `Gestionnaire` | consulter, approuver, refuser, gerer les blocages, lire les parametres |
| `Caissier` | consulter les reservations, marquer paye |
| `Lecteur` | consultation seule (reservations, blocages, parametres) |

Les quatre roles sont crees automatiquement au demarrage.

### Premier SuperAdmin

Hors production uniquement, l'application cree `admin@diamono.local` avec le role
`SuperAdmin`. **Aucun mot de passe n'est stocke dans le depot** : il doit etre fourni
par la configuration `DIAMONO_ADMIN_PASSWORD`.

```bash
dotnet user-secrets init --project src/Diamono.Web
dotnet user-secrets set DIAMONO_ADMIN_PASSWORD "<mot-de-passe-local>" --project src/Diamono.Web
```

Sous PowerShell, en variable d'environnement pour une session :

```powershell
$env:DIAMONO_ADMIN_PASSWORD="<mot-de-passe-local>"
dotnet run --project src/Diamono.Web
```

Sans valeur fournie, le compte n'est **pas** cree : l'application journalise la
procedure a suivre plutot que d'inventer un secret. Le mot de passe doit faire au
moins 12 caracteres et contenir majuscule, minuscule, chiffre et caractere non
alphanumerique.

### Comptes suivants

Les comptes operateurs se gerent depuis `/admin/utilisateurs`, reserve a la policy
`Administration.Manage`.

Le formulaire simple permet de creer uniquement :

- `Gestionnaire`
- `Caissier`
- `Lecteur`

La creation d'un autre `SuperAdmin` n'est pas exposee dans l'interface MVP. A la
creation, le serveur genere un mot de passe temporaire conforme a la politique
Identity et l'affiche une seule fois dans le backoffice. Il n'est jamais journalise
et n'est pas stocke en clair.

La desactivation ne supprime pas le compte : elle utilise le verrouillage Identity
et met a jour le `SecurityStamp` pour provoquer une reevaluation rapide des sessions.
La reactivation leve ce verrouillage.

### Recuperation de mot de passe MVP

Il n'existe pas encore de parcours autonome "mot de passe oublie". Pour le MVP, un
SuperAdmin doit creer un nouveau compte ou passer par une operation admin controlee
hors interface. Un flux de reset par token email reste une dette fonctionnelle.

### Audit metier

Le journal d'audit est disponible sur `/admin/audit`, reserve a
`Administration.Manage`. Il trace les actions sensibles du backoffice
(reservations, blocages, parametres, utilisateurs) dans `audit_logs`. Ce journal est
append-only cote application : aucune modification ou suppression n'est exposee.

### Dashboard reporting

`/admin` est le dashboard direction. La gestion operationnelle des reservations reste
sur `/admin/reservations`. Les conventions de recettes et de taux d'occupation sont
documentees dans `docs/03-architecture/adr-011-reporting-dashboard.md`.

## Production

Ne definir `DIAMONO_ADMIN_PASSWORD` sur aucun environnement de production : le compte
de demonstration n'y est de toute facon jamais cree. Creer le premier SuperAdmin par
un moyen maitrise, puis gerer les comptes suivants via `/admin/utilisateurs`.

Ne pas executer les migrations automatiquement depuis plusieurs instances web. Les migrations doivent etre une etape de deploiement explicite, executee par une identite autorisee a modifier le schema.
