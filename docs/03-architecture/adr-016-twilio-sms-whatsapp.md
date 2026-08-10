# ADR-016 - Notifications Twilio SMS et WhatsApp

## Statut

Accepte pour le MVP de demonstration.

## Contexte

DIA-020 a introduit une architecture de notifications avec journal persistant et provider interchangeable. DIA-032 ajoute l'envoi reel via Twilio sans faire dependre Application ou Domain du fournisseur.

## Decision

- `Diamono.Application` conserve `INotificationService`, `INotificationProvider` et les modeles de notification.
- `Diamono.Infrastructure` fournit les providers `TwilioSmsNotificationProvider` et `TwilioWhatsAppNotificationProvider`.
- Le provider composite applique une strategie unique par evenement : WhatsApp si configure et supporte, sinon SMS, sinon provider de developpement en mode Development.
- Le modele persiste `Provider`, `ProviderMessageId`, `ErrorCode` et `ErrorMessageSafe` dans `notification_logs`.
- Les numeros sont normalises en E.164 par un service centralise, avec `+221` comme indicatif par defaut.
- Le webhook `POST /api/notifications/twilio/status` valide la signature Twilio et met a jour le statut de maniere idempotente.

## Securite

Les secrets Twilio ne sont lus que depuis l'environnement :

- `TWILIO_ACCOUNT_SID`
- `TWILIO_AUTH_TOKEN`
- `TWILIO_SMS_FROM`
- `TWILIO_WHATSAPP_FROM`

Ils ne doivent pas etre stockes dans `appsettings.json`, `render.yaml`, l'audit ou les logs. Les logs techniques masquent les numeros et ne journalisent jamais l'AuthToken.

## Limites MVP

- Pas de portail de preferences client.
- Pas de renvoi manuel depuis le backoffice.
- Pas de callback public enrichi au-dela des statuts Twilio principaux.
- Le statut `Delivered` est prepare et utilise uniquement si Twilio l'envoie.
