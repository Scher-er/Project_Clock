# Arquitetura do Sistema

## Visão geral

O sistema segue o padrão **MVC + Service Layer + DAO** com inversão de dependência via DI nativo do .NET MAUI.

```
┌─────────────┐    ┌──────────────┐    ┌──────────┐    ┌──────┐    ┌────────┐
│  View       │ →  │  Controller  │ →  │ Service  │ →  │ DAO  │ →  │ Banco  │
│  (XAML)     │ ←  │              │ ←  │          │ ←  │      │ ←  │        │
└─────────────┘    └──────────────┘    └──────────┘    └──────┘    └────────┘
```

Cada camada conhece apenas a imediatamente abaixo. A View **nunca** chama o DAO diretamente.

## Camadas

### View (`/Views`)
Páginas XAML do MAUI. Sua única responsabilidade é capturar a interação do usuário e exibir resultados. Não contém regras de negócio nem acesso a banco.

### Controller (`/Controllers`)
Recebe a chamada da View, valida entradas básicas, orquestra um ou mais Services e devolve um `ResultadoOperacao<T>` padronizado. Toda Controller implementa `IController` (e `IController<T>` quando faz CRUD).

### Service (`/Services`)
Concentra as regras de negócio. Pode chamar várias DAOs e outros Services. Toda Service implementa `IService`. Exemplos:
- `AuthService` — autenticação, hashing de senha
- `BalancoCalculoService` (Fase 4) — totalizadores, validação Ativo = Passivo + PL
- `PdfParserService` (Fase 5) — extração de balanço de PDFs
- `ExcelExportService` (Fase 6) — geração de planilhas
- `LogService` (Fase 3) — orquestra escrita em MongoDB + TXT

### DAO (`/DAO`)
Persistência. Cada DAO é específica de uma fonte de dados e uma entidade. Todas implementam `IDao<T>`.
- `/DAO/MySQL/` — banco principal (Empresa, Balanço, etc)
- `/DAO/SQLite/` — configurações locais
- `/DAO/MongoDB/` — logs

### Model (`/Models`)
POCOs puros — apenas dados, sem comportamento de negócio relevante. Compartilhados por todas as camadas.

## Padrões aplicados

| Padrão | Onde aparece |
|---|---|
| **Dependency Injection** | `MauiProgram.cs` registra tudo; cada classe recebe dependências via construtor |
| **Factory** | `MySqlConnectionFactory`, `SqliteConnectionFactory`, `MongoConnectionFactory` — encapsulam criação de conexão |
| **Repository / DAO** | Todas as `*Dao.cs` |
| **Result Object** | `ResultadoOperacao<T>` — evita throw para fluxo de controle |
| **Interface Segregation** | `IDao`, `IController`, `IService` separadas; cada concreto pode adicionar contratos próprios |
| **Single Responsibility** | Cada Service tem um foco (Auth, Parser, Export, Log...) |

## POO em destaque

- **Encapsulamento** — propriedades públicas com `init`/`private set` quando faz sentido (`SessaoUsuario.UsuarioAtual`)
- **Herança** — `IController<T> : IController`, `LoginPage : ContentPage`
- **Polimorfismo** — todas as DAOs trocáveis por terem o mesmo contrato `IDao<T>`
- **Abstração** — Views dependem da interface do Controller (`ILoginController`), não da implementação

## Fluxo de uma operação real (Login)

1. Usuário clica em **ENTRAR** em `LoginPage.xaml`
2. `LoginPage.OnEntrarClicked` chama `ILoginController.AutenticarAsync(login, senha)`
3. `LoginController` repassa pra `IAuthService.AutenticarAsync`
4. (Fase 3) `AuthService` consulta `IUsuarioDao.BuscarPorLoginAsync` e valida hash BCrypt
5. (Fase 3) `AuthService` chama `ILogService.RegistrarAsync(TipoEventoLog.Login, ...)` que grava no MongoDB + TXT
6. `AuthService` chama `ISessaoUsuario.IniciarSessao(usuario)`
7. Retorna `ResultadoOperacao<Usuario>.Ok(usuario)` subindo as camadas até a View
8. A View troca `Application.Current.MainPage` para `AppShell` (entra no app)

## Três bancos, três responsabilidades

| Banco | Uso | Por que esse |
|---|---|---|
| **MySQL** | Dados estruturados — empresas, balanços, contas | Relacional, suporta integridade referencial nativa, ideal para queries analíticas |
| **SQLite** | Configurações locais do app (tema, último filtro, etc) | Zero-config, embarcado no app, perfeito para preferências |
| **MongoDB** | Logs estruturados de eventos | Schema flexível pra capturar detalhes variados de eventos; indexação por data nativa |
