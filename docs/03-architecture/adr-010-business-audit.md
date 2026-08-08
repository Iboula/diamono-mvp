# ADR-010 - Audit metier immuable

## Statut

Accepte pour DIA-018.

## Contexte

Les logs techniques servent a diagnostiquer l'application, mais ils ne repondent pas
a une question metier simple : qui a fait quoi, sur quel objet, et quand. Le
backoffice permet maintenant d'approuver des reservations, bloquer le terrain,
modifier les tarifs et gerer les comptes. Ces actions doivent etre tracables sans
dependre de Serilog ou de la console.

## Decision

Un journal metier dedie est ajoute via `AuditEntry` et la table `audit_logs`.
L'audit est append-only cote application : aucun cas d'usage `UpdateAudit` ou
`DeleteAudit` n'est expose, et le backoffice ne propose aucun bouton de suppression.

Les services applicatifs ecrivent l'audit uniquement apres une action metier
reussie. Une action refusee par permission ou par invariant ne produit pas une entree
de succes.

## Evenements traces

- `BookingApproved`
- `BookingRejected`
- `BookingMarkedPaid`
- `BookingCancelled`
- `BookingBlockCreated`
- `BookingBlockCancelled`
- `SettingsUpdated`
- `UserCreated`
- `UserRoleChanged`
- `UserDisabled`
- `UserEnabled`

## Donnees stockees

Chaque entree conserve :

- date/heure UTC
- utilisateur courant (`UserId`, `UserEmail`)
- action
- type et identifiant d'objet
- description lisible
- anciennes valeurs JSON si utile
- nouvelles valeurs JSON si utile
- metadonnees JSON si utile

Pour `SettingsUpdated`, l'ancienne et la nouvelle configuration sont capturees. Pour
`UserRoleChanged`, l'ancien et le nouveau role sont captures. Pour le workflow
`Booking`, l'ancien et le nouveau statut sont captures.

## Donnees interdites

Ne jamais serialiser :

- mot de passe temporaire
- `PasswordHash`
- token
- secret
- cookie
- `SecurityStamp`

Les evenements utilisateurs ne stockent que l'email, le nom affiche, le role et
l'etat actif/desactive.

## Lecture backoffice

La page `/admin/audit` utilise `IAuditReader` et exige la policy
`Administration.Manage`. Les roles `Gestionnaire`, `Caissier` et `Lecteur` n'ont pas
acces au journal dans cette story.

## Limites transactionnelles MVP

Pour les reservations, blocages et settings, l'entree d'audit est ajoutee au meme
`DbContext` avant le `SaveChanges` metier : la mutation et l'audit sont donc sauves
ensemble.

Pour Identity, `UserManager` persiste certaines operations immediatement. L'audit est
ecrit juste apres l'operation reussie et sauvegarde dans la meme base. C'est une
unite logique suffisante pour le MVP, mais pas une transaction unique couvrant chaque
appel interne de `UserManager`.

## Alternatives refusees

- Pas de Kafka ou bus d'evenements.
- Pas de CQRS/event sourcing.
- Pas d'audit de toutes les lectures.
- Pas d'exposition directe de la table aux roles operationnels.
