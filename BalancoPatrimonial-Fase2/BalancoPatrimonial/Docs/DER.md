# Diagrama Entidade-Relacionamento (DER)

## Versão textual

```
┌─────────────────────┐
│  grupo_economico    │
│─────────────────────│
│ PK  id              │
│     nome            │
│     descricao       │
│     data_cadastro   │
│     ativo           │
└──────────┬──────────┘
           │ 1
           │
           │ N
┌──────────▼──────────────────────┐         ┌──────────────────────┐
│  empresa                        │  N    N │  setor_atividade     │
│─────────────────────────────────│ ◀─────▶ │──────────────────────│
│ PK  id                          │  (via   │ PK  id               │
│ UQ  cnpj                        │  empresa│ UQ  codigo           │
│     razao_social                │  _setor)│     nome             │
│     nome_fantasia               │         │     descricao        │
│ FK  grupo_economico_id          │         └──────────────────────┘
│     tipo_empresa                │
│     rating                      │
│     limite_credito              │           ┌──────────────────────┐
│     uf_atuacao                  │           │  empresa_setor       │
│     local_atuacao               │           │──────────────────────│
│     caminho_logo                │           │ PK  empresa_id       │
│     data_cadastro               │           │ PK  setor_id         │
│     ativo                       │           │     principal        │
└─────────────┬───────────────────┘           └──────────────────────┘
              │ 1
              │
              │ N
┌─────────────▼─────────────────────┐         ┌──────────────────────┐
│  balanco                          │  N   1  │  usuario             │
│───────────────────────────────────│ ───────▶│──────────────────────│
│ PK  id                            │         │ PK  id               │
│ FK  empresa_id                    │         │     nome             │
│ FK  usuario_id                    │         │ UQ  login            │
│     ano_exercicio                 │         │ UQ  email            │
│     data_referencia               │         │     senha_hash       │
│     tipo_balanco                  │         │     perfil           │
│     data_planilhamento            │         │     ativo            │
│     origem                        │         │     data_cadastro    │
│     hash_origem_pdf               │         │     ultimo_login     │
│     observacoes                   │         └──────────────────────┘
│     moeda                         │
│     multiplicador_valores         │
│ UQ  (empresa_id,ano,tipo)         │
└─────────────┬─────────────────────┘
              │ 1
              │
              │ N
┌─────────────▼─────────────────────┐         ┌─────────────────────────┐
│  conta_balanco                    │  N   1  │  conta_padrao           │
│───────────────────────────────────│ ───────▶│─────────────────────────│
│ PK  id                            │         │ PK  id                  │
│ FK  balanco_id                    │         │ UQ  codigo              │
│ FK  conta_padrao_id               │         │     descricao           │
│     valor                         │         │     grupo_principal     │
│     descricao_original            │         │ FK  conta_pai_id ───┐   │
│ UQ  (balanco_id,conta_padrao_id)  │         │     nivel           │   │
└───────────────────────────────────┘         │     eh_totalizadora │   │
                                              │     ordem           │   │
                                              │     ativa           │   │
                                              └─────────────────────┼───┘
                                                  ▲                 │
                                                  └─────────────────┘
                                                  (auto-relacionamento 1:N)
```

## Cardinalidades

| Relacionamento | Cardinalidade | Tabela intermediária |
|---|---|---|
| grupo_economico → empresa | 1 : N | — |
| empresa → balanco | 1 : N | — |
| usuario → balanco | 1 : N | — |
| balanco → conta_balanco | 1 : N | — |
| conta_padrao → conta_balanco | 1 : N | — |
| conta_padrao → conta_padrao (auto) | 1 : N | — (mesma tabela) |
| empresa ↔ setor_atividade | **N : N** | **empresa_setor** |

## Regras de integridade

- **`empresa.grupo_economico_id`** — `ON DELETE SET NULL` (excluir grupo não apaga empresas)
- **`balanco.empresa_id`** — `ON DELETE CASCADE` (excluir empresa apaga seus balanços)
- **`balanco.usuario_id`** — `ON DELETE RESTRICT` (não permite excluir usuário com balanços)
- **`conta_balanco.balanco_id`** — `ON DELETE CASCADE` (excluir balanço apaga as contas)
- **`conta_balanco.conta_padrao_id`** — `ON DELETE RESTRICT` (não apaga contas em uso)
- **`empresa_setor.*`** — `ON DELETE CASCADE` em ambos os lados (limpa vínculos automaticamente)

## Constraints únicas adicionais

- `empresa.cnpj` único
- `usuario.login` único
- `usuario.email` único
- `setor_atividade.codigo` único
- `grupo_economico.nome` único
- `conta_padrao.codigo` único
- `(balanco.empresa_id, ano_exercicio, tipo_balanco)` — só 1 balanço por combinação
- `(conta_balanco.balanco_id, conta_padrao_id)` — uma conta não se repete no mesmo balanço
