# Release process

## Branches

1. Developper sur branche feature.
2. Ouvrir PR vers la branche de stabilisation.
3. Merger apres CI verte.
4. Tagger une version.

## Tags

Tags recommandes :

- `vX.Y.Z` pour une image versionnee.
- `demo-YYYYMMDD` pour declencher un deploiement demo.

## Gates

Avant demo :

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet list package --vulnerable --include-transitive
docker build -t diamono-web:release-check .
```

Apres deploiement :

```bash
scripts/smoke-demo.sh https://demo.stadediamono.sn
```

## Validation manuelle

- login admin;
- reservation;
- approbation;
- paiement;
- recu PDF;
- audit;
- dashboard.

## Rollback

Voir `docs/05-deployment/rollback.md`.
