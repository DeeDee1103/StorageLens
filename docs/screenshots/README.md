# StorageLens UI Screenshots

Full-page screenshots of every UI screen in the StorageLens web application, captured from a live running instance (all microservices running with in-memory databases).

---

## Dashboard

Operational dashboard view for capacity, duplicate waste, location coverage, and scan performance. Use the public home at `/` or `/Index` to enter the product experience, then open the operational dashboard from Global View and related workspace links.

![Dashboard](https://github.com/user-attachments/assets/9761f6d4-b9e5-4f4a-a3f6-e9dab7326d2f)

---

## Storage Locations

Manage NAS paths, local drives, and file shares monitored by StorageLens. Includes the Add Location form and a table of all configured locations with status, last scan time, total size, and file count.

![Storage Locations](https://github.com/user-attachments/assets/745f3011-dcaf-4d16-a537-8f3df661a321)

---

## Scan Jobs

Track current and historical scans with progress and outcomes. Includes the Start New Scan form (location selector + notes) and a jobs table showing status badges, progress bars, timestamps, and notes.

![Scan Jobs](https://github.com/user-attachments/assets/e989ac6e-580f-4bad-bc9d-bb472e1f6708)

---

## File Inventory

Browse file metadata from scanned content with search, extension filter, and pagination. Displays file name, path, extension badge, size, last modified date, location, and hash status.

![File Inventory](https://github.com/user-attachments/assets/f4975d15-0cff-4c8f-b3a1-c9d8aa92ec63)

---

## Duplicate Files

Identify duplicate waste and triage high-impact groups for cleanup. The page now opens in Interactive mode by default, with the Original view still available for the classic KPI-and-table layout.

![Duplicate Files](https://github.com/user-attachments/assets/4aeaa267-2d1f-41f9-9c8d-a749dd96e7af)

---

## Reports & Insights

Surface operational hotspots, old data, and heavy storage consumers. The page includes CSV export actions for oldest files, largest files, common file types, and duplicate-heavy locations.

![Reports](https://github.com/user-attachments/assets/fdb38511-b69f-4869-b601-b35f3f69a857)

---

## Pricing

Public-facing pricing catalogue. Shows the active plans: Starter ($99), Professional ($499), and Enterprise (contact sales), aligned to the current feature packaging and acquisition flow.

![Pricing](https://github.com/user-attachments/assets/a6ffd61a-1364-4d74-b0c7-ca90901c5077)

---

## Settings

Central settings hub. Links to Database Configuration, Appearance & Display, Notifications, Scanner & Performance, Environment, and Pricing Management. Also shows coming-soon cards for Authentication, Cloud Connectors, Scheduled Scans, Retention Policies, and AI Features.

![Settings](https://github.com/user-attachments/assets/461b8877-1760-4b4d-92f8-7355ecdf2870)

---

## Database Configuration

Configure database provider (LocalDB or Azure SQL), connection parameters, generated connection string preview, per-service connection string snippets, and service endpoint URLs.

![Database Configuration](https://github.com/user-attachments/assets/23dbf02e-b33d-495c-974c-4746620d456c)

---

## Appearance & Display

Personalise theme (Light / Dark / System), default landing page, sidebar collapse, rows per page, scan auto-refresh interval, dashboard date range, and animation/accessibility preferences. Includes a Reset to Defaults action.

![Appearance & Display](https://github.com/user-attachments/assets/3cd096e7-98a6-495f-8be9-00519ebaa101)

---

## Notification Settings

Configure SMTP email alerting and webhook notifications. Choose which events trigger alerts (scan completed, scan failed, duplicates detected, storage threshold exceeded).

![Notification Settings](https://github.com/user-attachments/assets/0dc6d5de-cf90-437f-8dad-4a580da4aa96)

---

## Scanner & Performance

Fine-tune hashing worker parallelism, batch size, and max file size. Configure scanner path mappings and auto-mapping behaviour. Includes a performance tuning reference table and downloadable `appsettings` snippets.

![Scanner & Performance](https://github.com/user-attachments/assets/83a6a957-e0a5-4055-99e7-7f433bbe6add)

---

## Environment Settings

Switch the active edit environment between Development and Production. Shows the runtime environment, the active edit target, and a reference table of the ASP.NET Core configuration file hierarchy.

![Environment Settings](https://github.com/user-attachments/assets/916e6cd1-76a4-43bf-ac5e-f1f9af953027)

---

## Pricing Admin

Owner-only pricing management, protected by a PIN. Allows editing tiers, services, and the active subscription tier. Shows the PIN unlock gate on first load.

![Pricing Admin](https://github.com/user-attachments/assets/2e6b6863-805f-4177-8291-245289af5c2b)

---

## Profile

User profile page showing avatar, role, contact details, activity stats, theme/notification preferences, security info (last sign-in, SSO, 2FA), active subscription plan, and recent activity feed.

![Profile](https://github.com/user-attachments/assets/4eaeda1c-f9f9-427c-9c6f-0515866cd7dc)

---

## About

About StorageLens page with product description, KPI highlights, mission statement, core values, key features grid, technology stack panel, support contact details, and release notes.

![About](https://github.com/user-attachments/assets/4707fa39-d3b2-4fda-acc3-4fa845e0566e)

---

## Privacy Policy

Privacy Policy page (placeholder content).

![Privacy Policy](https://github.com/user-attachments/assets/7f3ba4de-046c-423d-b654-7f6f72016b3c)
