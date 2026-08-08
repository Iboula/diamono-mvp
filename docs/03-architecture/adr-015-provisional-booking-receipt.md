# ADR-015 - Recu provisoire de demande de reservation

## Contexte

Le demandeur doit obtenir un document imprimable immediatement apres la creation
d'une demande, avant validation administrative et avant paiement.

## Decision

- Exposer `IBookingRequestReceiptService` cote Application.
- Confier le rendu PDF a `IBookingRequestReceiptRenderer`, implemente par
  `QuestPdfBookingRequestReceiptRenderer` dans l'Infrastructure.
- Generer le PDF en memoire avec QuestPDF, format A4, sans ecriture disque.
- Ajouter un token public aleatoire sur `Booking` pour l'URL publique :
  `/reservation/recu-provisoire/{token}`.
- Ajouter un endpoint backoffice protege :
  `/admin/reservations/{bookingId}/recu-provisoire`.

## Recu provisoire vs recu officiel

Le recu provisoire confirme uniquement la reception de la demande. Il affiche le
statut de demande et la mention obligatoire indiquant qu'il ne constitue pas une
reservation confirmee. Il ne remplace jamais le recu de paiement DIA-022, dont la
reference reste `RCT-*` et qui n'est disponible que pour un paiement `Paid`.

La reference du recu provisoire est deterministe a partir de la reservation :
`RPD-YYYY-xxxxx`, en reutilisant le suffixe de `DIA-YYYY-xxxxx`.

## Securite URL publique

Le Guid de reservation n'est pas expose publiquement. Le token est genere par
`RandomNumberGenerator`, non sequentiel, unique en base, et ne donne acces qu'au
PDF provisoire de la reservation associee. Les tokens invalides retournent un
404 generique.

Le token ne doit pas etre journalise. L'audit public enregistre seulement le mode
`PublicToken`, la reservation et la reference du document.

## Donnees et montants

Le PDF affiche les donnees enregistrees sur `Booking` : client, telephone,
categorie, creneau, activite, montant location, eclairage, caution et total. Les
montants ne sont jamais recalcules avec les parametres courants du stade; ils
representent l'estimation historique de la demande.

## Limites MVP

- Pas de compte citoyen.
- Pas d'envoi email/SMS automatique du PDF.
- Pas de QR code.
- Pas de facture.
- Pas de stockage persistant de PDF sur Render.
