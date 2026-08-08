# PostgreSQL backup and restore

Cette procedure reste volontairement simple pour le MVP. Elle doit etre
executee avant toute migration ou deploiement significatif.

## Backup

```bash
export PGPASSWORD="<secret>"
pg_dump \
  --host "<host>" \
  --port "5432" \
  --username "diamono" \
  --format custom \
  --file "diamono-$(date +%Y%m%d-%H%M%S).dump" \
  "diamono"
unset PGPASSWORD
```

Stocker le fichier dans un emplacement chiffre et controle par acces.

## Restore

Restaurer dans une base vide :

```bash
export PGPASSWORD="<secret>"
createdb --host "<host>" --port "5432" --username "diamono" "diamono_restore_test"
pg_restore \
  --host "<host>" \
  --port "5432" \
  --username "diamono" \
  --dbname "diamono_restore_test" \
  --clean \
  --if-exists \
  "diamono-YYYYMMDD-HHMMSS.dump"
unset PGPASSWORD
```

## Test restore

Apres restauration :

1. Pointer temporairement `DIAMONO_CONNECTION` vers la base restauree.
2. Lancer l'application.
3. Verifier `/health/ready`.
4. Verifier login, `/admin`, reservations, paiements et generation de recu.
5. Detruire la base de test apres validation.

## Rotation recommandee

- Backup avant chaque deploiement.
- Backup quotidien en phase pilote.
- Conserver au minimum 7 jours glissants.
- Tester une restauration au moins une fois par cycle de demonstration.

## Migrations deployment

Procedure recommandee :

1. Faire un backup `pg_dump`.
2. Verifier le restore sur une base de test si le changement est risqué.
3. Appliquer les migrations avec `dotnet ef database update`.
4. Deployer l'application.
5. Verifier `/health/live` puis `/health/ready`.
6. Faire un smoke test fonctionnel.

Ne pas utiliser `EnsureCreated` sur une base geree par migrations.
