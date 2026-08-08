# Database migrations

Diamono utilise les migrations EF Core comme source de verite du schema.

## Strategie demo Render MVP

Pour la demonstration Render MVP, le container applique les migrations au
startup avant d'initialiser les donnees minimales :

1. `Database.MigrateAsync`
2. `SeedData.InitializeAsync`
3. seed roles Identity
4. demarrage final de l'application web

Cette strategie permet a Render de demarrer sur une base PostgreSQL vide sans
commande pre-deploy separee.

`MigrateAsync` est idempotent : si la base est deja migree, aucune migration
n'est reappliquee. `SeedData.InitializeAsync` reste aussi idempotent et ne cree
que les donnees minimales absentes.

## Echec migration

Si une migration echoue :

- l'erreur est loggee en critique;
- le seed n'est pas execute;
- l'application ne demarre pas;
- la plateforme doit etre corrigee avant un nouveau redeploiement.

La connection string ne doit jamais etre loggee.

## Production future

Pour une production multi-instance, preferer une migration controlee separee :

1. backup PostgreSQL;
2. application des migrations depuis un runner unique;
3. deploiement des instances web;
4. health gate `/health/ready`.

Cette approche evite que plusieurs instances tentent de migrer en meme temps
et permet une procedure de rollback plus lisible.

## Interdits

- Ne pas utiliser `EnsureCreated`.
- Ne pas utiliser `EnsureDeleted`.
- Ne pas modifier les migrations publiees.
- Ne pas recréer une base existante automatiquement.
