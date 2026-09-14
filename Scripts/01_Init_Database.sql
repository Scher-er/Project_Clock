CREATE DATABASE IF NOT EXISTS balanco_patrimonial
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE balanco_patrimonial;

-- 1. Grupos Economicos
CREATE TABLE IF NOT EXISTS GruposEconomicos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(255) NOT NULL,
    Descricao TEXT NULL,
    DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 2. Setores de Atividade
CREATE TABLE IF NOT EXISTS SetoresAtividade (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(50) NOT NULL,
    Nome VARCHAR(255) NOT NULL,
    Descricao TEXT NULL
);

-- 3. Usuarios
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(255) NOT NULL,
    Login VARCHAR(100) NOT NULL UNIQUE,
    Email VARCHAR(255) NOT NULL,
    SenhaHash VARCHAR(255) NOT NULL,
    Perfil VARCHAR(50) NOT NULL DEFAULT 'Analista',
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UltimoLogin DATETIME NULL
);

-- 4. Empresas
CREATE TABLE IF NOT EXISTS Empresas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Cnpj VARCHAR(14) NOT NULL UNIQUE,
    RazaoSocial VARCHAR(255) NOT NULL,
    NomeFantasia VARCHAR(255) NULL,
    GrupoEconomicoId INT NULL,
    TipoEmpresa INT NOT NULL DEFAULT 0,
    Rating VARCHAR(50) NULL,
    LimiteCredito DECIMAL(18,2) NULL,
    UfAtuacao VARCHAR(2) NULL,
    LocalAtuacao VARCHAR(255) NULL,
    DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    CaminhoLogo VARCHAR(500) NULL,
    CONSTRAINT FK_Empresa_Grupo FOREIGN KEY (GrupoEconomicoId) REFERENCES GruposEconomicos(Id) ON DELETE SET NULL
);

-- 5. EmpresaSetor (Relacionamento N:N)
CREATE TABLE IF NOT EXISTS EmpresaSetor (
    EmpresaId INT NOT NULL,
    SetorAtividadeId INT NOT NULL,
    PRIMARY KEY (EmpresaId, SetorAtividadeId),
    CONSTRAINT FK_EmpresaSetor_Empresa FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id) ON DELETE CASCADE,
    CONSTRAINT FK_EmpresaSetor_Setor FOREIGN KEY (SetorAtividadeId) REFERENCES SetoresAtividade(Id) ON DELETE CASCADE
);

-- 6. ContasPadrao (Plano de contas hierarquico)
CREATE TABLE IF NOT EXISTS ContasPadrao (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(100) NOT NULL UNIQUE,
    Descricao VARCHAR(255) NOT NULL,
    GrupoPrincipal INT NOT NULL,
    ContaPaiId INT NULL,
    Nivel INT NOT NULL,
    EhTotalizadora BOOLEAN NOT NULL,
    Ordem INT NOT NULL,
    Ativa BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT FK_ContaPadrao_Pai FOREIGN KEY (ContaPaiId) REFERENCES ContasPadrao(Id) ON DELETE CASCADE
);

-- 7. Balancos
CREATE TABLE IF NOT EXISTS Balancos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EmpresaId INT NOT NULL,
    AnoExercicio INT NOT NULL,
    DataReferencia DATETIME NOT NULL,
    TipoBalanco INT NOT NULL,
    DataPlanilhamento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UsuarioId INT NOT NULL,
    Origem VARCHAR(255) NULL,
    HashOrigemPdf VARCHAR(255) NULL,
    Observacoes TEXT NULL,
    Moeda VARCHAR(10) NOT NULL DEFAULT 'BRL',
    MultiplicadorValores INT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Balanco UNIQUE (EmpresaId, AnoExercicio, TipoBalanco),
    CONSTRAINT FK_Balanco_Empresa FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Balanco_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE RESTRICT
);

-- 8. ContasBalanco (Valores das contas)
CREATE TABLE IF NOT EXISTS ContasBalanco (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    BalancoId INT NOT NULL,
    ContaPadraoId INT NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    DescricaoOriginal VARCHAR(255) NULL,
    CONSTRAINT UQ_ContaBalanco UNIQUE (BalancoId, ContaPadraoId),
    CONSTRAINT FK_ContaBalanco_Balanco FOREIGN KEY (BalancoId) REFERENCES Balancos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ContaBalanco_Padrao FOREIGN KEY (ContaPadraoId) REFERENCES ContasPadrao(Id) ON DELETE RESTRICT
);

-- 9. DREs
CREATE TABLE IF NOT EXISTS Dres (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EmpresaId INT NOT NULL,
    AnoExercicio INT NOT NULL,
    TipoBalanco INT NOT NULL,
    ReceitaLiquida DECIMAL(18,2) NOT NULL,
    LucroBruto DECIMAL(18,2) NOT NULL,
    ResultadoOperacional DECIMAL(18,2) NOT NULL,
    DespesasFinanceiras DECIMAL(18,2) NOT NULL,
    LucroLiquido DECIMAL(18,2) NOT NULL,
    UsuarioId INT NOT NULL,
    Origem VARCHAR(255) NULL,
    HashOrigemPdf VARCHAR(255) NULL,
    DataPlanilhamento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Dre UNIQUE (EmpresaId, AnoExercicio, TipoBalanco),
    CONSTRAINT FK_Dre_Empresa FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Dre_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE RESTRICT
);

-- 10. MapeamentosDePara
CREATE TABLE IF NOT EXISTS MapeamentosDePara (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TextoOriginal VARCHAR(255) NOT NULL,
    ContaPadraoId INT NOT NULL,
    EmpresaId INT NULL,
    CONSTRAINT UQ_Texto_Empresa UNIQUE (TextoOriginal, EmpresaId),
    CONSTRAINT FK_MapeamentosDePara_ContaPadrao FOREIGN KEY (ContaPadraoId) REFERENCES ContasPadrao(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MapeamentosDePara_Empresa FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id) ON DELETE CASCADE
);

-- DADOS INICIAIS DE TESTE
-- Usuario admin/admin
INSERT INTO Usuarios (Nome, Login, Email, SenhaHash, Perfil)
VALUES ('Administrador', 'admin', 'admin@banco.com.br', '$2a$11$Vp9m0wQp6x4s12Kk4tX6/OxB4jRk1b5W2e7K2c1vFqX4e2X2sY6qC', 'Administrador')
ON DUPLICATE KEY UPDATE Id=Id; -- Hash BCrypt da palavra 'admin'

-- Contas Padrao Basicas (Mock Inicial para o sistema abrir e a importacao B3 funcionar)
INSERT IGNORE INTO ContasPadrao (Id, Codigo, Descricao, GrupoPrincipal, ContaPaiId, Nivel, EhTotalizadora, Ordem) VALUES
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


