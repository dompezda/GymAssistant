# GymAssistant — deployment

Updated: 24 November 2026. Configuration and manual checks below reflect the owner's Azure portal and browser observations; this is not an independent audit of the Azure account.

## Production topology

| Component | Configuration |
| --- | --- |
| Frontend | Azure Static Web Apps, https://www.gymassistant.pl/ (apex domain also configured) |
| API | Azure App Service Linux / .NET 10, Basic B1, one instance, Poland Central, Always On |
| API hostname | https://api.gymassistant.pl/ |
| SQL | GymAssistantDb, Azure SQL General Purpose Serverless Free Offer, Poland Central |
| Database identity | System-assigned Managed Identity, db_datareader/db_datawriter; migrations run separately with admin permissions |
| CI/CD | GitHub Actions, frontend and backend automated deployment |

## DNS and HTTPS

OVHcloud manages the DNS zone. CNAME `api` points to `gymassistant-api-hdcqexa5eze8e7au.polandcentral-01.azurewebsites.net`. TXT `asuid.api` contains the Azure domain verification identifier; retain it and obtain its value from Azure rather than copying account-specific values here. Azure App Service Managed Certificate bound with SNI SSL; portal status Secured. Keep existing frontend DNS records unchanged.

Production frontend workflow `.github/workflows/azure-static-web-apps-salmon-stone-005ef080f.yml` sets `VITE_API_URL=https://api.gymassistant.pl` at build time. API CORS must allow `https://www.gymassistant.pl` with credentials. Review actual production origin configuration and HTTPS enforcement under P0-10. Keep secrets in Azure settings or approved secret stores, not source control or logs.

## Manual smoke checks

- Custom API GET /api/exercises without JWT: 401, no certificate error.
- Login, delete only the access token from localStorage, reload: refresh 200 and session recovered; repeated test passed.
- DevTools Sec-Fetch-Site: same-site; frontend and API remain cross-origin.
- Logout then reload: session not recovered.
- Remaining P0-02: natural JWT expiry and explicit rotation check. Full production end-to-end smoke tests remain P0-14.

## Costs and billing

- B1 portal estimate at selection: USD 13.155/month per instance (before applicable taxes, other usage and currency conversion); subscription cost analysis uses EUR. Actual bill may differ. Always On keeps API active but does not prevent SQL cold starts.
- Monthly-Azure-Budget: EUR 20, entire Personal-Projects subscription, monthly reset, expiry 31 October 2028. Actual-cost email alerts at EUR 10/16/20 (50/80/100%) and forecast alert at EUR 20 (100%). Saved conditions and recipient verified. Default payment method present; no card or email details recorded.
- Budgets notify; they do not cap spending or automatically stop resources. Cost reporting can lag. Stopping the web app alone does not stop B1 plan charges; downgrade or remove the paid plan if necessary, considering service interruption and custom TLS limitations.
- Azure SQL Free Offer applied; 100,000 vCore-seconds/month and 32 GB data limit shown. Overage billing disabled; auto-pause until next month selected. Min 0.5 and max 2 vCores. If quota is exhausted the database becomes unavailable until renewal rather than incurring paid overage. Other Azure services can still incur costs.
- Review actual cost and free SQL quota regularly; avoid adding DB-dependent periodic probes that unintentionally keep serverless SQL active. Renew the budget before expiry.

## SQL connection timeout investigation (24 November 2026)

After an idle period, two production POST /api/auth/login attempts returned HTTP 500; the third succeeded with approximately 5.76 seconds waiting for server response. App Service logs at 18:24:51 UTC show Microsoft.Data.SqlClient.SqlException, error -2, Connection Timeout Expired during the SQL post-login phase (pre-login initialization 81 ms, handshake 744 ms, post-login 14,004 ms). This is a connection-open failure, not a query execution timeout. Program.cs already configures EnableRetryOnFailure; effective deployed retry behavior has not been verified. SQL serverless auto-resume is the leading hypothesis, not established root cause.

Next: confirm database state around reproduction, inspect effective connection timeout and retry logs, then test Connect Timeout=60 in a controlled configuration change and compare cold/warm login behavior. This is a diagnostic, not a latency fix; assess reliable mitigation and error UX before public release. Do not change SQL tier, paid overage or add periodic database wake-up probes without assessing cost and quota impact. P1-12 is now a P0 release blocker (stable ID retained). No configuration change was made during this investigation.

## Open operational work

Production SMTP, Data Protection persistence, firewall review, backup retention and restoration verification, cold-start handling, health checks and monitoring, migration and rollback strategy, and full QA remain tracked in [todo.md](todo.md). Earlier deployment history is in [CHANGELOG.md](CHANGELOG.md).
