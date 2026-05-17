# DAO MySQL

Implementações de DAO para o banco MySQL (banco principal).

**Fase 2-3:** Serão criadas:
- `MySqlConnectionFactory.cs` — gerencia connection string e pool
- `MySqlEmpresaDao.cs` — CRUD de Empresa
- `MySqlGrupoEconomicoDao.cs`
- `MySqlBalancoDao.cs`
- `MySqlContaBalancoDao.cs`
- `MySqlUsuarioDao.cs`

Todas implementam `IDao<T>` (em `/Interfaces/IDao.cs`).
