# Twilio SMS et WhatsApp

## Variables Render

Configurer les secrets dans Render, jamais en clair dans le depot :

- `DIAMONO_NOTIFICATION_MODE=Demo` ou `Production`
- `TWILIO_ACCOUNT_SID`
- `TWILIO_AUTH_TOKEN`
- `TWILIO_SMS_FROM`
- `TWILIO_WHATSAPP_FROM`

Pour WhatsApp, le champ `TWILIO_WHATSAPP_FROM` doit utiliser le format Twilio, par exemple :

```text
whatsapp:+XXXXXXXXXXX
```

## Modes

- `Development` : sans credentials Twilio, le provider de developpement persiste une notification envoyee sans fournisseur externe.
- `Demo` : utilise Twilio si les credentials sont presents.
- `Production` : si Twilio est active mais mal configure, l'envoi echoue proprement et la notification passe `Failed`.

## Numeros E.164

Les numeros internes sont normalises en E.164. Pour le Senegal, `771234567` devient `+221771234567`. Un numero deja international, par exemple `+14155550100`, reste inchange.

## WhatsApp sandbox

Pour une demo WhatsApp Twilio :

1. Activer le sandbox WhatsApp dans la console Twilio.
2. Configurer `TWILIO_WHATSAPP_FROM` avec le numero sandbox fourni par Twilio.
3. Faire rejoindre le sandbox au telephone de test selon l'instruction Twilio.
4. Declarer l'URL de callback :

```text
https://<domaine-render>/api/notifications/twilio/status
```

Aucun numero sandbox n'est code en dur.

## Callback statut

Twilio appelle `POST /api/notifications/twilio/status`. L'application valide `X-Twilio-Signature` avec `TWILIO_AUTH_TOKEN`, puis mappe :

- `queued` -> `Pending`
- `sent` -> `Sent`
- `delivered` -> `Delivered`
- `failed` / `undelivered` -> `Failed`

Le traitement est idempotent.

## Securite

Ne jamais exposer :

- `TWILIO_AUTH_TOKEN`
- signature entrante
- tokens fournisseur

Les numeros peuvent apparaitre dans l'historique backoffice autorise, mais les logs techniques les masquent.

## Limites MVP

Le MVP n'integre pas encore de compte WhatsApp Business officiel, de templates WhatsApp approuves, ni de preferences client par utilisateur.
