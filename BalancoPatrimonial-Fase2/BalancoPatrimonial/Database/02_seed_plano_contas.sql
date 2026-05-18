-- =============================================================================
-- Seed do Plano de Contas Padrão (CVM/CPC)
-- Estrutura hierárquica em 4 níveis baseada nas DFs públicas brasileiras.
--
-- Convenções:
--   grupo_principal: 1=Ativo, 2=Passivo, 3=PatrimônioLíquido
--   eh_totalizadora: contas que somam filhas (não recebem valor direto)
--   nivel: profundidade na hierarquia (1 a 4)
-- =============================================================================

USE balanco_patrimonial;

-- Importante: a ordem de INSERT respeita a hierarquia (pais antes dos filhos)
-- pra que conta_pai_id possa referenciar IDs já existentes.

-- ─── NÍVEL 1: GRUPOS PRINCIPAIS ─────────────────────────────────────────────
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem) VALUES
('1',    'ATIVO TOTAL',                1, NULL, 1, 1,  1),
('2',    'PASSIVO E PATRIMÔNIO LÍQUIDO', 2, NULL, 1, 1, 100);

-- ─── NÍVEL 2: SUBGRUPOS ────────────────────────────────────────────────────
-- Pegamos os IDs dos grupos recém-inseridos via subquery
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01', 'Ativo Circulante',          1, id, 2, 1,  2 FROM conta_padrao WHERE codigo='1';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02', 'Ativo Não Circulante',      1, id, 2, 1, 20 FROM conta_padrao WHERE codigo='1';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.01', 'Passivo Circulante',        2, id, 2, 1, 101 FROM conta_padrao WHERE codigo='2';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.02', 'Passivo Não Circulante',    2, id, 2, 1, 120 FROM conta_padrao WHERE codigo='2';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.03', 'Patrimônio Líquido',        3, id, 2, 1, 140 FROM conta_padrao WHERE codigo='2';

-- ─── NÍVEL 3: CONTAS SINTÉTICAS ─────────────────────────────────────────────

-- Ativo Circulante (1.01.xx)
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.01', 'Caixa e Equivalentes de Caixa', 1, id, 3, 0,  3 FROM conta_padrao WHERE codigo='1.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.02', 'Aplicações Financeiras', 1, id, 3, 0,  4 FROM conta_padrao WHERE codigo='1.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.03', 'Contas a Receber', 1, id, 3, 1,  5 FROM conta_padrao WHERE codigo='1.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.04', 'Estoques', 1, id, 3, 0,  8 FROM conta_padrao WHERE codigo='1.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.05', 'Ativos Biológicos', 1, id, 3, 0,  9 FROM conta_padrao WHERE codigo='1.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.06', 'Tributos a Recuperar', 1, id, 3, 0, 10 FROM conta_padrao WHERE codigo='1.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.07', 'Despesas Antecipadas', 1, id, 3, 0, 11 FROM conta_padrao WHERE codigo='1.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.08', 'Outros Ativos Circulantes', 1, id, 3, 0, 12 FROM conta_padrao WHERE codigo='1.01';

-- Ativo Não Circulante (1.02.xx)
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02.01', 'Ativo Realizável a Longo Prazo', 1, id, 3, 1, 21 FROM conta_padrao WHERE codigo='1.02';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02.02', 'Investimentos', 1, id, 3, 0, 25 FROM conta_padrao WHERE codigo='1.02';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02.03', 'Imobilizado', 1, id, 3, 0, 26 FROM conta_padrao WHERE codigo='1.02';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02.04', 'Intangível', 1, id, 3, 0, 27 FROM conta_padrao WHERE codigo='1.02';

-- Passivo Circulante (2.01.xx)
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.01.01', 'Obrigações Sociais e Trabalhistas', 2, id, 3, 0, 102 FROM conta_padrao WHERE codigo='2.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.01.02', 'Fornecedores', 2, id, 3, 0, 103 FROM conta_padrao WHERE codigo='2.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.01.03', 'Obrigações Fiscais', 2, id, 3, 0, 104 FROM conta_padrao WHERE codigo='2.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.01.04', 'Empréstimos e Financiamentos', 2, id, 3, 0, 105 FROM conta_padrao WHERE codigo='2.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.01.05', 'Outras Obrigações', 2, id, 3, 0, 106 FROM conta_padrao WHERE codigo='2.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.01.06', 'Provisões', 2, id, 3, 0, 107 FROM conta_padrao WHERE codigo='2.01';

-- Passivo Não Circulante (2.02.xx)
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.02.01', 'Empréstimos e Financiamentos LP', 2, id, 3, 0, 121 FROM conta_padrao WHERE codigo='2.02';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.02.02', 'Outras Obrigações LP', 2, id, 3, 0, 122 FROM conta_padrao WHERE codigo='2.02';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.02.03', 'Tributos Diferidos', 2, id, 3, 0, 123 FROM conta_padrao WHERE codigo='2.02';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.02.04', 'Provisões LP', 2, id, 3, 0, 124 FROM conta_padrao WHERE codigo='2.02';

-- Patrimônio Líquido (2.03.xx)
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.03.01', 'Capital Social Realizado', 3, id, 3, 0, 141 FROM conta_padrao WHERE codigo='2.03';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.03.02', 'Reservas de Capital', 3, id, 3, 0, 142 FROM conta_padrao WHERE codigo='2.03';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.03.03', 'Reservas de Reavaliação', 3, id, 3, 0, 143 FROM conta_padrao WHERE codigo='2.03';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.03.04', 'Reservas de Lucros', 3, id, 3, 0, 144 FROM conta_padrao WHERE codigo='2.03';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.03.05', 'Lucros/Prejuízos Acumulados', 3, id, 3, 0, 145 FROM conta_padrao WHERE codigo='2.03';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '2.03.06', 'Ajustes de Avaliação Patrimonial', 3, id, 3, 0, 146 FROM conta_padrao WHERE codigo='2.03';

-- ─── NÍVEL 4: CONTAS ANALÍTICAS (sub-subcontas mais detalhadas) ──────────────

-- Contas a Receber (1.01.03.xx)
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.03.01', 'Clientes', 1, id, 4, 0, 6 FROM conta_padrao WHERE codigo='1.01.03';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.01.03.02', 'Outras Contas a Receber', 1, id, 4, 0, 7 FROM conta_padrao WHERE codigo='1.01.03';

-- Realizável LP (1.02.01.xx)
INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02.01.01', 'Contas a Receber LP', 1, id, 4, 0, 22 FROM conta_padrao WHERE codigo='1.02.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02.01.02', 'Aplicações Financeiras LP', 1, id, 4, 0, 23 FROM conta_padrao WHERE codigo='1.02.01';

INSERT INTO conta_padrao (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem)
SELECT '1.02.01.03', 'Tributos Diferidos LP', 1, id, 4, 0, 24 FROM conta_padrao WHERE codigo='1.02.01';
