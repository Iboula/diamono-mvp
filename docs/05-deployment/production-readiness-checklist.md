# Production readiness checklist

## Secrets and configuration

- [ ] `DIAMONO_CONNECTION` configure hors depot.
- [ ] `DIAMONO_ADMIN_PASSWORD` configure pour les environnements de demo non production.
- [ ] Aucun mot de passe, token, cookie ou chaine de connexion complete dans `appsettings*.json`.
- [ ] `ASPNETCORE_ENVIRONMENT=Production` pour le deploiement production-like.

## Database

- [ ] Backup PostgreSQL realise avant deploiement.
- [ ] Restore teste sur une base separee.
- [ ] Migrations appliquees avec `dotnet ef database update`.
- [ ] `EnsureCreated` absent du chemin de deploiement.

## Security

- [ ] HTTPS active devant l'application.
- [ ] Redirection HTTPS active hors Development.
- [ ] Cookies Identity `HttpOnly`, `Secure` hors Development et `SameSite=Lax`.
- [ ] Antiforgery actif.
- [ ] Rate limiting actif sur `/login`.
- [ ] Backoffice `/admin` protege.
- [ ] Admin password change avant demo.
- [ ] Demo accounts verifies et limites au contexte de demo.

## Observability

- [ ] `/health/live` retourne `Healthy`.
- [ ] `/health/ready` retourne `Healthy`.
- [ ] Logs startup disponibles.
- [ ] Logs erreurs DB disponibles sans secrets.
- [ ] Logs provider notification/payment disponibles sans secrets.
- [ ] Audit metier conserve separe des logs techniques.

## Quality gates

- [ ] `dotnet restore`.
- [ ] `dotnet build`.
- [ ] `dotnet test`.
- [ ] `dotnet list package --vulnerable --include-transitive`.
- [ ] Smoke test public pages.
- [ ] Smoke test login.
- [ ] Smoke test admin.
- [ ] Smoke test reservation.
- [ ] Smoke test payment.
- [ ] Smoke test receipt PDF.

## Business demo warnings

- [ ] La mention demo/provisoire figure toujours sur les recus.
- [ ] Les tarifs restent provisoires si non valides par la direction/mairie.
