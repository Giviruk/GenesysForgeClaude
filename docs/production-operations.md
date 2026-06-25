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
