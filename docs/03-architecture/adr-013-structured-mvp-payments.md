# ADR-013 - Paiements MVP structures

## Statut
Accepte pour le MVP.

## Contexte
Le workflow DIA-011 permettait de confirmer une reservation via un changement
direct de statut. DIA-021 introduit un paiement metier tracable afin que
l'encaissement, l'audit, le reporting et les futures integrations provider ne
reposent pas uniquement sur `BookingStatus.Confirmed`.

## Decision
- Creer l'entite `Payment` avec reference unique, montant, devise `XOF`,
  methode, statut, provider et horodatages.
- Capturer le montant depuis `Booking.TotalAmount` au moment de creation du
  paiement. Aucun recalcul retroactif n'est effectue avec les parametres courants.
- Introduire `PaymentApplicationService` et `IPaymentRepository`.
- Le seul chemin MVP pour confirmer une reservation payee est :
  `Payment Pending -> Payment Paid -> Booking Confirmed`.
- Le provider MVP est `ManualPaymentProvider`; il ne depend d'aucun SDK externe.
- Les recettes encaissees du reporting proviennent maintenant des paiements
  `Paid`, tandis que les recettes potentielles restent basees sur les bookings.
- Les evenements d'audit ajoutes sont `PaymentCreated`, `PaymentPaid` et
  `PaymentFailed`.

## Consequences
- Le bouton backoffice enregistre un paiement manuel et ne modifie plus le
  statut Booking directement.
- Une contrainte PostgreSQL empeche plusieurs paiements `Paid` pour une meme
  reservation dans le MVP.
- Les remboursements, webhooks, Wave et Orange Money sont volontairement hors
  scope de cette story.
