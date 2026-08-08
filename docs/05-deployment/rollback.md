# Rollback

Le rollback de demo doit rester simple et explicite.

## Rollback applicatif

1. Identifier l'image precedente connue comme stable.
2. Exporter `DIAMONO_IMAGE=ghcr.io/iboula/diamono-mvp:<previous-tag>`.
3. Relancer `docker compose up -d`.
4. Verifier `/health/live`.
5. Verifier `/health/ready`.
6. Lancer le smoke test.

## Rollback base de donnees

Si une migration a modifie le schema ou les donnees :

1. Stopper l'application.
2. Restaurer le backup PostgreSQL pris avant deploiement.
3. Redemarrer l'image compatible avec ce backup.
4. Verifier `/health/ready`.
5. Faire un smoke test manuel.

## Attention migrations

Toutes les migrations ne sont pas retrocompatibles. Quand une migration
supprime ou transforme des donnees, le rollback fiable est le restore du backup
pre-deploiement.

## Decision

Pour la demo, privilegier :

- rollback image si aucune migration destructive;
- restore backup si migration non retrocompatible;
- communication claire si des donnees demo creees apres le backup sont perdues.
