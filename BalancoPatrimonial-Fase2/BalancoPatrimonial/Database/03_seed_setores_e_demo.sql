-- =============================================================================
-- Seed de Setores e Dados Demo
-- Popula:
--   - Setores de atividade (CNAE simplificado)
--   - Usuário admin (senha "admin" — hash BCrypt placeholder, gerar no app)
--   - 1 grupo econômico + 3 empresas + 4 balanços com contas preenchidas
-- =============================================================================

USE balanco_patrimonial;

-- ─── SETORES DE ATIVIDADE ────────────────────────────────────────────────────
INSERT INTO setor_atividade (codigo, nome, descricao) VALUES
('A',      'Agropecuária',                       'Agricultura, pecuária, pesca e aquicultura'),
('B',      'Indústrias Extrativas',              'Extração de minerais, petróleo e gás'),
('C',      'Indústrias de Transformação',        'Manufatura em geral'),
('D',      'Energia',                            'Eletricidade, gás e similares'),
('F',      'Construção',                         'Construção civil e obras'),
('G',      'Comércio',                           'Atacado e varejo'),
('H',      'Transporte e Armazenagem',           'Logística e transporte'),
('I',      'Alojamento e Alimentação',           'Hotéis, restaurantes'),
('J',      'Tecnologia e Comunicação',           'Informação e comunicação'),
('K',      'Atividades Financeiras',             'Bancos, seguros, fundos'),
('L',      'Atividades Imobiliárias',            'Compra, venda, locação de imóveis'),
('M',      'Atividades Profissionais',           'Consultoria, engenharia, jurídico'),
('Q',      'Saúde Humana',                       'Saúde e assistência social'),
('R',      'Artes, Cultura e Esportes',          'Entretenimento, cultura, esporte'),
('S',      'Outros Serviços',                    'Serviços diversos');

-- ─── USUARIO ADMIN ───────────────────────────────────────────────────────────
-- O hash abaixo é o BCrypt da senha "admin" (gerado com BCrypt.Net 4.0).
-- Em produção, gerar via aplicação na criação do usuário, nunca commitar no SQL.
INSERT INTO usuario (nome, login, email, senha_hash, perfil, ativo) VALUES
('Administrador', 'admin', 'admin@banco.local',
 '$2a$11$rXq8KGqHvxgYO6vlOLi3OejL5kRiKTSwTjHTbU.bWtJ7zHfNo7nCu',
 'Administrador', 1);

-- ─── GRUPOS ECONÔMICOS DEMO ──────────────────────────────────────────────────
INSERT INTO grupo_economico (nome, descricao) VALUES
('Grupo Demo Varejo',     'Empresas do segmento varejista usadas para demonstração'),
('Grupo Demo Industrial', 'Empresas industriais usadas para demonstração');

-- ─── EMPRESAS DEMO ───────────────────────────────────────────────────────────
INSERT INTO empresa
    (cnpj, razao_social, nome_fantasia, grupo_economico_id, tipo_empresa,
     rating, limite_credito, uf_atuacao, local_atuacao)
VALUES
('12345678000101', 'Varejo Brasileiro S.A.',          'VarejoBR',
 (SELECT id FROM grupo_economico WHERE nome='Grupo Demo Varejo'),
 1, 'AA',  50000000.00, 'SP', 'São Paulo - SP'),

('98765432000199', 'Indústria Metalúrgica Sigma Ltda', 'Sigma Metal',
 (SELECT id FROM grupo_economico WHERE nome='Grupo Demo Industrial'),
 3, 'BBB', 15000000.00, 'MG', 'Belo Horizonte - MG'),

('11223344000155', 'Comercial Atacadista Delta SA',    'Delta Atacado',
 NULL,
 2, 'A',   8000000.00,  'RS', 'Porto Alegre - RS');

-- ─── VÍNCULOS EMPRESA × SETOR (N:N) ──────────────────────────────────────────
-- VarejoBR atua em G (comércio) como principal e J (tecnologia) secundário
INSERT INTO empresa_setor (empresa_id, setor_id, principal)
SELECT e.id, s.id, 1 FROM empresa e, setor_atividade s
 WHERE e.cnpj='12345678000101' AND s.codigo='G';
INSERT INTO empresa_setor (empresa_id, setor_id, principal)
SELECT e.id, s.id, 0 FROM empresa e, setor_atividade s
 WHERE e.cnpj='12345678000101' AND s.codigo='J';

-- Sigma Metal: C (transformação)
INSERT INTO empresa_setor (empresa_id, setor_id, principal)
SELECT e.id, s.id, 1 FROM empresa e, setor_atividade s
 WHERE e.cnpj='98765432000199' AND s.codigo='C';

-- Delta Atacado: G (comércio) e H (logística)
INSERT INTO empresa_setor (empresa_id, setor_id, principal)
SELECT e.id, s.id, 1 FROM empresa e, setor_atividade s
 WHERE e.cnpj='11223344000155' AND s.codigo='G';
INSERT INTO empresa_setor (empresa_id, setor_id, principal)
SELECT e.id, s.id, 0 FROM empresa e, setor_atividade s
 WHERE e.cnpj='11223344000155' AND s.codigo='H';

-- ─── BALANÇOS DEMO ───────────────────────────────────────────────────────────
-- VarejoBR: 3 balanços (2020, 2021, 2022) — vai aparecer na lista de empresas
-- como "balanços planilhados: 2020, 2021, 2022"
INSERT INTO balanco
    (empresa_id, ano_exercicio, data_referencia, tipo_balanco, usuario_id, origem, moeda, multiplicador_valores)
VALUES
((SELECT id FROM empresa WHERE cnpj='12345678000101'), 2020, '2020-12-31', 2,
 (SELECT id FROM usuario WHERE login='admin'), 'Demo seed', 'BRL', 1000),
((SELECT id FROM empresa WHERE cnpj='12345678000101'), 2021, '2021-12-31', 2,
 (SELECT id FROM usuario WHERE login='admin'), 'Demo seed', 'BRL', 1000),
((SELECT id FROM empresa WHERE cnpj='12345678000101'), 2022, '2022-12-31', 2,
 (SELECT id FROM usuario WHERE login='admin'), 'Demo seed', 'BRL', 1000);

-- Sigma Metal: 1 balanço 2022 individual
INSERT INTO balanco
    (empresa_id, ano_exercicio, data_referencia, tipo_balanco, usuario_id, origem, moeda)
VALUES
((SELECT id FROM empresa WHERE cnpj='98765432000199'), 2022, '2022-12-31', 1,
 (SELECT id FROM usuario WHERE login='admin'), 'Demo seed', 'BRL');

-- ─── CONTAS DO BALANÇO 2022 DA VAREJOBR (exemplo completo) ───────────────────
-- Valores fictícios em R$ (já convertidos da unidade de mil)
SET @bal_id := (SELECT b.id FROM balanco b
                JOIN empresa e ON e.id=b.empresa_id
                WHERE e.cnpj='12345678000101' AND b.ano_exercicio=2022);

INSERT INTO conta_balanco (balanco_id, conta_padrao_id, valor) VALUES
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.01.01'),  3500000.00),  -- Caixa
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.01.02'),  1200000.00),  -- Aplicações
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.01.03.01'), 8400000.00), -- Clientes
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.01.04'),  6700000.00),  -- Estoques
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.01.06'),  500000.00),   -- Trib. Rec.
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.02.02'),  2000000.00),  -- Investimentos
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.02.03'), 15000000.00),  -- Imobilizado
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='1.02.04'),  3200000.00),  -- Intangível
-- Passivo
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.01.02'),  4500000.00),  -- Fornecedores
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.01.04'),  3200000.00),  -- Emp. CP
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.01.01'),   900000.00),  -- Obrig. Trab.
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.01.03'),   600000.00),  -- Obrig. Fisc.
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.02.01'),  8000000.00),  -- Emp. LP
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.02.02'),  1500000.00),  -- Outras LP
-- PL
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.03.01'), 12000000.00),  -- Cap. Social
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.03.04'),  6000000.00),  -- Res. Lucros
(@bal_id, (SELECT id FROM conta_padrao WHERE codigo='2.03.05'),  3800000.00);  -- Luc. Acum.
-- Total Ativo = Total Passivo+PL = 40.500.000,00 ✓
