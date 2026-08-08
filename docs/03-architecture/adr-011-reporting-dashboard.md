# ADR-011 - Reporting et dashboard direction

## Statut

Accepte pour DIA-019.

## Contexte

Le backoffice operationnel existe deja pour gerer les reservations, les blocages,
les parametres, les utilisateurs et l'audit. La direction du stade a aussi besoin
d'une vue rapide pour piloter l'activite : volume de demandes, paiements, recettes
et occupation du terrain.

## Decision

`/admin` devient le dashboard d'entree du backoffice. La gestion detaillee des
reservations est conservee sur `/admin/reservations`.

Le dashboard est alimente par `ReportingApplicationService`, qui depend d'un
`IReportingRepository` read-only. Razor ne calcule pas les agregats metier.

## Permissions

Le dashboard exige `Bookings.View`, comme la lecture backoffice existante.
`Administration.Manage` n'est pas requis pour consulter les KPIs. Les raccourcis vers
parametres, utilisateurs et audit restent affiches selon les permissions existantes.

## Definitions KPI

### Aujourd'hui

- Reservations du jour : reservations dont le creneau intersecte la journee UTC du
  jour courant.
- Creneaux occupes : intervalles occupes fusionnes pour aujourd'hui, apres clipping
  aux horaires ouvrables.
- Creneaux disponibles : nombre approximatif de tranches horaires ouvrables non
  occupees aujourd'hui.
- Demandes en attente : reservations `PendingApproval` dans la fenetre chargee.
- Paiements en attente : reservations `AwaitingPayment` dans la fenetre chargee.

### Periode selectionnee

- Nombre total de reservations : reservations intersectant la periode.
- Reservations confirmees : statut `Confirmed`.
- Annulations : statut `Cancelled`.
- No-show : statut `NoShow` si disponible dans les donnees.
- Recettes encaissees : somme des montants des reservations `Confirmed`.
- Recettes potentielles : somme des montants `PendingApproval`, `AwaitingPayment` et
  `Confirmed`.
- Taux d'occupation : duree occupee fusionnee / duree theorique ouvrable.

## Taux d'occupation

Les intervalles occupes incluent :

- reservations `PendingApproval`, `AwaitingPayment`, `Confirmed`
- `BookingBlocks` actifs

Les overlaps sont fusionnes avant calcul, pour ne pas compter deux fois une meme
plage. Les intervalles sont limites aux horaires configures du stade.

## Conventions de dates

Les dates sont manipulees en `DateOnly` cote Application et converties en bornes UTC
pour les requetes. Le MVP reste aligne sur l'hypothese Dakar/UTC deja utilisee par le
prototype.

## Performance

Le repository reporting filtre les bookings et blocks par fenetre temporelle avec
`AsNoTracking` et projection directe vers des read models. Deux index incrementaux
sont ajoutes pour les requetes reporting :

- `bookings(StartsAt, EndsAt)`
- `booking_blocks(StartsAt, EndsAt)`

Le service Application recoit un jeu de donnees read-only et calcule les courbes sans
N+1.

## Limites MVP

- Pas de data warehouse, Power BI, event sourcing, IA generative ou microservice.
- Les graphiques sont calcules a la demande.
- Les "insights" sont deterministes et simples.
- Le calcul des creneaux disponibles reste horaire, pas un moteur de planning fin a
  la minute.
