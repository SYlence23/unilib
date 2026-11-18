# UniLib

[![CI](https://github.com/SYlence23/unilib/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/SYlence23/unilib/actions/workflows/ci.yml)

Desktop application for a university library: catalog, loans, reservations
and favorites. Built with .NET 10 WinForms, EF Core and PostgreSQL.

## Build and test

```powershell
dotnet build unilib.slnx
dotnet test unilib.slnx
```

The app reads its PostgreSQL connection string from `unilib/appsettings.json`.
The tests run against in-memory SQLite and need no database.

## Continuous integration

Every push to `main` and every pull request builds the solution, runs the
tests and checks formatting and vulnerable packages. See [docs/CI.md](docs/CI.md).
