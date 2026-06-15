# DAO MongoDB

Implementações de DAO para MongoDB (logs estruturados).

**Fase 3:** Serão criadas:
- `MongoConnectionFactory.cs`
- `MongoLogDao.cs` — registra todos os eventos do sistema:
  - Login / Logout
  - Inclusão de registro (empresa, balanço, etc)
  - Alteração de dados
  - Exclusão
  - Erros e exceções

Cada documento contém: `{ _id, usuario, acao, descricao, dataHora, tipoEvento, detalhes }`.
