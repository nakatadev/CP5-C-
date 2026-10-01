using CadastroProdutos.Logging;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Data;

/// <summary>
/// Executa o script .sql de criação da tabela (CREATE TABLE IF NOT EXISTS),
/// garantindo que o banco esteja pronto antes de abrir o menu.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly IOperacaoLogger _logger;

    public DatabaseInitializer(string connectionString, IOperacaoLogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public void Inicializar(string caminhoScript)
    {
        if (!File.Exists(caminhoScript))
            throw new DataAccessException($"Script de criação do banco não encontrado: {caminhoScript}");

        var script = File.ReadAllText(caminhoScript);

        try
        {
            using var conexao = new SqliteConnection(_connectionString);
            conexao.Open();

            using var transacao = conexao.BeginTransaction();
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = script;
            comando.ExecuteNonQuery();
            transacao.Commit();

            _logger.Info($"Banco de dados inicializado ({conexao.DataSource}).");
        }
        catch (SqliteException ex)
        {
            _logger.Erro("Falha ao inicializar o banco de dados.", ex);
            throw new DataAccessException(
                "Não foi possível inicializar o banco de dados. Verifique a connection string no appsettings.json.", ex);
        }
    }
}
