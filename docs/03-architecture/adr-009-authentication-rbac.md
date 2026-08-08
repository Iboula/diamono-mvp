# ADR-009 — Authentification et autorisation par permissions du backoffice

## Statut

Accepté pour DIA-016B.

## Contexte

`/admin` et `/admin/parametres` étaient accessibles sans authentification : n'importe
quel visiteur pouvait approuver une réservation, encaisser un paiement, bloquer le
terrain ou modifier la grille tarifaire.

Le projet ne possédait **aucune intégration IAM** : aucune référence à Zitadel, à
OpenID Connect ou à un fournisseur externe. Conformément à la story, aucun IAM n'est
introduit ici.

## Décision

### Pourquoi ASP.NET Core Identity pour le MVP

- Aucun IAM n'est déjà configuré, et en introduire un ferait porter au MVP le coût
  d'exploitation d'un service supplémentaire — contraire à la règle « pas de
  microservice dans le MVP » (AGENTS.md).
- Identity est fourni par la plateforme déjà retenue (ASP.NET Core / EF Core /
  PostgreSQL). Aucune technologie nouvelle n'est ajoutée, seulement un paquet.
- Le stockage des comptes se fait dans la base existante, via une migration
  incrémentale, sans service tiers à déployer pour la démonstration à la mairie.

### Ce qui rend le remplacement par OIDC possible plus tard

Le point clé n'est pas le fournisseur d'identité mais **où vit la règle d'accès** :

1. Les composants et les cas d'usage ne connaissent que des **permissions**, jamais
   des rôles. Il n'existe aucun test `if (role == "SuperAdmin")` dans le code.
2. Les rôles sont traduits en permissions par un `IClaimsTransformation`
   (`PermissionClaimsTransformation`), pas par Identity. Cette transformation
   s'applique à n'importe quel `ClaimsPrincipal` authentifié — cookie Identity
   aujourd'hui, jeton OIDC demain — et respecte le `RoleClaimType` porté par
   l'identité, ce qui couvre les fournisseurs qui émettent les rôles sur un autre
   type de claim (`roles`, par exemple).
3. Les cas d'usage dépendent de `IPermissionGuard`, une abstraction de la couche
   Application qui ignore totalement le mécanisme d'authentification.

Migrer vers Zitadel reviendra donc à remplacer `AddDiamonoIdentity` par la
configuration OIDC et à mapper les rôles du fournisseur. La matrice, les policies,
les écrans et les services applicatifs restent inchangés.

## Rôles

| Rôle | Usage visé |
|---|---|
| `SuperAdmin` | Direction / administration complète |
| `Gestionnaire` | Exploitation quotidienne du terrain |
| `Caissier` | Encaissement uniquement |
| `Lecteur` | Consultation seule |

## Permissions

`Bookings.View`, `Bookings.Approve`, `Bookings.Reject`, `Payments.MarkPaid`,
`BookingBlocks.View`, `BookingBlocks.Manage`, `Settings.View`, `Settings.Manage`,
`Administration.Manage`.

Chaque permission donne lieu à une policy ASP.NET Core homonyme, exigeant un
utilisateur authentifié portant le claim `diamono.permission` correspondant.

## Matrice

| Permission | SuperAdmin | Gestionnaire | Caissier | Lecteur |
|---|:---:|:---:|:---:|:---:|
| `Bookings.View` | ✅ | ✅ | ✅ | ✅ |
| `Bookings.Approve` | ✅ | ✅ | — | — |
| `Bookings.Reject` | ✅ | ✅ | — | — |
| `Payments.MarkPaid` | ✅ | — | ✅ | — |
| `BookingBlocks.View` | ✅ | ✅ | — | ✅ |
| `BookingBlocks.Manage` | ✅ | ✅ | — | — |
| `Settings.View` | ✅ | ✅ | — | ✅ |
| `Settings.Manage` | ✅ | — | — | — |
| `Administration.Manage` | ✅ | — | — | — |

Source de vérité unique : `Diamono.Domain.Security.PermissionMatrix`.

### Hypothèses assumées

1. **Annulation d'une réservation par le backoffice → `Bookings.Reject`.** La matrice
   DIA-016B ne prévoit pas de permission d'annulation. Plutôt que d'inventer une
   dixième permission, l'annulation est rattachée au refus, décision de même nature.
   Conséquence : un Caissier voit « Marquer payé » mais plus « Annuler ».
2. **`Administration.Manage` n'est consommée par aucun écran** dans cette story. La
   policy est déclarée pour que l'administration des comptes et des rôles (DIA-017+)
   s'y branche sans redéfinir la matrice.

## Défense en profondeur

Masquer un bouton n'est pas une sécurité. Trois barrières indépendantes :

1. **Route** — `@attribute [Authorize(Policy = ...)]` sur `/admin` (`Bookings.View`)
   et `/admin/parametres` (`Settings.View`). `AuthorizeRouteView` redirige les
   anonymes vers `/login?returnUrl=…` et les authentifiés sans droit vers
   `/acces-refuse`.
2. **Cas d'usage** — chaque méthode sensible de `BookingApplicationService`,
   `BookingBlockApplicationService` et `StadiumSettingsApplicationService` appelle
   `IPermissionGuard.EnsurePermissionAsync` et lève `PermissionDeniedException`.
   C'est la barrière qui compte : elle vaut même si l'interface est contournée.
3. **Interface** — les actions non autorisées ne sont pas rendues, pour éviter de
   proposer une opération qui échouerait.

Le site public (`/`, `/disponibilites`, `/reserve`) reste anonyme : ni la lecture des
disponibilités ni la création d'une demande ne passent par une permission.

## Gestion des secrets

- Aucun mot de passe n'est écrit dans le dépôt.
- Le compte de démonstration `admin@diamono.local` (rôle `SuperAdmin`) n'est créé
  **qu'en dehors de la production**, et uniquement si `DIAMONO_ADMIN_PASSWORD` est
  fourni par la configuration ou l'environnement.
- Sans valeur fournie, le compte n'est pas créé et la procédure locale est
  journalisée. Aucun secret n'est inventé silencieusement.
- Les mots de passe sont stockés hachés par Identity (`PasswordHasher`), jamais en
  clair, et ne sont jamais journalisés.

Procédure locale recommandée :

```bash
dotnet user-secrets init --project src/Diamono.Web
dotnet user-secrets set DIAMONO_ADMIN_PASSWORD "<mot-de-passe-local>" --project src/Diamono.Web
```

Politique de mot de passe : 12 caractères minimum, majuscule, minuscule, chiffre et
caractère non alphanumérique; verrouillage 15 minutes après 5 échecs.

## Cookie de session

`diamono.auth` : `HttpOnly`, `SameSite=Lax`, expiration glissante de 8 h.
`SecurePolicy` vaut `Always` hors développement et `SameAsRequest` en développement,
pour que `http://localhost` reste utilisable sans certificat. Les protections
antiforgery de Blazor sont conservées : `/login` et `/logout` sont des pages en rendu
statique serveur, ce qui laisse `EditForm` émettre nativement le jeton antiforgery.

## Conséquences

- `DiamonoDbContext` hérite désormais de `IdentityDbContext` : les tables
  `identity_*` vivent dans la même base et la même chaîne de migrations.
- `AddIdentity` est enregistré côté Web, car il appartient au framework ASP.NET Core;
  `Diamono.Infrastructure` reste une bibliothèque de classes sans dépendance au
  pipeline HTTP.
- `ICurrentUserAccessor` expose l'identité de l'opérateur courant afin que DIA-017
  puisse renseigner `ApprovedBy`, `RejectedBy`, `PaidBy`, `CreatedBy`, `CancelledBy`.
  Aucune colonne d'audit n'est écrite dans cette story.
