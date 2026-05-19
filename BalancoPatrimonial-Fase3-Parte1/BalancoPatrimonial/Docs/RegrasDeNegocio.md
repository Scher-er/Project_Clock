# Regras de Negócio

## Tema do sistema

**Análise de balanços patrimoniais para concessão de crédito bancário.**

O sistema é utilizado por analistas e gerentes de um banco para registrar e analisar os balanços patrimoniais publicados pelas empresas clientes (ou potenciais clientes). A partir dos balanços planilhados, o banco define rating e limite de crédito.

## Objetivo

Centralizar e padronizar o registro de balanços patrimoniais, alimentando os processos de:
- Atribuição de rating
- Definição de limite de crédito
- Acompanhamento histórico de empresas

## Atores

| Ator | Permissões |
|---|---|
| **Analista** | Planilhar balanços, consultar empresas, gerar relatórios e exportações |
| **Gerente** | Tudo do Analista + alterar rating e limite |
| **Administrador** | Tudo do Gerente + gerenciar usuários e configurações |

## Entidades principais

### Empresa
- CNPJ único na base
- Pertence (opcionalmente) a um Grupo Econômico
- Atua em um ou mais Setores (relação N:N)
- Possui Rating textual e Limite de Crédito em reais
- Tem histórico de balanços planilhados

### Balanço
- Sempre vinculado a uma empresa e a um ano de exercício
- Pode ser **Individual** (só a empresa) ou **Consolidado** (empresa + controladas)
- Apenas 1 balanço por combinação `empresa + ano + tipo` (regra de unicidade)
- Armazena a moeda original ("BRL", "USD") e o multiplicador (1, 1000, 1000000)
  para rastreabilidade da unidade da DF original

### Plano de Contas Padrão
- Hierárquico em até 4 níveis seguindo o padrão CVM/CPC
- Códigos numéricos hierárquicos (`1.01.03.02`)
- Contas totalizadoras não recebem valor direto — seu valor é a soma das filhas

## Regras computadas

| Regra | Descrição |
|---|---|
| **Ativo Total = Passivo + PL** | Toda balança válida bate por construção. Sistema avisa se desbalanceado (Fase 4) |
| **Multiplicador uniforme** | Dentro de um mesmo balanço, todas as contas usam o mesmo multiplicador. Conversão pra unidade base (R$) ocorre na importação |
| **Histórico imutável** | Balanço uma vez salvo só pode ser editado por quem planilhou ou por Gerente+. Atualizações geram log de alteração |
| **Detecção automática de tipo** | Parser de PDF (Fase 5) tenta detectar se o documento é Consolidado ou Individual analisando títulos e cabeçalhos |

## Fontes de dados aceitas

1. **PDF público** — DFP/ITR da CVM ou relatórios anuais das empresas
2. **Planilhamento manual** — quando o PDF não é parseável automaticamente
3. **Área de testes** — planilhamento volátil que **não persiste** no banco, apenas para conferência e exportação local em Excel

## Saídas do sistema

| Saída | Formato | Para que |
|---|---|---|
| Visualização no app | UI | Conferência rápida |
| Planilha Excel | `.xlsx` | Distribuição, análise externa |
| JSON | `.json` | Persistência no MySQL + integração futura |
| Relatório PDF | `.pdf` | Documento formal para arquivo/cliente |
| Logs XML | `.xml` | Auditoria, exigência regulatória |

## Logs e auditoria

Todo evento relevante é registrado:
- Login / Logout
- Inclusão, alteração, exclusão de balanços e empresas
- Importação de PDF e detecção de duplicidade (via hash)
- Erros de parsing
- Exportações realizadas

Logs ficam em **MongoDB** (estruturados, queryable) e replicados em **TXT diário** (resiliência e leitura humana). Exportação XML sob demanda na página Logs.

## Configurações persistidas (SQLite)

- Tema da interface (claro/escuro)
- Último usuário autenticado (pra pré-preencher login)
- Último filtro usado na página Empresas
- Último termo de pesquisa
- Idioma da interface

## Pendências de regra (a definir nas próximas fases)

- Política de re-importação: rejeitar duplicidade pelo hash do PDF? Sobrescrever? Versionar?
- Limites de armazenamento de logs: política de retenção (90 dias? 1 ano?)
- Aprovação de balanços por gerente antes de afetar rating?
