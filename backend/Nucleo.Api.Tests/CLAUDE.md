# Nucleo.Api.Tests

66 tests (xUnit + Moq): `Services/` covers the Service layer with **mocked repositories — no DB, no host, no `Jwt:Key`**; `Domain/` covers the state-transition tables. That's why CI can run them without SQL Server.

```
dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj
dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj --filter "FullyQualifiedName~ActivoServiceTests"
```

**The API must NOT be running** while testing — the running `Nucleo.Api.exe` locks the build output.

- `Helpers/SqlExceptionFactory` builds a real `SqlException` with error number 547 via reflection (the type has no public constructor) — used to test that `ActivoService.CambiarEstadoAsync` translates an FK violation to 404 **and** calls `RevertirTransaccionAsync()`. Reuse it for any future test that needs a specific SQL error.
- Transactional tests should verify the `IUnitOfWork` call sequence (iniciar → guardar → confirmar, or revertir on failure), not just the returned value.
- `docs/TESTING.md` has the full coverage map and the manual per-phase validation record (Postman/browser) — update it when adding test classes.
