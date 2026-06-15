# Sistema de Análise de Balanços Patrimoniais

Aplicativo desktop (Windows) em **C# / .NET MAUI** para análise de crédito de empresas a partir de seus balanços patrimoniais. Permite cadastrar empresas, lançar balanços (manualmente ou importando o PDF com auxílio de IA), calcular indicadores econômico-financeiros, gerar relatórios e gráficos e produzir uma avaliação de risco de crédito.



---

## Sumário

1. [Tecnologias](#tecnologias)
2. [Pré-requisitos](#pré-requisitos)
3. [Passo a passo para rodar](#passo-a-passo-para-rodar)
4. [Acesso ao sistema](#acesso-ao-sistema)
5. [Configuração da IA (Google Gemini)](#configuração-da-ia-google-gemini)
6. [O que o sistema faz, como e onde](#o-que-o-sistema-faz-como-e-onde)
7. [Verificação rápida das conexões](#verificação-rápida-das-conexões)
8. [Testes automatizados](#testes-automatizados)
9. [Solução de problemas](#solução-de-problemas)
10. [Estrutura do projeto](#estrutura-do-projeto)

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Linguagem / Framework | C#, .NET 8, .NET MAUI (Windows) |
| Banco relacional | MySQL |
| Banco local | SQLite |
| Banco NoSQL (logs) | MongoDB |
| Arquitetura | MVC + Service Layer + DAO |
| Relatórios / Gráficos | QuestPDF, LiveCharts2 |
| IA (leitura de PDF) | Google Gemini |

---

## Pré-requisitos

Instale, **antes de tudo**, na máquina onde o projeto será avaliado:

1. **Visual Studio 2022** (17.8 ou superior) com a carga de trabalho **"Desenvolvimento de aplicativos .NET Multi-plataforma (MAUI)"** marcada na instalação.
   - Isso já traz o **.NET 8 SDK**. (Caso use só linha de comando, instale o .NET 8 SDK e rode `dotnet workload install maui`.)
2. **MySQL Server 8.x** (com o MySQL Workbench, para rodar os scripts).
3. **MongoDB Community Server 6.x ou 7.x** — e deixe o **serviço rodando** (porta padrão `27017`).

> O **SQLite não precisa ser instalado**: ele é embarcado no aplicativo e o arquivo local é criado automaticamente na primeira execução.

O projeto-alvo de execução é **Windows Machine** (o app é compilado para `net8.0-windows`).

---

## Passo a passo para rodar

### 1. Clonar o repositório

```bash
git clone <url-deste-repositorio>
```

Abra o arquivo **`BalancoPatrimonial.sln`** no Visual Studio.

### 2. Criar o banco MySQL

Abra o MySQL Workbench e execute os scripts da pasta **`Database/`** **na ordem abaixo**, uma vez cada:

| Ordem | Script | O que faz |
|---|---|---|
| 1 | `01_schema.sql` | Cria o banco `balanco_patrimonial` e as 8 tabelas |
| 2 | `02_seed_plano_contas.sql` | Popula o plano de contas padrão |
| 3 | `03_seed_setores_e_demo.sql` | Setores, **usuário admin** e dados de demonstração |
| 4 | `04_migracao_soft_delete.sql` | Exclusão lógica de balanços |
| 5 | `05_migracao_empresa_softdelete.sql` | Exclusão lógica de empresas |
| 6 | `06_dre.sql` | Tabela da DRE (Demonstração do Resultado) |

> O `01_schema.sql` já inclui o `CREATE DATABASE`, então não é preciso criar o banco manualmente.

### 3. Iniciar o MongoDB

O MongoDB é usado para os **logs do sistema**. Basta ter o **serviço rodando** na porta `27017` — **não há script para rodar**: o banco (`balanco_logs`) e as coleções (`logs`, `analises_ia`) são criados automaticamente na primeira gravação.

- No Windows, o instalador normalmente cadastra o MongoDB como serviço já iniciado. Para conferir: abra **Serviços** (`services.msc`) e verifique se **"MongoDB Server"** está **Em execução**.

### 4. SQLite — nada a fazer

O arquivo local (`balanco_local.db3`) e sua tabela de configurações são criados **automaticamente** pelo aplicativo na primeira execução, na pasta de dados do app. Nenhuma ação é necessária.

### 5. Configurar as credenciais (IMPORTANTE)

Abra o arquivo **`BalancoPatrimonial.App/appsettings.json`** e confira os dados de conexão:

```json
{
  "MySql": {
    "Server": "127.0.0.1",
    "Port": 3306,
    "Database": "balanco_patrimonial",
    "User": "root",
    "Password": "Comput2026"
  },
  "Mongo": {
    "ConnectionString": "mongodb://127.0.0.1:27017",
    "Database": "balanco_logs"
  }
}
```

> ⚠️ **Ponto de atenção mais comum:** o campo **`Password`** precisa ser igual à senha do seu MySQL local. O projeto vem configurado com a senha `Comput2026`. **Se o seu MySQL usa outra senha (ou o usuário não é `root`), edite este arquivo** antes de rodar — caso contrário o app não conecta ao banco. O mesmo vale se o MongoDB não estiver na porta padrão.

### 6. Compilar e executar

1. No Visual Studio, selecione o destino **Windows Machine**.
2. **Build → Clean Solution** e depois **Build → Rebuild Solution** (a primeira restauração de pacotes NuGet pode demorar um pouco).
3. Pressione **F5** para executar.

---

## Acesso ao sistema

Na tela de login, use as credenciais padrão criadas pelo seed:

- **Usuário:** `admin`
- **Senha:** `admin`

---

## Configuração da IA (Google Gemini)

> **A chave de API do Gemini NÃO acompanha o projeto.** Ela é armazenada de forma **criptografada no próprio computador** (SecureStorage do Windows), e não fica no código, no `appsettings.json`, nem no banco de dados. Isso é proposital: evita expor uma chave secreta em um repositório público. Por isso, **ao baixar o projeto, nenhuma chave vem junto** — quem for executar precisa configurar a sua própria.

A IA é usada em **um** recurso específico: importar um **PDF de balanço** e extrair as contas automaticamente. **Todo o restante do sistema funciona sem ela** — os balanços podem ser lançados manualmente, e o banco já vem com dados de demonstração.

**Como configurar (leva 1 minuto):**

1. Obtenha uma chave gratuita em **https://aistudio.google.com/apikey** (requer apenas uma conta Google).
2. No app, abra o menu lateral → **"Área de Testes"** → seção **"Google Gemini"**.
3. Cole a chave no campo **"API Key (AIza...)"**, escolha o **Modelo** (o padrão `gemini-3.5-flash` é gratuito) e clique em **Salvar**.
4. Clique em **Testar** para confirmar que a chave está válida.

Feito isso, a opção de importar PDF por IA (na tela de Planilhamento) passa a funcionar.

> 💡 Como a demonstração da IA depende dessa configuração, ela também é mostrada funcionando no **vídeo de apresentação** do trabalho.

---

## O que o sistema faz, como e onde

O sistema cobre o ciclo completo de análise de crédito por balanços. Abaixo, cada funcionalidade e a tela (menu lateral) onde encontrá-la.

### Login
Autenticação de acesso, com senha protegida por **hash BCrypt**. Cada login/logout é registrado nos logs.

### Planilhamento *(menu "Planilhamento")*
É onde se **registra um balanço**. O usuário escolhe a empresa, o ano e o tipo (Individual ou Consolidado) e preenche os valores das contas, organizadas pelo **plano de contas padrão**. Há duas formas de preencher:
- **Manualmente**, digitando os valores.
- **Importando um PDF** do balanço: a **IA do Gemini** lê o documento e extrai as contas; em seguida abre uma **tela de revisão** comparando o que foi extraído com os subtotais impressos. Se já existir um balanço para aquela empresa/ano/tipo, o sistema avisa e oferece **substituir**.

### Empresas *(menu "Empresas")*
**CRUD completo de empresas**: cadastrar, editar e excluir (exclusão lógica — o registro é preservado no banco). Inclui **pesquisa e filtros** e o botão **"Importar JSON"**, que traz um balanço de um arquivo `.json` para o banco (com validação e tratamento de erros). O cadastro de empresa também relaciona **grupo econômico** (1:N) e **setores de atividade** (N:N), com validações de formulário.

### Detalhe do Balanço *(ao tocar em um balanço de uma empresa)*
Mostra o **balanço salvo** em duas abas: **Balanço Patrimonial** (contas agrupadas em Ativo, Passivo e PL, editáveis) e **DRE** (Demonstração do Resultado). Permite **exportar** o balanço em **PDF**, **Excel** e **JSON**, e excluir. A lista de balanços de cada empresa traz um selo **"✓ Fecha / ⚠ Não fecha"** indicando se o balanço está equilibrado.

### Análises *(menu "Análises")*
O coração da análise de crédito. A partir dos balanços de uma empresa, calcula e exibe:
- **Indicadores**: liquidez (corrente, seca, imediata, geral), endividamento, estrutura de capital e, quando há DRE, **rentabilidade** (margens, ROE, ROA).
- **Análise vertical e horizontal**.
- **Avaliação de risco**: um **score de 0 a 100** e uma **classe de A a D**, com parecer — gerado por regras determinísticas e, opcionalmente, um **parecer redigido pela IA**.
- **Gráficos** (LiveCharts2): evolução dos índices ano a ano e a composição do Ativo e da origem dos recursos (gráficos de pizza).

### Comparar *(menu "Comparar")*
Coloca **duas empresas lado a lado**, comparando seus indicadores para apoiar a decisão.

### Logs *(menu "Logs")*
Lista os **logs de auditoria** gravados no **MongoDB** (login, inclusões, alterações, exclusões, importações e erros, com data/hora e usuário). Permite **exportar os logs em XML**.

### Área de Testes *(menu "Área de Testes")*
Tela utilitária que **testa as conexões** dos três bancos, **configura a chave da IA** (acima) e gera logs de exemplo.

### Recursos por trás das telas
- **Três bancos de dados**: **MySQL** guarda os dados principais; **SQLite** guarda as **configurações locais** (tema, última pesquisa/filtro), carregadas automaticamente; **MongoDB** guarda os **logs**.
- **Relatórios e exportações**: PDF (QuestPDF), Excel (ClosedXML), JSON (importação/exportação) e XML (logs).
- **Imagens**: logotipo na tela de login e ícones do app.
- **Arquitetura**: MVC + camada de Service + DAO, com interfaces base (IDao, IController, IService) e injeção de dependência.

---

## Verificação rápida das conexões

Antes de explorar o sistema, abra no menu lateral a tela **"Área de Testes"**. Ela testa as três conexões (MySQL, SQLite e MongoDB) e mostra o status de cada uma. **Se as três aparecerem como conectadas, todo o resto do sistema funcionará normalmente.** É a forma mais rápida de confirmar que o ambiente está correto.

---

## Testes automatizados

O projeto inclui testes unitários (xUnit) cobrindo o cálculo de indicadores e a avaliação de risco, no projeto **`BalancoPatrimonial.Tests`**.

- No Visual Studio: menu **Test → Run All Tests**.
- Ou via terminal, na pasta da solution:

```bash
dotnet test
```

---

## Solução de problemas

| Sintoma | Causa provável | Solução |
|---|---|---|
| App abre mas dá erro ao logar ou listar empresas | MySQL não conecta | Confirme que o serviço do MySQL está rodando, que os scripts foram executados e que a **senha no `appsettings.json`** bate com a do seu MySQL. |
| Login funciona, mas os **logs** não aparecem | MongoDB não está rodando | Inicie o serviço do MongoDB (porta `27017`). O app **não trava** sem ele, mas os logs só são gravados com o Mongo no ar. |
| Pequena lentidão (alguns segundos) ao executar ações | MongoDB ausente/parado | É o tempo de espera (limitado a ~3s) até o app desistir de gravar o log. Inicie o MongoDB para eliminar. |
| Erros de pacote/NuGet ao compilar | Restauração incompleta ou workload MAUI ausente | Confirme a carga de trabalho **MAUI** no Visual Studio e rode **Rebuild** (restaura os NuGets). |
| Não aparece o destino "Windows Machine" | Workload MAUI/Windows não instalada | Instale a carga **.NET MAUI** pelo Visual Studio Installer. |

> Observação: o aplicativo foi desenvolvido e testado para **Windows**. A leitura de PDF por IA (Google Gemini) é **opcional** e exige configurar uma chave própria — veja a seção [Configuração da IA](#configuração-da-ia-google-gemini). Ela **não é necessária** para avaliar as demais funcionalidades, já que os balanços podem ser lançados manualmente ou pelos dados de demonstração do seed.

---

## Estrutura do projeto

```
.
├─ BalancoPatrimonial.sln            Solução (abrir esta)
├─ BalancoPatrimonial.App/           Aplicativo MAUI
│  ├─ Models/                        Entidades de domínio e enums
│  ├─ Views/                         Telas (XAML) e code-behind
│  ├─ Controllers/                   Orquestram as Views
│  ├─ Services/                      Regras de negócio
│  ├─ DAO/                           Acesso a dados (MySQL, SQLite, Mongo)
│  ├─ Interfaces/                    Interfaces base (IDao, IController, IService)
│  ├─ Resources/                     Imagens e estilos
│  └─ appsettings.json               Conexões de banco
├─ BalancoPatrimonial.Tests/         Testes unitários (xUnit)

```

---

### Resumo do que o avaliador precisa instalar

- ✅ **Visual Studio 2022 + workload .NET MAUI** (traz o .NET 8)
- ✅ **MySQL** — rodar os 6 scripts e conferir a senha no `appsettings.json`
- ✅ **MongoDB** — apenas manter o serviço rodando (porta 27017)
- ✅ **SQLite** — nada a fazer (automático)
- ⚙️ **Chave do Google Gemini** — *opcional*, só para a importação de PDF por IA (configurada dentro do app; veja a seção da IA)
