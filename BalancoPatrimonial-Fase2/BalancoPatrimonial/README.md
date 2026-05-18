# Balanço Patrimonial — Análise de Crédito

Software de análise de balanços patrimoniais de empresas para fins de análise de crédito bancária.

> **Status atual:** Fase 2 — Models de domínio + Scripts MySQL + Conexões com os 3 bancos.

---

## Stack

- **.NET 8** + **.NET MAUI**
- **C# 12** com `nullable` e `implicit usings`
- **Arquitetura:** MVC + Service Layer + DAO
- **Bancos:**
  - MySQL (principal) — `MySqlConnector` 2.3
  - SQLite (configurações locais) — `sqlite-net-pcl`
  - MongoDB (logs) — `MongoDB.Driver` 2.28
- **Senhas:** BCrypt.Net-Next

## Setup completo

### 1. Pré-requisitos

```bash
dotnet workload install maui
# E ter rodando localmente:
#   - MySQL 8+   na porta 3306
#   - MongoDB 6+ na porta 27017
```

### 2. Restaurar pacotes

```bash
cd BalancoPatrimonial.App
dotnet restore
```

### 3. Criar e popular o MySQL

```bash
cd ../Database
mysql -u root -p < 01_schema.sql
mysql -u root -p balanco_patrimonial < 02_seed_plano_contas.sql
mysql -u root -p balanco_patrimonial < 03_seed_setores_e_demo.sql
```

Detalhes em [`Database/README.md`](Database/README.md).

### 4. Ajustar credenciais

Edite os defaults em `BalancoPatrimonial.App/DAO/DatabaseSettings.cs` se sua senha de root não for `root` ou se MongoDB não estiver em localhost.

### 5. Rodar

```bash
dotnet build -t:Run -f net8.0-windows10.0.19041.0
```

## Login de teste

- **Usuário:** `admin` / **Senha:** `admin`

(Ainda stub em código na Fase 2. Na Fase 3 será autenticado contra a tabela `usuario` do MySQL.)

## Estrutura de pastas

```
BalancoPatrimonial/
├── BalancoPatrimonial.sln
├── BalancoPatrimonial.App/         ← código C#
│   ├── Interfaces/                 ← IDao, IController, IService
│   ├── Models/                     ← entidades de domínio
│   │   ├── Empresa.cs
│   │   ├── Balanco.cs
│   │   ├── ContaPadrao.cs
│   │   ├── ContaBalanco.cs
│   │   ├── GrupoEconomico.cs
│   │   ├── SetorAtividade.cs
│   │   ├── Usuario.cs
│   │   ├── ConfiguracaoLocal.cs    ← SQLite
│   │   ├── ResultadoOperacao.cs
│   │   ├── Enums/                  ← TipoBalanco, GrupoContaPrincipal, etc
│   │   └── Log/LogSistema.cs       ← MongoDB
│   ├── DAO/
│   │   ├── DatabaseSettings.cs     ← configs centrais
│   │   ├── MySQL/                  ← MySqlConnectionFactory
│   │   ├── SQLite/                 ← SqliteConnectionFactory
│   │   └── MongoDB/                ← MongoConnectionFactory
│   ├── Services/
│   ├── Controllers/
│   ├── Views/                      ← Login + 4 páginas do sidebar
│   └── Resources/Styles/
├── Database/                       ← scripts SQL
│   ├── 01_schema.sql
│   ├── 02_seed_plano_contas.sql
│   ├── 03_seed_setores_e_demo.sql
│   └── README.md
└── Docs/                           ← documentação pra entrega
    ├── Arquitetura.md
    ├── RegrasDeNegocio.md
    └── DER.md
```

## Fases

- [x] **Fase 1:** Skeleton + sidebar + login (stub)
- [x] **Fase 2:** Models + schema MySQL + conexões com os 3 bancos
- [ ] **Fase 3:** DAOs concretas + CRUD da página Empresas + autenticação real + LogService
- [ ] **Fase 4:** Planilhamento manual com cálculo automático
- [ ] **Fase 5:** Parser de PDF (PdfPig) com detecção Consolidado/Individual
- [ ] **Fase 6:** Exports — Excel (ClosedXML), JSON, XML de logs, PDF de relatório (QuestPDF)
- [ ] **Fase 7:** Gráficos (LiveCharts2), Área de Testes funcional, polimento

## Documentação

- [Arquitetura](Docs/Arquitetura.md) — camadas, padrões, fluxo de uma operação
- [Regras de Negócio](Docs/RegrasDeNegocio.md) — tema, atores, regras
- [DER](Docs/DER.md) — diagrama entidade-relacionamento em texto

Esses três documentos cobrem boa parte da exigência documental do trabalho.
