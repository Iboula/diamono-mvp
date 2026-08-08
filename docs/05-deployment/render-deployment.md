# Render deployment

Render doit construire le repository depuis la racine. Le Dockerfile attendu
est versionne a la racine :

```text
./Dockerfile
```

## Parametres Render

| Parametre | Valeur |
|---|---|
| Branch | `release/v0.2-demo` |
| Root Directory | vide |
| Dockerfile Path | `./Dockerfile` |
| Docker Build Context Directory | `.` |
| Health Check Path | `/health/ready` |
| Pre-Deploy Command | vide |

Le `Pre-Deploy Command` reste vide dans cette branche : pour la demo Render MVP,
le container applique `Database.MigrateAsync` au startup avant le seed. Cette
strategie permet a une base PostgreSQL vide de demarrer sans commande separee.
En production multi-instance, preferer une migration controlee separee.
Ne pas utiliser `EnsureCreated`.

## Variables d'environnement

Obligatoires :

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `DIAMONO_ENABLE_DEMO_ADMIN=true` pour la demo Render uniquement
- `DIAMONO_RESET_DEMO_ADMIN_PASSWORD=false` par defaut
- `DIAMONO_CONNECTION`
- `DIAMONO_ADMIN_PASSWORD`

`DIAMONO_CONNECTION` doit pointer vers PostgreSQL avec SSL si Render fournit
une base managée qui l'exige. Ne pas exposer PostgreSQL publiquement.

`DIAMONO_ENABLE_DEMO_ADMIN` ne doit pas etre active sur un environnement mairie
ou production reelle. Sans ce flag explicite, aucun compte demo n'est cree en
`Production`.

## Recuperer le compte admin de demo

Si `admin@diamono.local` existe deja mais que son mot de passe n'est plus aligne
avec le secret Render, effectuer un reset controle :

1. Definir temporairement :

```text
DIAMONO_ENABLE_DEMO_ADMIN=true
DIAMONO_RESET_DEMO_ADMIN_PASSWORD=true
DIAMONO_ADMIN_PASSWORD=<nouveau secret>
```

2. Redeployer une fois et verifier la connexion au backoffice.
3. Remettre :

```text
DIAMONO_RESET_DEMO_ADMIN_PASSWORD=false
```

4. Redeployer.

Ne jamais laisser ce flag actif sur une production mairie/reelle.

## Blueprint

`render.yaml` fournit un blueprint minimal pour le web service Docker. Les
secrets `DIAMONO_CONNECTION` et `DIAMONO_ADMIN_PASSWORD` sont marques
`sync: false` afin d'etre saisis dans Render sans etre commités.

## Flow de redeploiement

1. Pousser `release/v0.2-demo`.
2. Configurer Render sur cette branche.
3. Verifier que Root Directory est vide et Dockerfile Path vaut `./Dockerfile`.
4. Lancer le redeploiement Render.
5. Le startup applique les migrations puis initialise le seed minimal.
6. Attendre `/health/ready = 200`.
7. Lancer le smoke test :

```bash
scripts/smoke-demo.sh https://<render-url>
```

## Smoke manuel

- page accueil;
- disponibilites;
- reserve;
- login admin;
- backoffice;
- reservation;
- approbation;
- paiement;
- recu PDF;
- audit;
- dashboard.
