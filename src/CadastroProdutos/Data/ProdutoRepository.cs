using CadastroProdutos.Logging;
using CadastroProdutos.Models;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Data;

/// <summary>
/// Responsável por TODO o acesso ao banco de dados da tabela Produtos (ADO.NET puro).
///
/// Boas práticas aplicadas:
///  - Todos os comandos SQL são PARAMETRIZADOS (@Parametro) -> evita SQL Injection;
///  - ExecuteNonQuery para INSERT / UPDATE / DELETE;
///  - ExecuteReader para SELECT, com mapeamento MANUAL do DataReader para Produto;
///  - Transações nas operações de escrita (Commit / Rollback);
///  - Soft delete: "Excluir" apenas marca o registro como inativo;
///  - SqliteException é registrada no log e convertida em DataAccessException.
/// </summary>
public class ProdutoRepository
{
    private const string ColunasSelect =
        "Id, Nome, Preco, Estoque, Categoria, Ativo, DataCadastro, DataExclusao";

    private readonly string _connectionString;
    private readonly IOperacaoLogger _logger;

    public ProdutoRepository(string connectionString, IOperacaoLogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string não foi informada.", nameof(connectionString));

        _connectionString = connectionString;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // INSERT
    // ------------------------------------------------------------------
    /// <summary>Insere um produto e retorna o Id gerado pelo banco.</summary>
    public int Inserir(Produto produto)
    {
        ValidarProduto(produto);

        const string sql = @"
            INSERT INTO Produtos (Nome, Preco, Estoque, Categoria)
            VALUES (@Nome, @Preco, @Estoque, @Categoria);";

        try
        {
            using var conexao = AbrirConexao();
            using var transacao = conexao.BeginTransaction();
            try
            {
                using (var comando = conexao.CreateCommand())
                {
                    comando.Transaction = transacao;
                    comando.CommandText = sql;
                    AdicionarParametrosProduto(comando, produto);
                    comando.ExecuteNonQuery();
                }

                // Recupera o Id gerado na MESMA conexão/transação
                int novoId;
                using (var comandoId = conexao.CreateCommand())
                {
                    comandoId.Transaction = transacao;
                    comandoId.CommandText = "SELECT last_insert_rowid();";
                    novoId = Convert.ToInt32(comandoId.ExecuteScalar());
                }

                transacao.Commit();

                produto.Id = novoId;
                _logger.Info($"INSERIR  | Produto inserido: Id={novoId}, Nome='{produto.Nome}', " +
                             $"Preco={produto.Preco}, Estoque={produto.Estoque}, Categoria='{produto.Categoria}'");
                return novoId;
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }
        catch (SqliteException ex)
        {
            throw TratarErro(ex, "inserir o produto");
        }
    }

    // ------------------------------------------------------------------
    // SELECT (lista)
    // ------------------------------------------------------------------
    /// <summary>Lista os produtos ativos (ou todos, incluindo os excluídos logicamente).</summary>
    public List<Produto> Listar(bool incluirExcluidos = false)
    {
        var sql = $@"
            SELECT {ColunasSelect}
            FROM Produtos
            WHERE Ativo = 1 OR @IncluirExcluidos = 1
            ORDER BY Id;";

        try
        {
            using var conexao = AbrirConexao();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.Add("@IncluirExcluidos", SqliteType.Integer).Value = incluirExcluidos ? 1 : 0;

            var produtos = new List<Produto>();
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                produtos.Add(MapearProduto(leitor));
            }

            _logger.Info($"LISTAR   | {produtos.Count} produto(s) retornado(s) (incluirExcluidos={incluirExcluidos}).");
            return produtos;
        }
        catch (SqliteException ex)
        {
            throw TratarErro(ex, "listar os produtos");
        }
    }

    // ------------------------------------------------------------------
    // SELECT por Id
    // ------------------------------------------------------------------
    /// <summary>Busca um produto pelo Id. Retorna null se não existir (ou se estiver excluído).</summary>
    public Produto? BuscarPorId(int id, bool incluirExcluidos = false)
    {
        var sql = $@"
            SELECT {ColunasSelect}
            FROM Produtos
            WHERE Id = @Id AND (Ativo = 1 OR @IncluirExcluidos = 1);";

        try
        {
            using var conexao = AbrirConexao();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.Add("@Id", SqliteType.Integer).Value = id;
            comando.Parameters.Add("@IncluirExcluidos", SqliteType.Integer).Value = incluirExcluidos ? 1 : 0;

            using var leitor = comando.ExecuteReader();
            if (leitor.Read())
            {
                var produto = MapearProduto(leitor);
                _logger.Info($"BUSCAR   | Produto encontrado: Id={id}.");
                return produto;
            }

            _logger.Aviso($"BUSCAR   | Produto não encontrado: Id={id}.");
            return null;
        }
        catch (SqliteException ex)
        {
            throw TratarErro(ex, $"buscar o produto {id}");
        }
    }

    // ------------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------------
    /// <summary>Atualiza um produto ativo. Retorna false se o Id não existir.</summary>
    public bool Atualizar(Produto produto)
    {
        ValidarProduto(produto);

        const string sql = @"
            UPDATE Produtos
               SET Nome      = @Nome,
                   Preco     = @Preco,
                   Estoque   = @Estoque,
                   Categoria = @Categoria
             WHERE Id = @Id
               AND Ativo = 1;";

        try
        {
            using var conexao = AbrirConexao();
            using var transacao = conexao.BeginTransaction();
            try
            {
                using var comando = conexao.CreateCommand();
                comando.Transaction = transacao;
                comando.CommandText = sql;
                AdicionarParametrosProduto(comando, produto);
                comando.Parameters.Add("@Id", SqliteType.Integer).Value = produto.Id;

                var linhasAfetadas = comando.ExecuteNonQuery();
                transacao.Commit();

                if (linhasAfetadas == 0)
                {
                    _logger.Aviso($"ATUALIZAR| Nenhum produto ativo com Id={produto.Id}.");
                    return false;
                }

                _logger.Info($"ATUALIZAR| Produto atualizado: Id={produto.Id}, Nome='{produto.Nome}', " +
                             $"Preco={produto.Preco}, Estoque={produto.Estoque}, Categoria='{produto.Categoria}'");
                return true;
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }
        catch (SqliteException ex)
        {
            throw TratarErro(ex, $"atualizar o produto {produto.Id}");
        }
    }

    // ------------------------------------------------------------------
    // DELETE lógico (soft delete) - bônus
    // ------------------------------------------------------------------
    /// <summary>
    /// Exclusão lógica: marca o produto como inativo (Ativo = 0) e registra a data.
    /// O registro continua no banco e pode ser restaurado.
    /// </summary>
    public bool Excluir(int id)
    {
        const string sql = @"
            UPDATE Produtos
               SET Ativo = 0,
                   DataExclusao = datetime('now', 'localtime')
             WHERE Id = @Id
               AND Ativo = 1;";

        return ExecutarComandoPorId(sql, id, "EXCLUIR  ", "Produto excluído (soft delete)", $"excluir o produto {id}");
    }

    /// <summary>Restaura um produto excluído logicamente.</summary>
    public bool Restaurar(int id)
    {
        const string sql = @"
            UPDATE Produtos
               SET Ativo = 1,
                   DataExclusao = NULL
             WHERE Id = @Id
               AND Ativo = 0;";

        return ExecutarComandoPorId(sql, id, "RESTAURAR", "Produto restaurado", $"restaurar o produto {id}");
    }

    // ------------------------------------------------------------------
    // DELETE físico
    // ------------------------------------------------------------------
    /// <summary>Exclusão definitiva: remove fisicamente o registro da tabela.</summary>
    public bool ExcluirDefinitivamente(int id)
    {
        const string sql = "DELETE FROM Produtos WHERE Id = @Id;";

        return ExecutarComandoPorId(sql, id, "DELETAR  ", "Produto removido definitivamente", $"excluir definitivamente o produto {id}");
    }

    // ------------------------------------------------------------------
    // Métodos auxiliares
    // ------------------------------------------------------------------
    private bool ExecutarComandoPorId(string sql, int id, string operacaoLog, string mensagemSucesso, string descricaoErro)
    {
        try
        {
            using var conexao = AbrirConexao();
            using var transacao = conexao.BeginTransaction();
            try
            {
                using var comando = conexao.CreateCommand();
                comando.Transaction = transacao;
                comando.CommandText = sql;
                comando.Parameters.Add("@Id", SqliteType.Integer).Value = id;

                var linhasAfetadas = comando.ExecuteNonQuery();
                transacao.Commit();

                if (linhasAfetadas == 0)
                {
                    _logger.Aviso($"{operacaoLog}| Nenhum produto afetado para Id={id}.");
                    return false;
                }

                _logger.Info($"{operacaoLog}| {mensagemSucesso}: Id={id}.");
                return true;
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }
        catch (SqliteException ex)
        {
            throw TratarErro(ex, descricaoErro);
        }
    }

    private SqliteConnection AbrirConexao()
    {
        var conexao = new SqliteConnection(_connectionString);
        conexao.Open();
        return conexao;
    }

    /// <summary>Adiciona os parâmetros comuns de INSERT/UPDATE (nunca concatenamos valores no SQL).</summary>
    private static void AdicionarParametrosProduto(SqliteCommand comando, Produto produto)
    {
        comando.Parameters.Add("@Nome", SqliteType.Text).Value = produto.Nome.Trim();
        comando.Parameters.Add("@Preco", SqliteType.Real).Value = produto.Preco;
        comando.Parameters.Add("@Estoque", SqliteType.Integer).Value = produto.Estoque;
        comando.Parameters.Add("@Categoria", SqliteType.Text).Value = produto.Categoria.Trim();
    }

    /// <summary>Mapeamento MANUAL de uma linha do DataReader para um objeto Produto.</summary>
    private static Produto MapearProduto(SqliteDataReader leitor)
    {
        var ordinalDataExclusao = leitor.GetOrdinal("DataExclusao");

        return new Produto
        {
            Id = leitor.GetInt32(leitor.GetOrdinal("Id")),
            Nome = leitor.GetString(leitor.GetOrdinal("Nome")),
            Preco = Math.Round(leitor.GetDecimal(leitor.GetOrdinal("Preco")), 2),
            Estoque = leitor.GetInt32(leitor.GetOrdinal("Estoque")),
            Categoria = leitor.GetString(leitor.GetOrdinal("Categoria")),
            Ativo = leitor.GetInt32(leitor.GetOrdinal("Ativo")) == 1,
            DataCadastro = leitor.GetDateTime(leitor.GetOrdinal("DataCadastro")),
            DataExclusao = leitor.IsDBNull(ordinalDataExclusao) ? null : leitor.GetDateTime(ordinalDataExclusao)
        };
    }

    private void ValidarProduto(Produto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        var erros = produto.Validar();
        if (erros.Count > 0)
        {
            var mensagem = string.Join(" ", erros);
            _logger.Aviso($"VALIDACAO| Produto inválido: {mensagem}");
            throw new ArgumentException(mensagem);
        }
    }

    /// <summary>
    /// Registra o erro no log e o converte em uma mensagem amigável,
    /// de acordo com o código de erro do SQLite.
    /// </summary>
    private DataAccessException TratarErro(SqliteException ex, string operacao)
    {
        var detalhe = ex.SqliteErrorCode switch
        {
            19 => "os dados violam uma restrição do banco (valor inválido, nulo ou duplicado).", // SQLITE_CONSTRAINT
            5 or 6 => "o banco de dados está ocupado/bloqueado por outro processo. Tente novamente.", // BUSY / LOCKED
            8 => "o banco de dados está em modo somente leitura.",                                   // READONLY
            14 => "não foi possível abrir o arquivo do banco. Verifique a connection string.",       // CANTOPEN
            26 => "o arquivo informado não é um banco SQLite válido.",                               // NOTADB
            1 => "erro no comando SQL ou a tabela não existe.",                                      // SQLITE_ERROR
            _ => "ocorreu um erro inesperado no banco de dados."
        };

        _logger.Erro($"ERRO     | Falha ao {operacao} (código SQLite {ex.SqliteErrorCode}).", ex);
        return new DataAccessException($"Não foi possível {operacao}: {detalhe}", ex);
    }
}
