# ADR-012 - Notifications metier

## Statut
Accepte pour le MVP.

## Contexte
Le workflow de reservation declenche des moments ou le demandeur doit etre
informe : demande recue, approbation avec paiement attendu, refus, paiement
enregistre et annulation. DIA-020 ne doit pas integrer de fournisseur SMS ou
email reel, mais doit preparer une architecture interchangeable et conserver un
historique consultable.

## Decision
- Le Domain expose `NotificationLog`, `NotificationChannel`, `NotificationTemplate`
  et `NotificationStatus`.
- L'Application expose `INotificationService` pour les cas d'usage, ainsi que
  `NotificationRequest`, `NotificationQuery` et `INotificationReader`.
- L'Infrastructure fournit `DevelopmentNotificationService`, qui persiste le log
  via EF Core, et `INotificationProvider`, dont l'implementation MVP
  `DevelopmentNotificationProvider` simule une livraison reussie sans appeler un
  fournisseur externe.
- Les notifications de reservation sont creees dans `BookingApplicationService`
  uniquement apres succes de la transition metier et avant le `SaveChanges`
  final, afin que reservation, audit et notification soient valides ensemble.
- Le canal MVP utilise pour les reservations est `Sms`, car le modele de
  reservation collecte le telephone. Le canal `Email` est disponible pour une
  future evolution du formulaire.
- Les templates sont en francais simple. Le delai de paiement vient de
  `StadiumBookingSettings.PaymentDeadlineHours`.
- L'historique est expose dans `/admin/notifications`, protege par
  `Administration.Manage`.

## Securite
Les metadonnees journalisees contiennent des informations metier minimales :
reference, statut, horaires, montant et motif le cas echeant. Aucun token,
mot de passe, cle provider ou secret technique n'est inclus dans `MetadataJson`.

## Consequences
- Remplacer le provider de developpement par Orange SMS, Twilio, SMTP ou
  SendGrid ne doit pas modifier les cas d'usage metier.
- Les rappels `BookingPaymentReminder` et `BookingUpcomingReminder` sont modelises
  mais non planifies automatiquement dans cette story.
- La table `notification_logs` conserve l'historique meme si la livraison MVP ne
  fait aucun envoi externe.

## Dette restante
- Ajouter un vrai canal email si l'adresse email devient collectee.
- Ajouter un ordonnanceur pour les rappels de paiement et de reservation.
- Ajouter une politique de retention/masquage selon les exigences RGPD locales.
- Reporter cette decision dans `diamono-docs` quand la synchronisation documentaire
  sera disponible.
