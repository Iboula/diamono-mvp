# Règles métier MVP — baseline v0.1

> Ces règles sont inventées pour la démonstration et doivent être validées par la direction/mairie avant mise en production.

| ID | Règle |
|---|---|
| BR-001 | Ouverture 08:00–23:00. |
| BR-002 | Durée minimale 2 h, maximale 6 h. |
| BR-003 | Tarif standard : 25 000 F CFA/h. |
| BR-004 | ASC de Cambérène : 15 000 F CFA/h. |
| BR-005 | Éclairage après 19:00 : +5 000 F CFA/h. |
| BR-006 | Caution : 25 000 F CFA. |
| BR-007 | Toute demande commence à `PendingApproval`. |
| BR-008 | Une réservation en `PendingApproval`, `AwaitingPayment` ou `Confirmed` bloque le créneau. |
| BR-009 | Paiement après approbation; délai cible 24 h. |
| BR-010 | Réservation au maximum 60 jours à l'avance (à implémenter au sprint suivant). |
| BR-011 | Un blocage maintenance/événement interdit les réservations qui se chevauchent. |
| BR-012 | Un blocage operationnel ne peut pas chevaucher une reservation `PendingApproval`, `AwaitingPayment` ou `Confirmed`. |
