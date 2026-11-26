# Security

SqlServerMcp gives AI agents read-only access to SQL Server. The agent is treated as an
**untrusted client**: anything it sends may be wrong, malicious, or the result of prompt
injection. This document explains how the server limits what an agent can do, which rules
the code follows, and what is not covered yet.

> Status: work in progress (version 0.x). Each item below is marked ✅ implemented or 🔜 planned.

## Reporting a vulnerability

Please do not open a public issue. Use GitHub's private vulnerability reporting
(the **Security** tab of this repository, then **Advisories** → **Report a vulnerability**).

Only the latest commit on `main` is supported while the project is at version 0.x.

## Defense in depth

Each layer assumes the one before it could fail.

| # | Layer | Where | Status |
|---|---|---|---|
| 1 | **Database permissions.** A dedicated user with `SELECT` granted table by table, explicit `DENY` on writes, `EXECUTE` and sensitive tables. On Azure, the Container App's managed identity: no password exists. | [`db/create-readonly-user.sql`](db/create-readonly-user.sql) | ✅ local, 🔜 Azure |
| 2 | **Table allow-list.** Tables not listed in `SqlMcp:AllowedTables` do not exist for the agent, even if the database user could read them. | `TableAllowList`, `SchemaService` | ✅ |
| 3 | **Validated identifiers.** Names must match `^[A-Za-z_][A-Za-z0-9_]*$` (max 128 characters), so a validated name cannot carry SQL syntax. Quoted with `[ ]` and `]` escaped anyway. | `TableName` | ✅ tables, 🔜 columns |
| 4 | **No free SQL.** The agent never sends SQL. `query_table` takes a structured request (table, columns, filters, top) and the server builds the statement. Values are always SQL parameters. | `QueryBuilder` | 🔜 |
| 5 | **Limits.** Row cap (`SqlMcp:MaxRows`), command timeout (`SqlMcp:CommandTimeoutSeconds`), `ApplicationIntent=ReadOnly`. | `SqlMcpOptions`, `SqlConnectionFactory` | ✅ timeout and intent, 🔜 row cap in `query_table` |
| 6 | **Authentication.** `/mcp` requires the `X-API-Key` header, compared in constant time. `/health` is public and returns no data. | `ApiKeyMiddleware` | ✅ (Microsoft Entra ID 🔜 after the MVP) |

## Rules the code follows

### Wrong configuration stops the server; wrong requests do not

| | Who controls it | What happens when it is wrong |
|---|---|---|
| **Configuration** | Only the operator | The server **refuses to start** (fail fast). |
| **Requests** | Anyone who can reach the endpoint | The request gets an **error response**; the server keeps running. |

A request must never be able to stop the process. In practice:

- `Parse` (throws) is used only on configuration; `TryParse` (returns `false`) is used on
  everything that comes from outside.
- No `async void` and no `Task.Run` without `await`: an unobserved exception must not be
  able to crash the host.

### Fail fast, with a message that names the setting

At startup the server validates its configuration and stops if any of these is wrong:

- an invalid name in `SqlMcp:AllowedTables`, or an empty list;
- `SqlMcp:MaxRows` or `SqlMcp:CommandTimeoutSeconds` out of range;
- `ApiKey:Enabled` is `true` but `ApiKey:Key` is empty;
- `ApiKey:Enabled` is `false` outside the Development environment;
- `ConnectionStrings:Sql` is missing, empty or whitespace.

Every error message names the configuration key to fix, so an operator reading the logs
of a failed deployment knows what to change. These rules are covered by
`StartupValidationTests`, which also check that each failure is the expected one and only
that one.

### Minimal visibility

Types that are not needed outside their assembly are `internal`. For example,
`ISqlConnectionFactory` is internal to Infrastructure, so the Server project cannot obtain
a raw `SqlConnection` and bypass the allow-list or the structured queries.

This is a **design guardrail, not a security boundary**: it stops developers (or a coding
agent) from taking shortcuts at compile time, but code running in the same process could
still use reflection. Attackers are kept out by the layers above, not by access modifiers.

### Secrets

- No secret is stored in the repository. Locally: `.env` (git-ignored) or `dotnet user-secrets`.
  On Azure: a Container Apps secret for the API key, managed identity for the database. 🔜
- The API key is never written to logs, not even a wrong one: a wrong key is often the
  right key with a typo. ✅

## Security events

| EventId | Meaning | LogLevel |
|---|---|---|
| **1001** | The API key header is missing from the request | Information |
| **1002** | The API key header is present but incorrect | Warning |

Both events record the client IP address and User-Agent. The key itself is never logged.
Behind a reverse proxy, such as the Azure Container Apps ingress, the recorded IP address is the proxy's until the ForwardedHeaders middleware is configured. 🔜

## Known risks and planned countermeasures

| Risk | Countermeasure | Status |
|---|---|---|
| **Resource exhaustion** (many or heavy requests) | Command timeout; row cap; rate limiting on `/mcp` and a request body size limit (Kestrel's default is about 30 MB, an MCP message is a few KB) before the public deployment; Resource Governor for the agent's sessions, classified by `ApplicationName` | ✅ timeout, 🔜 the rest |
| **Brute force on the API key** | Logging of failed attempts; rate limiting; Entra ID after the MVP | ✅ logging, 🔜 the rest |
| **Prompt injection from the data** (text stored in the database that tries to instruct the agent) | The server is read-only and the agent cannot run free SQL, so injected instructions cannot write or escalate; data is returned as data, never as instructions | 🔜 documented handling |
| **Configuration drift** between `SqlMcp:AllowedTables` and the database grants | Both lists must be kept in sync; the database layer still blocks anything not granted | Manual |
| **Sensitive columns** inside allowed tables | Column deny-list | 🔜 after the MVP |

