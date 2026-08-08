# Configuration and secrets

Diamono doit etre configure par variables d'environnement en environnement
de demo ou production. Aucun mot de passe, token, cookie ou chaine de
connexion complete ne doit etre versionne dans `appsettings*.json`.

## Variables requises

| Variable | Usage | Exemple non secret |
|---|---|---|
| `DIAMONO_CONNECTION` | Connexion PostgreSQL EF Core | `Host=db;Port=5432;Database=diamono;Username=diamono;Password=<secret>` |
| `DIAMONO_ADMIN_PASSWORD` | Ancien secret admin demo, ignore par le hotfix DIA-025F | `<secret>` |
| `DIAMONO_ENABLE_DEMO_ADMIN` | Autorise explicitement le compte admin demo temporaire en Production demo | `true` uniquement pour Render demo |
| `ASPNETCORE_ENVIRONMENT` | Environnement ASP.NET Core | `Production` |
| `ASPNETCORE_URLS` | URL interne ecoutee par le container | `http://+:8080` |

En `Production`, le seed du compte de demonstration n'est execute que si
`DIAMONO_ENABLE_DEMO_ADMIN=true`. Hotfix DIA-025F : pour la demo MVP uniquement,
ce flag garantit temporairement le compte `admin@diamono.local` avec le mot de
passe connu de demonstration. Ce mecanisme doit etre supprime avant une
production mairie.

## Placeholders futurs

Ces variables sont reservees pour les prochaines integrations. Elles ne
doivent pas etre definies avec des valeurs reelles dans le depot.

| Variable | Usage futur |
|---|---|
| `DIAMONO_SMTP_HOST` | Provider e-mail SMTP |
| `DIAMONO_SMTP_USERNAME` | Identifiant SMTP |
| `DIAMONO_SMTP_PASSWORD` | Secret SMTP |
| `DIAMONO_SMS_PROVIDER` | Provider SMS |
| `DIAMONO_SMS_API_KEY` | Secret provider SMS |
| `DIAMONO_PAYMENT_PROVIDER` | Provider paiement |
| `DIAMONO_PAYMENT_API_KEY` | Secret provider paiement |
| `DIAMONO_SEED_DEMO` | Seed de donnees demo optionnel, jamais active par defaut |

## Regles d'exploitation

- Injecter les secrets via le gestionnaire de secrets de l'hebergeur, pas via Git.
- Ne jamais logger `DIAMONO_CONNECTION` en entier.
- Ne jamais logger mot de passe, token, cookie ou cle provider.
- Changer tout secret expose avant une demonstration publique.
- Verifier que `ASPNETCORE_ENVIRONMENT=Production` est positionne pour un deploiement production-like.
- Ne pas activer `DIAMONO_SEED_DEMO=true` sur une production reelle.
- Ne pas activer `DIAMONO_ENABLE_DEMO_ADMIN=true` sur une production mairie/reelle.

## Configuration locale

Pour le developpement local, utiliser `dotnet user-secrets` ou une variable
d'environnement de session :

```powershell
$env:DIAMONO_CONNECTION = "Host=localhost;Port=5432;Database=diamono;Username=diamono;Password=<secret>"
$env:DIAMONO_ADMIN_PASSWORD = "<mot-de-passe-local>"
```
