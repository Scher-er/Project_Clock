# Balanço Patrimonial — Análise de Crédito

Software de análise de balanços patrimoniais de empresas para fins de análise de crédito bancária.

> **Status atual:** Fase 1 — Skeleton + Sidebar + Login.
> As fases seguintes adicionam banco de dados, parser de PDF, exports etc.

---

## Stack

- **.NET 8** + **.NET MAUI**
- **C# 12** com `nullable` e `implicit usings`
- **Arquitetura:** MVC + Service Layer + DAO
- **Bancos (próximas fases):** MySQL (principal), SQLite (config local), MongoDB (logs)

## Como rodar (Fase 1)

1. Garanta que o workload MAUI está instalado:
   ```bash
   dotnet workload install maui
   ```
2. Restaure pacotes:
   ```bash
   dotnet restore
   ```
3. Rode na sua plataforma de preferência:
   ```bash
   # Windows
   dotnet build -t:Run -f net8.0-windows10.0.19041.0
   # Android (precisa emulador rodando)
   dotnet build -t:Run -f net8.0-android
   ```

## Login de teste

- **Usuário:** `admin`
- **Senha:** `admin`

(Hardcoded em `AuthService`. Será movido pro MySQL na Fase 3.)

## Estrutura de pastas

```
BalancoPatrimonial.App/
├── Interfaces/         # IDao, IController, IService — contratos base
├── Models/             # POCOs de domínio
├── Controllers/        # Camada de controle (recebe das Views, orquestra Services)
├── Services/           # Regras de negócio
├── DAO/                # Acesso a dados
│   ├── MySQL/          # Persistência principal
│   ├── SQLite/         # Configurações locais
│   └── MongoDB/        # Logs estruturados
├── Views/              # Páginas XAML (LoginPage + as 4 do sidebar)
├── Helpers/            # Utilitários (futuro: PdfReader, XmlExporter, etc)
└── Resources/Styles/   # Cores e estilos globais
```

## Fluxo de uma operação típica (padrão MVC + Service)

```
View (botão clicado)
   │
   ▼
Controller (LoginController)
   │
   ▼
Service (AuthService)      ←── regras de negócio, validações
   │
   ▼
DAO (UsuarioDAO)           ←── persistência (futuro)
   │
   ▼
Banco (MySQL / SQLite / MongoDB)
```

A View **nunca** acessa o DAO diretamente. Sempre passa pelo Controller → Service.

## Próximas fases

- [ ] **Fase 2:** Models de domínio (Balanço, Conta, Empresa, GrupoEconomico) + scripts MySQL + conexões com os 3 bancos
- [ ] **Fase 3:** CRUD da página Empresas com persistência real
- [ ] **Fase 4:** Planilhamento manual de balanço
- [ ] **Fase 5:** Parser de PDF (PdfPig)
- [ ] **Fase 6:** Exports — Excel (ClosedXML), JSON, XML de logs, PDF de relatório (QuestPDF)
- [ ] **Fase 7:** Gráficos (LiveCharts2) + Área de Testes + ajustes finais
