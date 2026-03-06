# Docker Deployment

## Compose Stack

Use root `docker-compose.yml`.

## Commands

```bash
docker compose up -d --build
docker compose ps -a
docker compose logs -f orchestrator
docker compose down
```

## Notes

- SQL Server persists via `storagelens-sql-data` volume.
- Scanner and Hashing mount `./sample-scan:/scan-root` for local sample data.
- RabbitMQ is included for future asynchronous integration.

## Troubleshooting

- If containers exit quickly, inspect logs by service.
- Validate database name isolation per service.
- Confirm host path mounts exist and are accessible.
