-- =============================================================================
-- Schema MySQL — Sistema de Análise de Balanço Patrimonial
-- Banco principal. Cobre os requisitos do trabalho:
--   ✓ 8 tabelas (mínimo: 5)
--   ✓ Chaves primárias em todas
--   ✓ Chaves estrangeiras com integridade referencial
--   ✓ Relacionamentos 1:N (vários)
--   ✓ Relacionamento N:N com tabela intermediária (empresa_setor)
-- =============================================================================

DROP DATABASE IF EXISTS balanco_patrimonial;
CREATE DATABASE balanco_patrimonial
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE balanco_patrimonial;

-- =============================================================================
-- 1) USUARIO — operadores do sistema (analistas, gerentes, admins)
-- =============================================================================
CREATE TABLE usuario (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    nome            VARCHAR(150)  NOT NULL,
    login           VARCHAR(50)   NOT NULL UNIQUE,
    email           VARCHAR(150)  NOT NULL UNIQUE,
    senha_hash      VARCHAR(255)  NOT NULL,  -- BCrypt: 60 chars
    perfil          VARCHAR(30)   NOT NULL DEFAULT 'Analista',
    ativo           TINYINT(1)    NOT NULL DEFAULT 1,
    data_cadastro   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ultimo_login    DATETIME      NULL,

    INDEX idx_usuario_login (login)
) ENGINE=InnoDB;

-- =============================================================================
-- 2) GRUPO_ECONOMICO — grupos societários (1:N com empresa)
-- =============================================================================
CREATE TABLE grupo_economico (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    nome            VARCHAR(200)  NOT NULL UNIQUE,
    descricao       TEXT          NULL,
    data_cadastro   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ativo           TINYINT(1)    NOT NULL DEFAULT 1
) ENGINE=InnoDB;

-- =============================================================================
-- 3) SETOR_ATIVIDADE — setores econômicos (CNAE simplificado)
-- =============================================================================
CREATE TABLE setor_atividade (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    codigo          VARCHAR(20)   NOT NULL UNIQUE,
    nome            VARCHAR(200)  NOT NULL,
    descricao       TEXT          NULL
) ENGINE=InnoDB;

-- =============================================================================
-- 4) EMPRESA — entidade central; FK 1:N com grupo_economico
-- =============================================================================
CREATE TABLE empresa (
    id                   INT AUTO_INCREMENT PRIMARY KEY,
    cnpj                 VARCHAR(14)   NOT NULL UNIQUE,
    razao_social         VARCHAR(200)  NOT NULL,
    nome_fantasia        VARCHAR(200)  NULL,
    grupo_economico_id   INT           NULL,
    tipo_empresa         TINYINT       NOT NULL DEFAULT 3,   -- enum TipoEmpresa
    rating               VARCHAR(10)   NULL,
    limite_credito       DECIMAL(18,2) NULL,
    uf_atuacao           CHAR(2)       NULL,
    local_atuacao        VARCHAR(200)  NULL,
    caminho_logo         VARCHAR(500)  NULL,
    data_cadastro        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ativo                TINYINT(1)    NOT NULL DEFAULT 1,

    CONSTRAINT fk_empresa_grupo
        FOREIGN KEY (grupo_economico_id) REFERENCES grupo_economico(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE,

    INDEX idx_empresa_razao (razao_social),
    INDEX idx_empresa_cnpj  (cnpj)
) ENGINE=InnoDB;

-- =============================================================================
-- 5) EMPRESA_SETOR — N:N entre empresa e setor (tabela intermediária)
-- =============================================================================
CREATE TABLE empresa_setor (
    empresa_id      INT NOT NULL,
    setor_id        INT NOT NULL,
    principal       TINYINT(1) NOT NULL DEFAULT 0,  -- marca setor principal

    PRIMARY KEY (empresa_id, setor_id),

    CONSTRAINT fk_es_empresa
        FOREIGN KEY (empresa_id) REFERENCES empresa(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_es_setor
        FOREIGN KEY (setor_id) REFERENCES setor_atividade(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB;

-- =============================================================================
-- 6) CONTA_PADRAO — plano de contas hierárquico (auto-referente)
-- =============================================================================
CREATE TABLE conta_padrao (
    id                 INT AUTO_INCREMENT PRIMARY KEY,
    codigo             VARCHAR(20)   NOT NULL UNIQUE,        -- "1.01.03.02"
    descricao          VARCHAR(200)  NOT NULL,
    grupo_principal    TINYINT       NOT NULL,                -- enum GrupoContaPrincipal
    conta_pai_id       INT           NULL,
    nivel              TINYINT       NOT NULL,                -- 1..4
    eh_totalizadora    TINYINT(1)    NOT NULL DEFAULT 0,
    ordem              INT           NOT NULL DEFAULT 0,
    ativa              TINYINT(1)    NOT NULL DEFAULT 1,

    CONSTRAINT fk_conta_pai
        FOREIGN KEY (conta_pai_id) REFERENCES conta_padrao(id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    INDEX idx_conta_codigo (codigo),
    INDEX idx_conta_grupo  (grupo_principal)
) ENGINE=InnoDB;

-- =============================================================================
-- 7) BALANCO — balanço de uma empresa em um ano específico
--    FK 1:N com empresa e com usuario (quem planilhou)
-- =============================================================================
CREATE TABLE balanco (
    id                     INT AUTO_INCREMENT PRIMARY KEY,
    empresa_id             INT          NOT NULL,
    ano_exercicio          INT          NOT NULL,
    data_referencia        DATE         NOT NULL,
    tipo_balanco           TINYINT      NOT NULL,            -- enum TipoBalanco
    data_planilhamento     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    usuario_id             INT          NOT NULL,
    origem                 VARCHAR(255) NULL,
    hash_origem_pdf        VARCHAR(64)  NULL,
    observacoes            TEXT         NULL,
    moeda                  CHAR(3)      NOT NULL DEFAULT 'BRL',
    multiplicador_valores  INT          NOT NULL DEFAULT 1,

    CONSTRAINT fk_balanco_empresa
        FOREIGN KEY (empresa_id) REFERENCES empresa(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_balanco_usuario
        FOREIGN KEY (usuario_id) REFERENCES usuario(id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    -- Garante apenas 1 balanço por empresa+ano+tipo
    CONSTRAINT uq_balanco_empresa_ano_tipo
        UNIQUE (empresa_id, ano_exercicio, tipo_balanco),

    INDEX idx_balanco_empresa (empresa_id),
    INDEX idx_balanco_ano     (ano_exercicio)
) ENGINE=InnoDB;

-- =============================================================================
-- 8) CONTA_BALANCO — valor de uma conta padrão em um balanço
--    FK 1:N com balanco e com conta_padrao
-- =============================================================================
CREATE TABLE conta_balanco (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    balanco_id          INT           NOT NULL,
    conta_padrao_id     INT           NOT NULL,
    valor               DECIMAL(18,2) NOT NULL DEFAULT 0,
    descricao_original  VARCHAR(255)  NULL,

    CONSTRAINT fk_cb_balanco
        FOREIGN KEY (balanco_id) REFERENCES balanco(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_cb_conta
        FOREIGN KEY (conta_padrao_id) REFERENCES conta_padrao(id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    -- Não permite duplicar a mesma conta no mesmo balanço
    CONSTRAINT uq_cb_balanco_conta
        UNIQUE (balanco_id, conta_padrao_id),

    INDEX idx_cb_balanco (balanco_id)
) ENGINE=InnoDB;
