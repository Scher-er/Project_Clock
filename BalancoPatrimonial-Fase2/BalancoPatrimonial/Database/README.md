# Scripts SQL — Banco Principal (MySQL)

Ordem de execução:

1. `01_schema.sql` — cria o banco `balanco_patrimonial` e todas as 8 tabelas
2. `02_seed_plano_contas.sql` — popula o plano de contas padrão (CVM/CPC) com hierarquia completa
3. `03_seed_setores_e_demo.sql` — popula setores CNAE, cria usuário admin, e insere 3 empresas demo com balanços

## Como executar

### Via linha de comando

```bash
mysql -u root -p < 01_schema.sql
mysql -u root -p balanco_patrimonial < 02_seed_plano_contas.sql
mysql -u root -p balanco_patrimonial < 03_seed_setores_e_demo.sql
```

### Via MySQL Workbench / DBeaver

Abra cada arquivo na ordem e execute (Ctrl+Shift+Enter para script inteiro).

## Validação rápida

Depois de rodar, espera-se:

```sql
SELECT COUNT(*) FROM conta_padrao;      -- ~ 35 contas
SELECT COUNT(*) FROM setor_atividade;   -- 15 setores
SELECT COUNT(*) FROM empresa;           -- 3 empresas demo
SELECT COUNT(*) FROM balanco;           -- 4 balanços (3 da VarejoBR + 1 da Sigma)
SELECT COUNT(*) FROM conta_balanco;     -- 17 contas preenchidas (balanço 2022 da VarejoBR)

-- Confere se Ativo Total = Passivo + PL no balanço 2022 da VarejoBR
SELECT
    SUM(CASE WHEN cp.grupo_principal = 1 THEN cb.valor ELSE 0 END) AS ativo,
    SUM(CASE WHEN cp.grupo_principal IN (2,3) THEN cb.valor ELSE 0 END) AS passivo_pl
FROM conta_balanco cb
JOIN conta_padrao cp ON cp.id = cb.conta_padrao_id
JOIN balanco b ON b.id = cb.balanco_id
WHERE b.ano_exercicio = 2022
  AND b.empresa_id = (SELECT id FROM empresa WHERE cnpj='12345678000101');
-- Deve retornar ativo = passivo_pl = 40500000.00
```

## Mapa de relacionamentos

```
grupo_economico ─1───N─ empresa ─1───N─ balanco ─1───N─ conta_balanco ─N───1─ conta_padrao
                          │                  │                                      │
                          │                  │                                      └── auto-ref (conta_pai_id)
                          │                  └── N───1── usuario
                          │
                          └── N───N ── setor_atividade  (via empresa_setor)
```

**Atende os requisitos do trabalho:**
- ✅ 8 tabelas (mínimo: 5)
- ✅ Chave primária em todas
- ✅ Chave estrangeira com integridade referencial (ON DELETE/UPDATE)
- ✅ Relacionamento 1:N (vários)
- ✅ Relacionamento N:N com tabela intermediária (`empresa_setor`)

## Credenciais demo

- **Usuário:** `admin`
- **Senha:** `admin`

O hash BCrypt no SQL é um placeholder. Para garantir que funcione, recrie via aplicação na Fase 3 ou substitua o `senha_hash` por outro gerado com `BCrypt.Net.BCrypt.HashPassword("admin")`.
