# Stock Replenishment Requests

A small feature where line workers request material for their station, and a reviewer approves or rejects
the request before the warehouse fulfils it.

Built with ASP.NET Core (.NET 10), EF Core (in-memory), Blazor Server + MudBlazor, NUnit and NSubstitute.

## Running it

You need the .NET 10 SDK.

```
cd src/StockReplenishment
dotnet run
```

Open the URL shown in the console (http://localhost:5110 by default). The app starts with 18 seeded
requests covering every status, so there is something to look at right away.

Swagger is at `/swagger` if you want to try the API directly.

Tests: run `dotnet test` from the repository root.

## How it works

**Workflow.** A request goes Draft → Submitted → Approved → Fulfilled, or Submitted → Rejected. The rules
live in `ReplenishmentRequest` itself (`Submit()`, `Approve()`, `Reject()`, `Fulfill()`), so the
controllers and the UI can't put a request in an invalid state.

**The slow stock check.** Submitting saves the request as Pending, puts its id on an in-memory queue and
returns `202 Accepted` right away. A background service picks it up and calls the (simulated) warehouse
service, which takes 3–8 seconds and fails about 10% of the time. The result is written back to the
request: Completed with available quantities per line, or Failed.

The UI finds out by polling: the request page re-fetches every 2 seconds while the check is pending.
Any other API client can do the same with `GET /api/requests/{id}`.

**Errors.** Invalid input returns 400, an unknown id 404, and an action that isn't allowed in the current
status (for example approving while the check is still running) 409. All of them are standard problem
details responses.

**UI.** The Blazor pages call the REST API through `HttpClient` rather than using the services directly,
since the assignment is about the API integration.

## API

| Method | Route | |
|---|---|---|
| GET | `/api/requests?status=&priority=&locationId=&page=&pageSize=` | paged list with filters |
| GET | `/api/requests/{id}` | one request with its lines |
| POST | `/api/requests` | create a draft |
| POST | `/api/requests/{id}/submit` | 202, starts the stock check |
| POST | `/api/requests/{id}/approve` | |
| POST | `/api/requests/{id}/reject` | requires `{ "reason": "..." }` |
| POST | `/api/requests/{id}/fulfill` | fulfilled quantity per line |
| GET | `/api/locations` | |

## Assumptions

- Quantities are whole numbers.
- There's no login. "Requested by" is a free-text name.
- A request can be approved after a failed stock check. The reviewer decides; only a pending check
  blocks approval.

## What I'd do with more time

- Push the stock check result to the browser with SignalR instead of polling.
- Retries and a timeout around the warehouse call, plus a "retry check" button.
- Re-queue pending checks on startup. Right now a restart loses them, since the queue is in memory.
- Optimistic concurrency, so two people can't act on the same request at the same time.
- A status history per request.
- Authentication and roles, including "you can't approve your own request".
- A real database with migrations, and API-level tests with `WebApplicationFactory`.
