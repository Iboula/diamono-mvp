# CI/CD

## CI

Workflow : `.github/workflows/ci.yml`.

Declencheurs :

- push sur toute branche;
- pull request;
- tags `v*`.

Etapes :

1. `dotnet restore`
2. `dotnet build --no-restore`
3. `dotnet test --no-build`
4. `dotnet list package --vulnerable --include-transitive`

Le workflow echoue si le scan detecte une section `has the following
vulnerable packages`.

## Docker image

Le job Docker pousse vers GitHub Container Registry :

```text
ghcr.io/iboula/diamono-mvp:<tag>
```

Tags :

- SHA du commit pour chaque push;
- `latest` sur la branche par defaut;
- tag Git pour une release versionnee.

Les credentials ne sont pas stockes dans le depot. Le workflow utilise
`GITHUB_TOKEN` avec permission `packages: write`.

## CD demo

Workflow : `.github/workflows/deploy-demo.yml`.

Declencheurs :

- manuel via `workflow_dispatch`;
- tag `demo-*`.

Le workflow est volontairement generique : il deploie par SSH vers un hote
configure par secrets GitHub. Il ne suppose ni cloud provider, ni Kubernetes,
ni Terraform.

## Parametres requis

Secrets :

- `DEMO_SSH_HOST`
- `DEMO_SSH_USER`
- `DEMO_SSH_PRIVATE_KEY`
- `DIAMONO_CONNECTION`
- `DIAMONO_POSTGRES_PASSWORD`
- `DIAMONO_ADMIN_PASSWORD`
- `GHCR_USERNAME`
- `GHCR_TOKEN`

Variables GitHub d'environnement `demo` :

- `DEMO_DEPLOY_PATH`
- `DEMO_URL`

## Release flow

1. Developper sur branche feature.
2. Ouvrir PR.
3. CI verte.
4. Merger vers branche de release ou branche par defaut.
5. Creer un tag `vX.Y.Z` pour image versionnee.
6. Creer un tag `demo-YYYYMMDD` ou lancer `deploy-demo` manuellement.
7. Health gate.
8. Smoke test automatique.
9. Smoke test manuel staging.
10. Validation demo.

## Limite volontaire

Les migrations ne sont pas lancees directement par le container runtime. Elles
doivent etre appliquees avant deploiement avec un runner disposant du SDK .NET
et de la variable `DIAMONO_CONNECTION`.
