# Reverse proxy

Configuration recommandee : Caddy, pour HTTPS automatique et support simple
des WebSockets Blazor.

## Caddy

Voir `deploy/Caddyfile.example`.

Points importants :

- TLS automatique via le domaine public.
- Reverse proxy vers le container `Diamono.Web` sur `8080`.
- Headers `X-Forwarded-Proto`, `X-Forwarded-Host`, `X-Forwarded-For`.
- Timeouts superieurs aux actions interactives Blazor.
- PostgreSQL non expose publiquement.

Exemple :

```caddyfile
demo.stadediamono.sn {
	reverse_proxy diamono-web:8080 {
		header_up X-Forwarded-Proto {scheme}
		header_up X-Forwarded-Host {host}
		header_up X-Forwarded-For {remote_host}
		transport http {
			read_timeout 120s
			write_timeout 120s
		}
	}
}
```

## Blazor Server

Blazor Server utilise WebSocket/SignalR. Caddy proxy automatiquement les
upgrades HTTP necessaires. Si un autre proxy est utilise, verifier :

- support `Connection: Upgrade`;
- support `Upgrade: websocket`;
- timeouts assez longs;
- forwarding du protocole HTTPS.

## HTTPS et cookies

En `Production`, Diamono marque les cookies Identity comme `Secure` et redirige
HTTP vers HTTPS, sauf pour les endpoints `/health/*` afin de permettre les
healthchecks internes Docker.

Le reverse proxy doit transmettre `X-Forwarded-Proto` pour que l'application
reconnaisse correctement la requete publique HTTPS.
