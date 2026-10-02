# OpenMU Portal

A public website for players: account registration, login, character and guild
rankings, server status and client downloads.

It runs as a separate process next to the game server and works directly on the
OpenMU database. It doesn't need the game configuration to be loaded, so it
starts fast and uses little memory.

## How it works

* **Data access:** `Data/PortalDbContext` maps only the tables and columns the
  portal needs. The schema is owned and migrated by the game server; the portal
  never migrates anything. `PortalSchemaTest` in
  `tests/MUnique.OpenMU.Web.Portal.Tests` compares the mapping with the server
  model (`EntityDataContext`). When a server migration renames or changes a
  column the portal uses, or adds a required column to `Account`, that test
  fails.
* **Passwords:** hashed with BCrypt, like the game server and the admin panel do,
  so accounts created here can log in to the game right away.
* **Rankings:** characters are ordered by resets, level, master level and
  experience. Template, bot, game master and banned accounts are left out.
  Results are cached for `Portal:CacheDuration`.
* **Server status:** queried from the admin panel API (`GET api/status`) with an
  API key. Without `Portal:AdminPanelUrl` the status isn't shown.
* **Security:** cookie authentication, antiforgery tokens on all forms (Razor
  Pages default), and rate limiting on the login and registration pages.

## Configuration

All settings are in `appsettings.json` and can be overridden by environment
variables, for example `ConnectionStrings__OpenMU` or `Portal__ServerName`.

| Setting | Description |
|---|---|
| `ConnectionStrings:OpenMU` | Connection string to the OpenMU database. |
| `Portal:ServerName` | Name shown in the header and page titles. |
| `Portal:RegistrationEnabled` | Turns registration on or off. |
| `Portal:MaximumPasswordLength` | 10 for older clients, up to 20 for newer ones. |
| `Portal:RankingSize` | Number of ranking entries. |
| `Portal:ShowBotsInRankings` | Whether bot characters appear in the rankings. |
| `Portal:CacheDuration` | How long rankings and status are cached. |
| `Portal:AdminPanelUrl` | Base address of the admin panel, e.g. `http://localhost/`. |
| `Portal:AdminPanelApiKey` | API key created on the admin panel's *API Keys* page. |
| `Portal:Downloads` | List of client download links (`Title`, `Url`, `Size`). |

## Database role

Don't connect with the `postgres` superuser. Create a role that can only read
what the portal shows and insert accounts:

```sql
CREATE ROLE portal WITH LOGIN PASSWORD 'change-me';
GRANT USAGE ON SCHEMA data, config, guild TO portal;
GRANT SELECT ON data."Account", data."Character", data."StatAttribute" TO portal;
GRANT INSERT ON data."Account" TO portal;
GRANT SELECT ON config."CharacterClass" TO portal;
GRANT SELECT ON guild."Guild", guild."GuildMember" TO portal;
```

## Running

```bash
dotnet run --project src/Web/Portal --urls http://localhost:5080
```

Then open `http://localhost:5080`. In production, run it
behind a reverse proxy (nginx, caddy) that terminates TLS. If the proxy isn't on
the same host, add its address to `ForwardedHeadersOptions.KnownProxies` in
`Program.cs`, so that rate limiting sees the real client addresses.

## Next steps

* Password change and password reset by e-mail.
* News, e.g. from markdown files or a headless CMS.
* Character actions in the account area (reset, stat reset, unstuck). These have
  to be checked against the game server, because the character must not be
  online while the database is changed.
* Translations (`IStringLocalizer`).
