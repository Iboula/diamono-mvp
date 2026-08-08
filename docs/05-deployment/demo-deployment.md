# Demo deployment

Objectif : publier une instance de demonstration reproductible de Diamono
Platform avec HTTPS, PostgreSQL persistant, migrations controlees et health
gate.

## Architecture cible

- `Diamono.Web` dans un container Docker publie depuis GHCR.
- PostgreSQL 17 non expose publiquement, avec volume persistant.
- Reverse proxy HTTPS en frontal, de preference Caddy pour TLS automatique.
- Health checks publics limites a `/health/live` et `/health/ready`.

## Registry

L'image de reference est :

```text
ghcr.io/iboula/diamono-mvp:<tag>
```

Tags produits par CI :

- commit SHA pour chaque push;
- `latest` sur la branche par defaut;
- tag Git pour les releases versionnees.

## Secrets GitHub requis pour CD

| Secret / variable | Type | Usage |
|---|---|---|
| `DEMO_SSH_HOST` | Secret | Hote de demo |
| `DEMO_SSH_USER` | Secret | Utilisateur SSH |
| `DEMO_SSH_PRIVATE_KEY` | Secret | Cle SSH de deploiement |
| `DEMO_DEPLOY_PATH` | Variable d'environnement GitHub | Dossier compose sur le serveur |
| `DEMO_URL` | Variable d'environnement GitHub | URL publique par defaut |
| `DIAMONO_CONNECTION` | Secret | Connexion PostgreSQL interne |
| `DIAMONO_POSTGRES_PASSWORD` | Secret | Mot de passe PostgreSQL |
| `DIAMONO_ADMIN_PASSWORD` | Secret | Mot de passe admin demo hors production reelle |
| `GHCR_USERNAME` | Secret | Identifiant lecture GHCR sur le serveur |
| `GHCR_TOKEN` | Secret | Token lecture GHCR sur le serveur |

Aucun secret ne doit etre ecrit dans le depot, les logs ou les fichiers
compose versionnes.

## Premiere installation serveur

1. Installer Docker et Docker Compose.
2. Creer le dossier cible, par exemple `/opt/diamono`.
3. Copier `docker-compose.prod-like.yml` sous `/opt/diamono/docker-compose.yml`.
4. Configurer le reverse proxy Caddy avec le domaine choisi.
5. Exporter les variables d'environnement ou utiliser un gestionnaire de secrets.
6. Lancer PostgreSQL, restaurer un backup si necessaire.
7. Appliquer les migrations.
8. Demarrer l'image Diamono.
9. Verifier `/health/live` et `/health/ready`.

## Migration flow

Avant chaque deploiement :

1. Backup PostgreSQL avec `pg_dump`.
2. Verifier que le backup est lisible.
3. Appliquer les migrations avec `dotnet ef database update` depuis un runner
   disposant du SDK et de `DIAMONO_CONNECTION`.
4. Deployer l'image.
5. Attendre `/health/ready = 200`.
6. Lancer `scripts/smoke-demo.sh`.

Ne jamais utiliser `EnsureCreated`.

## Health gate

Le deploiement est valide uniquement si :

```bash
curl -fsS https://demo.stadediamono.sn/health/live
curl -fsS https://demo.stadediamono.sn/health/ready
```

`/health/ready` doit retourner HTTP 200.

## Smoke test automatique

```bash
scripts/smoke-demo.sh https://demo.stadediamono.sn
```

Ou sous Windows :

```powershell
./scripts/smoke-demo.ps1 -BaseUrl https://demo.stadediamono.sn
```

Pages verifiees :

- `/`
- `/disponibilites`
- `/reserve`
- `/login`
- `/health/live`
- `/health/ready`

## Smoke test manuel staging

1. Se connecter en administrateur.
2. Creer une reservation publique.
3. L'approuver dans le backoffice.
4. Marquer le paiement comme paye.
5. Telecharger le recu PDF.
6. Verifier l'audit.
7. Verifier le dashboard.

## Demo data

Un jeu de donnees de demonstration peut etre ajoute plus tard derriere
`DIAMONO_SEED_DEMO=true`. Il ne doit jamais etre active par defaut et ne doit
jamais polluer une production reelle.

Donnees possibles :

- reservations sur plusieurs statuts;
- un blocage operationnel;
- categories client variees;
- paiements payes et en attente.

## Domaine

Exemple documentaire : `demo.stadediamono.sn`.

Le domaine n'est pas hardcode dans le code. Il doit etre configure dans DNS,
Caddy et les variables de deploiement.
