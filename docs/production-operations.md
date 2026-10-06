# Production operations

Run all commands from the deploy directory containing `docker-compose.prod.yml` and `.env`.

## Release checklist

1. CI is green for backend and frontend.
2. `JWT_KEY` is random, at least 32 characters and is not a sample value.
3. `PRIVATE_HOSTNAME` and `PUBLIC_HOSTNAME` are HTTPS hostnames with DNS pointed to the VPS.
4. `docker compose -f docker-compose.prod.yml config` succeeds.
5. Create an on-demand backup: `sh deploy/backup-now.sh`.
6. Record the currently deployed `IMAGE_TAG` as the rollback tag.
7. Pull and deploy the new immutable commit tag.
8. Verify both endpoints:
   - `https://$PRIVATE_HOSTNAME/api/health`
   - `https://$PUBLIC_HOSTNAME/api/health`
9. Smoke-test login, refresh, character list and one public reference page.
10. Verify logs contain no startup/configuration errors.

## Automated backups

The `backup` compose service creates custom-format PostgreSQL dumps every 24 hours:

- `/backups/private-<UTC timestamp>.dump`
- `/backups/public-<UTC timestamp>.dump`

The dumps live in the `pgbackups` Docker volume. Default retention is 14 days and can be
changed with `BACKUP_RETENTION_DAYS`. Monitor free disk space and copy backups off-host;
a volume on the same VPS is not a disaster-recovery copy.

Create local copies immediately:

```sh
sh deploy/backup-now.sh
```

## Restore

Restore is destructive and requires an explicit typed confirmation:

```sh
sh deploy/restore.sh private private-20260625T120000Z.dump
sh deploy/restore.sh public public-20260625T120000Z.dump
```

After restore, check `/api/health`, login and representative character/reference data.

## Rollback

Deploy the previously recorded immutable commit image tag:

```sh
sh deploy/rollback.sh <previous-git-sha>
```

The script updates application containers only; it does not roll back the database. Database
migrations must therefore remain backward-compatible, or a separately approved restore must be
performed. After verification, write the previous tag to `.env` as `IMAGE_TAG`.

## PostgreSQL version

`postgres`, `postgres-public` and `backup` use a pinned patch tag (`postgres:17.11-alpine`), so a
release deploy never upgrades or restarts the databases by accident. To upgrade, change the tag in all
three services in one PR (`pg_dump` in `backup` must match the server version) and expect both
databases to restart during that deploy.

## Query statistics (pg_stat_statements)

Both `postgres` and `postgres-public` start with `shared_preload_libraries=pg_stat_statements`. The
extension itself is created once per database and persists in its volume:

```sh
docker exec genesysforge-db psql -U genesys -d genesysforge \
  -c 'CREATE EXTENSION IF NOT EXISTS pg_stat_statements;'
docker exec genesysforge-db-public psql -U genesys -d genesysforge_public \
  -c 'CREATE EXTENSION IF NOT EXISTS pg_stat_statements;'
```

Slowest statements by total time:

```sh
docker exec genesysforge-db psql -U genesys -d genesysforge -c "
  SELECT calls, round(mean_exec_time::numeric, 2) AS mean_ms, round(max_exec_time::numeric, 2) AS max_ms,
         round(total_exec_time::numeric) AS total_ms, left(regexp_replace(query, '\s+', ' ', 'g'), 120) AS query
  FROM pg_stat_statements ORDER BY total_exec_time DESC LIMIT 20;"
```

Reset counters after a change you want to measure: `SELECT pg_stat_statements_reset();`.

## Stack capacity

Both `api` and `api-public` connect with `Maximum Pool Size=10;Max Auto Prepare=64;Auto Prepare Min Usages=2`.
Each sheet read runs 13 split queries; without prepared statements PostgreSQL spent ~4 ms planning each
of them and ~0.2 ms executing. Prepared plans live in every backend process, so the pool is capped and
both databases have 384m. Measured locally on 06.10.2026 with `scripts/load-api.mjs` (production
limits, PostgreSQL 17, nginx):

| Full sheet read | Before | After |
| --- | ---: | ---: |
| 1 client, p50 | 98 ms | 24 ms |
| 10 clients, requests/s | 30 | 109 |
| 50 clients, requests/s | 33 | 111 |

Both APIs have 768m: under load the process takes ~400 MB including ~100 MB of shared memory for JIT
code, and .NET gives the GC heap 75% of the limit. `api-public` (1.5 CPU) and `postgres-public` (1 CPU)
are capped so a public traffic spike cannot take both vCPUs of the shared VPS; the private stack is not
capped. The extra memory comes from the reoair and ariadne-test stacks stopped on 06.10.2026.

## PublicSafe isolation

The public stack uses:

- `genesysforge-api-public`, built from Docker target `public`;
- `Content__Mode=PublicSafe`;
- a separate `genesysforge_public` database and `pgdata_public` volume;
- a distinct JWT signing key namespace derived from the deployment secret;
- `web-public` with `API_UPSTREAM=api-public`;
- `PUBLIC_HOSTNAME` in Caddy.

The public Docker target publishes Infrastructure with `IncludePrivateContent=false`, so
`private-content/*.ru.json` resources are not embedded in its runtime assembly. Do not retag the
private API image as the public API image.

If the GitHub variable `PUBLIC_HOSTNAME` is absent, deployment uses
`public-disabled.localhost`. The isolated public containers still start, but no public DNS name is
enabled. Set the variable and DNS before announcing the public service.
