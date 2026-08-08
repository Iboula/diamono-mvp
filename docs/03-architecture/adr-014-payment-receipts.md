# ADR-014 - Recus de paiement PDF

## Statut
Accepte pour le MVP.

## Contexte
Apres DIA-021, le paiement est une entite metier tracee. Le gestionnaire ou
caissier doit pouvoir telecharger un recu professionnel et imprimable pour un
paiement `Paid`, sans generer de document depuis Razor.

## Decision
- Exposer `IPaymentReceiptService` cote Application.
- Garder les regles de generation dans `PaymentReceiptService` :
  permission `Payments.MarkPaid`, paiement obligatoirement `Paid`, audit minimal.
- Confier le rendu PDF a `IPaymentReceiptRenderer`, implemente dans
  l'Infrastructure par `QuestPdfPaymentReceiptRenderer`.
- Utiliser QuestPDF `2026.7.2`, compatible .NET 10 et Linux/Windows, pour eviter
  un moteur HTML externe ou une dependance systeme fragile.
- La reference de recu est stable et lisible :
  `RCT-{annee paiement}-{suffixe reference paiement}`. Comme la reference paiement
  est unique, la reference recu est unique par paiement.
- Le PDF contient les informations essentielles du paiement, de la reservation et
  du client, avec devise `XOF / F CFA`.

## Securite
Le telechargement passe par un endpoint backoffice protege par
`Payments.MarkPaid`. Aucun recu n'est expose publiquement. L'identifiant technique
du paiement reste dans l'URL protegee, et la reference affichee au gestionnaire
est la reference lisible du recu.

## Audit
Chaque generation ajoute `PaymentReceiptGenerated` avec `PaymentId`, `BookingId`
et `ReceiptReference`. Le contenu PDF complet n'est pas audite.

## Statut du template
Le document porte explicitement la mention :
`Document de demonstration / format provisoire`.
Ce format devra etre remplace ou valide lorsque le modele officiel mairie sera
disponible.

## Hors scope
- Facture fiscale.
- Signature electronique.
- QR code.
- Remboursement.
- Integration paiement externe.
