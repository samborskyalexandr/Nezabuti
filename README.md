# Nezabuti

Сервіс цифрових меморіальних сторінок про полеглих.

Публічна сторінка: `https://nezabuti.com.ua/m/{PublicId}`

## Stack

- ASP.NET Core Web API (.NET 8)
- Angular 19 + SSR
- Tailwind CSS
- MongoDB 7
- Docker / Docker Compose v2

Локальна розробка працює через Docker. Локально встановлені .NET SDK або MongoDB не потрібні.

## Project structure

```
/backend          ASP.NET Core API
/frontend         Angular (SSR) + nginx entrypoint
/uploads          Photo storage (gitkeep only in Git)
docker-compose.yml
docker-compose.prod.yml
.env.example
BACKUP.md
README.md
```

## Docker architecture (local)

Services:

| Service   | Role                                      | Ports                          |
|-----------|-------------------------------------------|--------------------------------|
| `mongodb` | MongoDB 7                                 | internal only                  |
| `backend` | API on `:8080`                            | exposed inside network         |
| `frontend`| nginx `:80` → SSR Node + proxy `/api`,`/uploads` | host `${FRONTEND_HOST_PORT:-8088}:80` |

MongoDB is **not** published to the host.

Frontend nginx:

- serves the site via Angular SSR (Node on `:4000` behind nginx)
- proxies `/api/` → `backend:8080`
- proxies `/uploads/` → `backend:8080`

## Environment setup

```bash
cp .env.example .env
# edit secrets: JWT_SECRET, ADMIN_USERNAME, ADMIN_PASSWORD, PUBLIC_BASE_URL, …
```

Required values are listed in `.env.example` (no real secrets in Git).

## Local startup

```bash
docker compose up --build
```

Or detached:

```bash
docker compose up --build -d
```

## Local URLs

- Site: http://localhost:8088/
- Memorial: http://localhost:8088/m/{PublicId}
- Admin: http://localhost:8088/manage-nz7k4p/login
  (старий `/admin` навмисно дає 404; JWT обовʼязковий)
- API health: http://localhost:8088/api/health
- API ready (Mongo): http://localhost:8088/api/health/ready

## Build / stop / logs

```bash
docker compose build
docker compose up -d
docker compose stop
docker compose down
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f mongodb
```

## Persistence

- **MongoDB**: Docker volume `mongodb_data`
- **Uploads**: bind mount `./uploads` → `/app/uploads` in backend

## Production compose override

Production VPS already has:

- external Docker network `edge`
- `edge-proxy` owning host ports 80/443

Nezabuti production must **not** publish 80/443.

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build -d
```

Override behavior:

- frontend host ports cleared
- frontend joins external network `edge` with alias `nezabuti-frontend`
- edge-proxy routes `nezabuti.com.ua` → `nezabuti-frontend:80`
- local compose does **not** require network `edge`

Set production `PUBLIC_BASE_URL=https://nezabuti.com.ua` in `.env`.

## Backup

See [BACKUP.md](./BACKUP.md) for scheduled backup, retention, and manual runs (MongoDB + `/uploads`).

## PublicId

Each Memorial gets an immutable cryptographically random `PublicId` (~10 chars, no `0/O/1/I`). QR codes always encode `{PublicBaseUrl}/m/{PublicId}`.
