# Nezabuti — Backup

Backup is part of the production architecture. This stage documents the approach; there is no restore API.

## What to back up

1. **MongoDB** — database `${MONGO_DATABASE}` (default `nezabuti`)
2. **Uploads** — host/volume path `./uploads` (memorial photos under `uploads/memorials/{PublicId}/`)

## Scheduled backup (recommended)

Run daily via cron on the VPS (example: 03:15 UTC).

```bash
#!/usr/bin/env bash
set -euo pipefail

RETENTION=14
BACKUP_ROOT=/var/backups/nezabuti
STAMP=$(date -u +%Y%m%dT%H%M%SZ)
DEST="$BACKUP_ROOT/$STAMP"
COMPOSE_DIR=/opt/nezabuti   # adjust

mkdir -p "$DEST/mongodb" "$DEST/uploads"

# Mongo dump from the running compose service (no published host port required)
docker compose -f "$COMPOSE_DIR/docker-compose.yml" -f "$COMPOSE_DIR/docker-compose.prod.yml" \
  exec -T mongodb mongodump --db "${MONGO_DATABASE:-nezabuti}" --archive > "$DEST/mongodb/nezabuti.archive"

# Uploads directory (bind mount or copy from volume)
cp -a "$COMPOSE_DIR/uploads/." "$DEST/uploads/"

# Optional compression
tar -C "$BACKUP_ROOT" -czf "$DEST.tar.gz" "$STAMP"
rm -rf "$DEST"

# Retention: keep last N backups
ls -1dt "$BACKUP_ROOT"/*.tar.gz | tail -n +$((RETENTION + 1)) | xargs -r rm -f
```

Store `RETENTION` as the number of newest backups to keep (for example `14`).

## Manual backup

From the project directory:

```bash
# 1) MongoDB
docker compose exec -T mongodb mongodump --db nezabuti --archive > backup-mongo-$(date -u +%Y%m%d).archive

# 2) Uploads
tar -czf backup-uploads-$(date -u +%Y%m%d).tar.gz uploads
```

On production, include the prod override files in `docker compose` commands as needed.

## Notes

- Prefer consistent timing: dump Mongo first, then copy uploads.
- Do not commit backup archives to Git.
- Restore procedures will be documented in a later stage (no restore API in this foundation).
- Verify backups periodically by restoring into a disposable environment.
