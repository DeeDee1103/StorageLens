# Dashboard Guide

## Access

- Use `/` or `/Index` for the public product home.
- Open **Global View** from the landing page or the app navigation for the operational dashboard experience.

## Dashboard Metrics

- Total storage
- Total files
- Duplicate percentage
- Potential savings bytes
- Total locations

## Visualization Areas

- Storage tiers and capacity trend
- Global storage distribution map
- Active workflow status and progress rings
- File type distribution

## Global Storage Distribution Behavior

- Global distribution values are now powered by live aggregated FileInventory data, not sample/demo values.
- Location size and file counts in Global View are synchronized from authoritative file inventory totals.
- When a new location is added, it appears in Global View after analytics refresh and receives map placement automatically.
- If explicit coordinates are unavailable, map placement is derived deterministically from location metadata so every configured location can be plotted.

## Navigation Note

- Use the product home for overview and entry-point actions.
- Use **Global View** for live operational monitoring, and use the surrounding app pages for locations, jobs, duplicates, and reports.

## Interpretation Tips

- Compare duplicate trends over time, not single scans only
- Validate outlier spikes against recent scan scope changes
- Correlate storage-tier and workflow signals before prioritizing remediation work
- If a location initially shows low or zero values, trigger or wait for analytics refresh after scan ingestion to populate synchronized totals.
