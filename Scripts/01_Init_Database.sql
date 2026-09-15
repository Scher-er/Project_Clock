CREATE DATABASE IF NOT EXISTS balanco_patrimonial
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE balanco_patrimonial;

-- 1. grupo_economico
CREATE TABLE IF NOT EXISTS grupo_economico (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    descricao TEXT NULL,
    data_cadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 2. setor_atividade
CREATE TABLE IF NOT EXISTS setor_atividade (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL,
    nome VARCHAR(255) NOT NULL,
    descricao TEXT NULL
);

-- 3. usuario
CREATE TABLE IF NOT EXISTS usuario (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    login VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL,
    senha_hash VARCHAR(255) NOT NULL,
    perfil VARCHAR(50) NOT NULL DEFAULT 'Analista',
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    data_cadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ultimo_login DATETIME NULL
);

-- 4. empresa
CREATE TABLE IF NOT EXISTS empresa (
    id INT AUTO_INCREMENT PRIMARY KEY,
    cnpj VARCHAR(14) NOT NULL UNIQUE,
    razao_social VARCHAR(255) NOT NULL,
    nome_fantasia VARCHAR(255) NULL,
    grupo_economico_id INT NULL,
    tipo_empresa INT NOT NULL DEFAULT 0,
    rating VARCHAR(50) NULL,
    limite_credito DECIMAL(18,2) NULL,
    uf_atuacao VARCHAR(2) NULL,
    local_atuacao VARCHAR(255) NULL,
    data_cadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    ativado BOOLEAN NOT NULL DEFAULT TRUE,
    caminho_logo VARCHAR(500) NULL,
    CONSTRAINT FK_empresa_grupo FOREIGN KEY (grupo_economico_id) REFERENCES grupo_economico(id) ON DELETE SET NULL
);

-- 5. empresa_setor (Relacionamento N:N)
CREATE TABLE IF NOT EXISTS empresa_setor (
    empresa_id INT NOT NULL,
    setor_id INT NOT NULL,
    principal BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (empresa_id, setor_id),
    CONSTRAINT FK_es_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id) ON DELETE CASCADE,
    CONSTRAINT FK_es_setor FOREIGN KEY (setor_id) REFERENCES setor_atividade(id) ON DELETE CASCADE
);

-- 6. conta_padrao (Plano de contas hierarquico)
CREATE TABLE IF NOT EXISTS conta_padrao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(100) NOT NULL UNIQUE,
    descricao VARCHAR(255) NOT NULL,
    grupo_principal INT NOT NULL,
    conta_pai_id INT NULL,
    nivel INT NOT NULL,
    eh_totalizadora BOOLEAN NOT NULL,
    ordem INT NOT NULL,
    ativa BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT FK_cp_pai FOREIGN KEY (conta_pai_id) REFERENCES conta_padrao(id) ON DELETE CASCADE
);

-- 7. balanco
CREATE TABLE IF NOT EXISTS balanco (
    id INT AUTO_INCREMENT PRIMARY KEY,
    empresa_id INT NOT NULL,
    ano_exercicio INT NOT NULL,
    data_referencia DATETIME NOT NULL,
    tipo_balanco INT NOT NULL,
    data_planilhamento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    usuario_id INT NOT NULL,
    origem VARCHAR(255) NULL,
    hash_origem_pdf VARCHAR(255) NULL,
    observacoes TEXT NULL,
    moeda VARCHAR(10) NOT NULL DEFAULT 'BRL',
    multiplicador_valores INT NOT NULL DEFAULT 1,
    ativado BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT UQ_balanco UNIQUE (empresa_id, ano_exercicio, tipo_balanco),
    CONSTRAINT FK_balanco_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id) ON DELETE CASCADE,
    CONSTRAINT FK_balanco_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT
);

-- 8. conta_balanco (Valores das contas)
CREATE TABLE IF NOT EXISTS conta_balanco (
    id INT AUTO_INCREMENT PRIMARY KEY,
    balanco_id INT NOT NULL,
    conta_padrao_id INT NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    descricao_original VARCHAR(255) NULL,
    CONSTRAINT UQ_cb UNIQUE (balanco_id, conta_padrao_id),
    CONSTRAINT FK_cb_balanco FOREIGN KEY (balanco_id) REFERENCES balanco(id) ON DELETE CASCADE,
    CONSTRAINT FK_cb_padrao FOREIGN KEY (conta_padrao_id) REFERENCES conta_padrao(id) ON DELETE RESTRICT
);

-- 9. dre
CREATE TABLE IF NOT EXISTS dre (
    id INT AUTO_INCREMENT PRIMARY KEY,
    empresa_id INT NOT NULL,
    ano_exercicio INT NOT NULL,
    tipo_balanco INT NOT NULL,
    receita_liquida DECIMAL(18,2) NOT NULL,
    lucro_bruto DECIMAL(18,2) NOT NULL,
    resultado_operacional DECIMAL(18,2) NOT NULL,
    despesas_financeiras DECIMAL(18,2) NOT NULL,
    lucro_liquido DECIMAL(18,2) NOT NULL,
    usuario_id INT NOT NULL,
    origem VARCHAR(255) NULL,
    hash_origem_pdf VARCHAR(255) NULL,
    data_planilhamento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ativado BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT UQ_dre UNIQUE (empresa_id, ano_exercicio, tipo_balanco),
    CONSTRAINT FK_dre_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id) ON DELETE CASCADE,
    CONSTRAINT FK_dre_usuario FOREIGN KEY (usuario_id) REFERENCES usuario(id) ON DELETE RESTRICT
);

-- 10. MapeamentosDePara
CREATE TABLE IF NOT EXISTS MapeamentosDePara (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TextoOriginal VARCHAR(255) NOT NULL,
    ContaPadraoId INT NOT NULL,
    EmpresaId INT NULL,
    CONSTRAINT UQ_Texto_Empresa UNIQUE (TextoOriginal, EmpresaId),
    CONSTRAINT FK_mdp_cp FOREIGN KEY (ContaPadraoId) REFERENCES conta_padrao(id) ON DELETE CASCADE,
    CONSTRAINT FK_mdp_empresa FOREIGN KEY (EmpresaId) REFERENCES empresa(id) ON DELETE CASCADE
);

-- DADOS INICIAIS DE TESTE
-- Usuario admin/admin
INSERT INTO usuario (nome, login, email, senha_hash, perfil)
VALUES ('Administrador', 'admin', 'admin@banco.com.br', '$2a$11$HTuD7sZgW6pyFgnBAh2mie77rTrE39WJEayENBaNSoxsjkp9Y9IUG', 'Administrador')
ON DUPLICATE KEY UPDATE id=id; -- Hash BCrypt da palavra 'admin'

-- Contas Padrao Basicas (Mock Inicial para o sistema abrir e a importacao B3 funcionar)
INSERT IGNORE INTO conta_padrao (id, codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem) VALUES
(1, '1', 'ATIVO', 0, NULL, 1, 1, 10),
(2, '1.01', 'ATIVO CIRCULANTE', 0, 1, 2, 1, 20),
(3, '1.01.01', 'Caixa e Equivalentes de Caixa', 0, 2, 3, 0, 30),
(4, '1.01.02', 'Aplicacoes Financeiras', 0, 2, 3, 0, 40),
(5, '1.01.03', 'Contas a Receber', 0, 2, 3, 0, 50),
(6, '1.01.04', 'Estoques', 0, 2, 3, 0, 60),
(7, '1.02', 'ATIVO NAO CIRCULANTE', 0, 1, 2, 1, 70),
(8, '1.02.01', 'Ativo Realizavel a Longo Prazo', 0, 7, 3, 0, 80),
(9, '1.02.02', 'Investimentos', 0, 7, 3, 0, 90),
(10, '1.02.03', 'Imobilizado', 0, 7, 3, 0, 100),
(11, '1.02.04', 'Intangivel', 0, 7, 3, 0, 110),
(12, '2', 'PASSIVO', 1, NULL, 1, 1, 120),
(13, '2.01', 'PASSIVO CIRCULANTE', 1, 12, 2, 1, 130),
(14, '2.01.01', 'Obrigacoes Sociais e Trabalhistas', 1, 13, 3, 0, 140),
(15, '2.01.02', 'Fornecedores', 1, 13, 3, 0, 150),
(16, '2.01.03', 'Obrigacoes Fiscais', 1, 13, 3, 0, 160),
(17, '2.01.04', 'Emprestimos e Financiamentos', 1, 13, 3, 0, 170),
(18, '2.02', 'PASSIVO NAO CIRCULANTE', 1, 12, 2, 1, 180),
(19, '2.02.01', 'Emprestimos e Financiamentos a Longo Prazo', 1, 18, 3, 0, 190),
(20, '2.02.02', 'Provisoes', 1, 18, 3, 0, 200),
(21, '2.03', 'PATRIMONIO LIQUIDO', 2, 12, 2, 1, 210),
(22, '2.03.01', 'Capital Social', 2, 21, 3, 0, 220),
(23, '2.03.02', 'Reservas de Capital', 2, 21, 3, 0, 230),
(24, '2.03.03', 'Reservas de Lucros', 2, 21, 3, 0, 240),
(25, '2.03.04', 'Lucros/Prejuizos Acumulados', 2, 21, 3, 0, 250);

