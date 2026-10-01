using CadastroProdutos.Data;
using CadastroProdutos.Models;
using Microsoft.Data.Sqlite;

namespace CadastroProdutos.Tests;

/// <summary>
/// Testes do ProdutoRepository. Cada teste usa um banco SQLite temporário e isolado,
/// criado com o mesmo script .sql da aplicação.
/// </summary>
public class ProdutoRepositoryTests : IDisposable
{
    private readonly string _caminhoBanco;
    private readonly string _connectionString;
    private readonly LoggerEmMemoria _logger = new();
    private readonly ProdutoRepository _repository;

    public ProdutoRepositoryTests()
    {
        _caminhoBanco = Path.Combine(Path.GetTempPath(), $"produtos_teste_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_caminhoBanco}";

        var script = Path.Combine(AppContext.BaseDirectory, "database", "script.sql");
        new DatabaseInitializer(_connectionString, _logger).Inicializar(script);

        _repository = new ProdutoRepository(_connectionString, _logger);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_caminhoBanco))
            File.Delete(_caminhoBanco);
    }

    private static Produto NovoProduto(string nome = "Teclado Mecânico", decimal preco = 249.90m,
        int estoque = 15, string categoria = "Periféricos") =>
        new() { Nome = nome, Preco = preco, Estoque = estoque, Categoria = categoria };

    // ---------------- INSERIR ----------------

    [Fact]
    public void Inserir_ProdutoValido_RetornaIdEPersisteDados()
    {
        var id = _repository.Inserir(NovoProduto());

        Assert.True(id > 0);
        var salvo = _repository.BuscarPorId(id);
        Assert.NotNull(salvo);
        Assert.Equal("Teclado Mecânico", salvo!.Nome);
        Assert.Equal(249.90m, salvo.Preco);
        Assert.Equal(15, salvo.Estoque);
        Assert.Equal("Periféricos", salvo.Categoria);
        Assert.True(salvo.Ativo);
    }

    [Fact]
    public void Inserir_DoisProdutos_GeraIdsDiferentes()
    {
        var id1 = _repository.Inserir(NovoProduto("Produto A"));
        var id2 = _repository.Inserir(NovoProduto("Produto B"));

        Assert.NotEqual(id1, id2);
    }

    [Theory]
    [InlineData("", 10, 1, "Cat")]
    [InlineData("Nome", -1, 1, "Cat")]
    [InlineData("Nome", 10, -5, "Cat")]
    [InlineData("Nome", 10, 1, "  ")]
    public void Inserir_ProdutoInvalido_LancaArgumentExceptionENaoGrava(string nome, decimal preco, int estoque, string categoria)
    {
        Assert.Throws<ArgumentException>(() => _repository.Inserir(NovoProduto(nome, preco, estoque, categoria)));
        Assert.Empty(_repository.Listar(incluirExcluidos: true));
    }

    // ---------------- SQL INJECTION ----------------

    [Fact]
    public void Inserir_NomeComSqlInjection_GravaTextoLiteralENaoApagaTabela()
    {
        const string nomeMalicioso = "x'); DROP TABLE Produtos; --";

        var id = _repository.Inserir(NovoProduto(nomeMalicioso));

        // A tabela continua existindo e o texto foi salvo exatamente como digitado
        var salvo = _repository.BuscarPorId(id);
        Assert.NotNull(salvo);
        Assert.Equal(nomeMalicioso, salvo!.Nome);
        Assert.Single(_repository.Listar());
    }

    [Fact]
    public void Atualizar_CategoriaComSqlInjection_AfetaApenasOProdutoInformado()
    {
        var id1 = _repository.Inserir(NovoProduto("Produto 1"));
        var id2 = _repository.Inserir(NovoProduto("Produto 2"));

        var produto = _repository.BuscarPorId(id1)!;
        produto.Categoria = "Hack' WHERE 1=1; --";
        _repository.Atualizar(produto);

        Assert.Equal("Hack' WHERE 1=1; --", _repository.BuscarPorId(id1)!.Categoria);
        Assert.Equal("Periféricos", _repository.BuscarPorId(id2)!.Categoria);
    }

    // ---------------- LISTAR / BUSCAR ----------------

    [Fact]
    public void Listar_RetornaProdutosOrdenadosPorId()
    {
        _repository.Inserir(NovoProduto("Primeiro"));
        _repository.Inserir(NovoProduto("Segundo"));
        _repository.Inserir(NovoProduto("Terceiro"));

        var lista = _repository.Listar();

        Assert.Equal(3, lista.Count);
        Assert.Equal(new[] { "Primeiro", "Segundo", "Terceiro" }, lista.Select(p => p.Nome));
    }

    [Fact]
    public void BuscarPorId_IdInexistente_RetornaNull()
    {
        Assert.Null(_repository.BuscarPorId(9999));
    }

    // ---------------- ATUALIZAR ----------------

    [Fact]
    public void Atualizar_ProdutoExistente_AlteraOsDados()
    {
        var id = _repository.Inserir(NovoProduto());
        var produto = _repository.BuscarPorId(id)!;

        produto.Nome = "Teclado Mecânico RGB";
        produto.Preco = 299.99m;
        produto.Estoque = 7;
        produto.Categoria = "Gamer";

        Assert.True(_repository.Atualizar(produto));

        var atualizado = _repository.BuscarPorId(id)!;
        Assert.Equal("Teclado Mecânico RGB", atualizado.Nome);
        Assert.Equal(299.99m, atualizado.Preco);
        Assert.Equal(7, atualizado.Estoque);
        Assert.Equal("Gamer", atualizado.Categoria);
    }

    [Fact]
    public void Atualizar_IdInexistente_RetornaFalse()
    {
        var produto = NovoProduto();
        produto.Id = 12345;

        Assert.False(_repository.Atualizar(produto));
    }

    // ---------------- EXCLUIR (soft delete) ----------------

    [Fact]
    public void Excluir_SoftDelete_OcultaDaListagemMasMantemNoBanco()
    {
        var id = _repository.Inserir(NovoProduto());

        Assert.True(_repository.Excluir(id));

        Assert.Null(_repository.BuscarPorId(id));
        Assert.Empty(_repository.Listar());

        var excluido = _repository.BuscarPorId(id, incluirExcluidos: true);
        Assert.NotNull(excluido);
        Assert.False(excluido!.Ativo);
        Assert.NotNull(excluido.DataExclusao);
    }

    [Fact]
    public void Excluir_ProdutoJaExcluido_RetornaFalse()
    {
        var id = _repository.Inserir(NovoProduto());
        _repository.Excluir(id);

        Assert.False(_repository.Excluir(id));
    }

    [Fact]
    public void Restaurar_ProdutoExcluido_VoltaAFicarAtivo()
    {
        var id = _repository.Inserir(NovoProduto());
        _repository.Excluir(id);

        Assert.True(_repository.Restaurar(id));

        var restaurado = _repository.BuscarPorId(id);
        Assert.NotNull(restaurado);
        Assert.True(restaurado!.Ativo);
        Assert.Null(restaurado.DataExclusao);
    }

    [Fact]
    public void Atualizar_ProdutoExcluidoLogicamente_NaoAltera()
    {
        var id = _repository.Inserir(NovoProduto());
        var produto = _repository.BuscarPorId(id)!;
        _repository.Excluir(id);

        produto.Nome = "Não deveria mudar";

        Assert.False(_repository.Atualizar(produto));
    }

    // ---------------- EXCLUIR definitivamente ----------------

    [Fact]
    public void ExcluirDefinitivamente_RemoveRegistroDoBanco()
    {
        var id = _repository.Inserir(NovoProduto());

        Assert.True(_repository.ExcluirDefinitivamente(id));
        Assert.Null(_repository.BuscarPorId(id, incluirExcluidos: true));
    }

    [Fact]
    public void ExcluirDefinitivamente_IdInexistente_RetornaFalse()
    {
        Assert.False(_repository.ExcluirDefinitivamente(4242));
    }

    // ---------------- TRATAMENTO DE ERROS / TRANSAÇÃO ----------------

    [Fact]
    public void Operacao_TabelaInexistente_LancaDataAccessExceptionERegistraLog()
    {
        // Simula um banco "quebrado": remove a tabela
        using (var conexao = new SqliteConnection(_connectionString))
        {
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DROP TABLE Produtos;";
            comando.ExecuteNonQuery();
        }

        var ex = Assert.Throws<DataAccessException>(() => _repository.Listar());

        Assert.IsType<SqliteException>(ex.InnerException);
        Assert.Contains(_logger.Mensagens, m => m.StartsWith("ERRO"));
    }

    [Fact]
    public void Inserir_ViolacaoDeRestricaoNoBanco_FazRollbackENaoGrava()
    {
        // Cria uma trigger que faz o INSERT falhar dentro da transação
        using (var conexao = new SqliteConnection(_connectionString))
        {
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = @"
                CREATE TRIGGER BloqueiaNomeProibido BEFORE INSERT ON Produtos
                WHEN NEW.Nome = 'PROIBIDO'
                BEGIN SELECT RAISE(ABORT, 'nome proibido'); END;";
            comando.ExecuteNonQuery();
        }

        var ex = Assert.Throws<DataAccessException>(() => _repository.Inserir(NovoProduto("PROIBIDO")));

        Assert.Contains("restrição", ex.Message);
        Assert.Empty(_repository.Listar(incluirExcluidos: true));
    }

    [Fact]
    public void Construtor_ConnectionStringVazia_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ProdutoRepository("", _logger));
    }

    [Fact]
    public void Operacoes_SaoRegistradasNoLog()
    {
        var id = _repository.Inserir(NovoProduto());
        _repository.Listar();
        _repository.Excluir(id);

        Assert.Contains(_logger.Mensagens, m => m.Contains("INSERIR"));
        Assert.Contains(_logger.Mensagens, m => m.Contains("LISTAR"));
        Assert.Contains(_logger.Mensagens, m => m.Contains("EXCLUIR"));
    }
}
