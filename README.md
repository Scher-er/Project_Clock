# Project Clock: Balanço Patrimonial & Análise de Crédito

Bem-vindo ao repositório do **Project Clock**, um sistema completo e moderno para controle, planilhamento e análise de crédito corporativo baseado em balanços patrimoniais.

Este projeto foi arquitetado para fornecer uma experiência fluida, ágil e altamente responsiva para instituições financeiras, focado em estabilidade e na adoção das melhores práticas do ecossistema .NET.

---

## 🚀 O que há de novo na Versão 2.0 (MVVM & IA)

O projeto passou por uma refatoração arquitetural profunda, abandonando o padrão procedural antigo em favor de uma **arquitetura MVVM pura**, focada em performance e separação de responsabilidades.

* **Arquitetura MVVM:** Todas as telas agora utilizam o CommunityToolkit.Mvvm para binding declarativo, commands assíncronos e reatividade total sem necessidade de código no code-behind (exceto inicialização).
* **Alta Performance com Dapper:** O acesso ao MySQL foi migrado do Entity Framework para o **Dapper (Micro-ORM)**.
* **Componentes Nativos (CollectionView):** A tela principal de Planilhamento agora renderiza e virtualiza balanços dinâmicos lateralmente usando nativamente as APIS do MAUI, eliminando completamente travamentos.
* **Inteligência Artificial (Lote):** Integração com **Google Gemini** para ler planilhas e PDFs com extração automática. A V2 suporta **Importação em Lote Assíncrona** (Task.WhenAll) disparando dezenas de interpretações de OCR em paralelo.
* **Integração B3 (Mock):** Automação para puxar dados das empresas abertas da CVM/B3 digitando apenas o Ticker da ação.
* **Dashboard e Analytics:** Dashboard dinâmico integrado, acompanhado do módulo de **Análise Setorial**, que permite comparar balanços e indicadores de múltiplas empresas.
* **Exportação Avançada:** Geração de arquivos em **PDF, Excel, JSON** e um módulo arquitetural projetado para **Apresentações PowerPoint (.pptx)** automático.

---

## 💻 Como configurar e rodar

### 1. Requisitos
- **Visual Studio 2022** com a carga de trabalho **.NET MAUI** instalada (.NET 8.0+).
- **MySQL Server** rodando (porta padrão 3306).
- **MongoDB** rodando localmente (porta padrão 27017 - utilizado de forma não-bloqueante para os logs de auditoria).
- **SQLite** é embarcado e criado automaticamente.

### 2. Banco de Dados (MySQL)
Execute os scripts SQL disponíveis na pasta Scripts/ no seu MySQL.
Em seguida, certifique-se de ajustar a connection string no arquivo ppsettings.json localizado na raiz do projeto BalancoPatrimonial.App.

### 3. Configuração da Inteligência Artificial (Google Gemini)
O uso do módulo de IA requer uma chave de API:
1. Obtenha uma chave gratuita no [Google AI Studio](https://aistudio.google.com/apikey).
2. Rode o app, abra o menu lateral -> **"Área de Testes"**.
3. Na seção "Google Gemini", cole sua chave e clique em **Salvar**.
4. A aplicação salvará sua configuração localmente (via SQLite) e desbloqueará as opções de **Importação por IA (Lote)** no Planilhamento.

---

## 📂 Estrutura do Projeto

* Models/: Classes de domínio (Empresa, Balanco, ContaPadrao, etc).
* ViewModels/: A lógica reativa e de controle de estado das views (MVVM Toolkit).
* Views/: Telas e componentes XAML puros.
* Services/: Camada de regras de negócio pesada, integrações de APIs (Gemini) e exportações (QuestPDF, ClosedXML).
* DAO/: Camada de abstração de acesso a dados (MySQL com Dapper, Mongo, SQLite).
* Controllers/: Camada orquestradora (legado da V1, sendo encapsulada pelos ViewModels).

---

## 🛠️ Tecnologias e Bibliotecas Empregadas

* **.NET MAUI** (.NET 8.0)
* **CommunityToolkit.Mvvm** (Padrão MVVM Moderno e Reativo)
* **Dapper** (Acesso de dados de alta performance)
* **LiveCharts2** (Gráficos financeiros de Análise e Setorial)
* **QuestPDF & ClosedXML** (Geração de relatórios robustos em PDF e Excel)
* **Google.GenerativeAI** (SDK do Gemini para análise de Balanços não estruturados em PDF)
* **MongoDB.Driver** (Logs de auditoria não-bloqueantes)

---

Desenvolvido para demonstração de Arquitetura de Software e Engenharia de Aplicações de Alta Disponibilidade.
