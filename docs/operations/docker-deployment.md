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
- The Compose file works on Linux, macOS, and Windows hosts. Windows-specific drive and UNC volume mounts have been removed; add them back locally if you need to scan host paths.

## Troubleshooting

- If containers exit quickly, inspect logs by service.
- Validate database name isolation per service.
- Confirm host path mounts exist and are accessible.
- If a service crashes on first start with a SQL connection error, SQL Server may still be initialising. The startup initialiser retries automatically; restart the affected service with `docker compose restart <service>` if it already exited before the retry window.
- If Global View shows stale or zero location totals, verify both endpoints are healthy from the analytics container perspective:
	- `GET /api/files/stats/by-location` (FileInventory)
	- `PUT /api/locations/{id}/stats` (Locations)
