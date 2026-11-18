# UniLib

[![CI](https://github.com/SYlence23/unilib/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/SYlence23/unilib/actions/workflows/ci.yml)

Desktop application for a university library: catalog, loans, reservations
and favorites. Built with .NET 10 WinForms, EF Core and PostgreSQL.

## Build and test

```powershell
dotnet build unilib.slnx
dotnet test unilib.slnx
```

The tests run against in-memory SQLite and need no database.

## Configuration

`unilib/appsettings.json` holds no secrets. Put the database password in
`unilib/appsettings.Local.json` (git-ignored; copy `appsettings.Local.example.json`)
or set the `UNILIB_ConnectionStrings__Default` environment variable.

Logs are written to `%LOCALAPPDATA%\UniLib\logs`. Logging, error handling, input
validation and the security review are described in [docs/Security.md](docs/Security.md).

## Continuous integration

Every push to `main` and every pull request builds the solution, runs the
tests and checks formatting and vulnerable packages; CodeQL scans the code for
security issues. See [docs/CI.md](docs/CI.md).
