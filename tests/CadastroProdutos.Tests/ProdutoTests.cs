using CadastroProdutos.Models;

namespace CadastroProdutos.Tests;

public class ProdutoTests
{
    [Fact]
    public void Validar_ProdutoValido_NaoRetornaErros()
    {
        var produto = new Produto { Nome = "Monitor 24\"", Preco = 899.90m, Estoque = 3, Categoria = "Monitores" };

        Assert.Empty(produto.Validar());
    }

    [Fact]
    public void Validar_ProdutoVazio_RetornaErrosDeNomeECategoria()
    {
        var erros = new Produto().Validar();

        Assert.Contains(erros, e => e.Contains("nome"));
        Assert.Contains(erros, e => e.Contains("categoria"));
    }

    [Fact]
    public void Validar_NomeMuitoLongo_RetornaErro()
    {
        var produto = new Produto
        {
            Nome = new string('A', Produto.TamanhoMaximoNome + 1),
            Preco = 1,
            Estoque = 1,
            Categoria = "Teste"
        };

        Assert.Single(produto.Validar());
    }

    [Fact]
    public void Validar_PrecoEEstoqueNegativos_RetornaDoisErros()
    {
        var produto = new Produto { Nome = "X", Preco = -1, Estoque = -1, Categoria = "Y" };

        Assert.Equal(2, produto.Validar().Count);
    }
}
