-- =============================================================
--  Cadastro de Produtos - Script de criação do banco (SQLite)
--  Aluno: Rodrigo Nakata - RM 556417
-- =============================================================
--  Este script é executado automaticamente pela aplicação na
--  inicialização (DatabaseInitializer), mas também pode ser
--  executado manualmente:
--      sqlite3 produtos.db < database/script.sql
-- =============================================================

CREATE TABLE IF NOT EXISTS Produtos (
    Id            INTEGER        PRIMARY KEY AUTOINCREMENT,
    Nome          TEXT           NOT NULL CHECK (length(trim(Nome)) > 0 AND length(Nome) <= 100),
    Preco         NUMERIC(10,2)  NOT NULL CHECK (Preco >= 0),
    Estoque       INTEGER        NOT NULL DEFAULT 0 CHECK (Estoque >= 0),
    Categoria     TEXT           NOT NULL CHECK (length(trim(Categoria)) > 0 AND length(Categoria) <= 50),

    -- Colunas de controle para o SOFT DELETE (bônus)
    Ativo         INTEGER        NOT NULL DEFAULT 1 CHECK (Ativo IN (0, 1)),
    DataCadastro  TEXT           NOT NULL DEFAULT (datetime('now', 'localtime')),
    DataExclusao  TEXT           NULL
);

-- Índices para acelerar as consultas mais comuns
CREATE INDEX IF NOT EXISTS IX_Produtos_Ativo     ON Produtos (Ativo);
CREATE INDEX IF NOT EXISTS IX_Produtos_Categoria ON Produtos (Categoria);
