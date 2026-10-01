using System.Globalization;
using CadastroProdutos.Data;
using CadastroProdutos.Logging;
using CadastroProdutos.Models;

namespace CadastroProdutos.UI;

/// <summary>
/// Camada de INTERFACE: exibe o menu e conversa com o usuário.
/// Não contém nenhum SQL — todo acesso ao banco é delegado ao ProdutoRepository.
/// </summary>
public class MenuProdutos
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly ProdutoRepository _repository;
    private readonly IOperacaoLogger _logger;

    public MenuProdutos(ProdutoRepository repository, IOperacaoLogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public void Executar()
    {
        _logger.Info("Aplicação iniciada.");

        try
        {
            var continuar = true;
            while (continuar)
            {
                ExibirMenu();
                var opcao = ConsoleHelper.LerLinha();

                try
                {
                    switch (opcao)
                    {
                        case "1": InserirProduto(); break;
                        case "2": ListarProdutos(); break;
                        case "3": BuscarProdutoPorId(); break;
                        case "4": AtualizarProduto(); break;
                        case "5": ExcluirProduto(); break;
                        case "6": continuar = false; break;
                        default:
                            ConsoleHelper.Erro("Opção inválida. Escolha um número de 1 a 6.");
                            break;
                    }
                }
                catch (DataAccessException ex)
                {
                    // Erros de banco já foram registrados no log pelo Repository
                    ConsoleHelper.Erro(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    ConsoleHelper.Erro($"Dados inválidos: {ex.Message}");
                }

                if (continuar)
                    ConsoleHelper.Pausar();
            }
        }
        catch (ConsoleHelper.EntradaEncerradaException)
        {
            Console.WriteLine();
        }

        _logger.Info("Aplicação encerrada pelo usuário.");
        ConsoleHelper.Sucesso("Até logo!");
    }

    private static void ExibirMenu()
    {
        ConsoleHelper.LimparTela();
        Console.WriteLine();
        ConsoleHelper.Escrever("╔══════════════════════════════════════╗", ConsoleColor.Cyan);
        ConsoleHelper.Escrever("║        CADASTRO DE PRODUTOS          ║", ConsoleColor.Cyan);
        ConsoleHelper.Escrever("║   ADO.NET + SQLite  |  RM 556417     ║", ConsoleColor.Cyan);
        ConsoleHelper.Escrever("╚══════════════════════════════════════╝", ConsoleColor.Cyan);
        Console.WriteLine("  1 - Inserir produto");
        Console.WriteLine("  2 - Listar produtos");
        Console.WriteLine("  3 - Buscar produto por ID");
        Console.WriteLine("  4 - Atualizar produto");
        Console.WriteLine("  5 - Excluir produto");
        Console.WriteLine("  6 - Sair");
        Console.WriteLine();
        Console.Write("Escolha uma opção: ");
    }

    // 1 - INSERIR ------------------------------------------------------
    private void InserirProduto()
    {
        ConsoleHelper.Titulo("Inserir produto");

        var produto = new Produto
        {
            Nome = ConsoleHelper.LerTexto("Nome", Produto.TamanhoMaximoNome),
            Preco = ConsoleHelper.LerDecimal("Preço (R$)", 0m),
            Estoque = ConsoleHelper.LerInteiro("Estoque", 0),
            Categoria = ConsoleHelper.LerTexto("Categoria", Produto.TamanhoMaximoCategoria)
        };

        var id = _repository.Inserir(produto);
        ConsoleHelper.Sucesso($"Produto cadastrado com sucesso! Id gerado: {id}");
    }

    // 2 - LISTAR -------------------------------------------------------
    private void ListarProdutos()
    {
        ConsoleHelper.Titulo("Listar produtos");

        var incluirExcluidos = ConsoleHelper.Confirmar("Incluir produtos excluídos (soft delete)?");
        var produtos = _repository.Listar(incluirExcluidos);

        if (produtos.Count == 0)
        {
            ConsoleHelper.Aviso("Nenhum produto cadastrado.");
            return;
        }

        ExibirTabela(produtos);
        Console.WriteLine($"Total: {produtos.Count} produto(s).");
    }

    // 3 - BUSCAR POR ID ------------------------------------------------
    private void BuscarProdutoPorId()
    {
        ConsoleHelper.Titulo("Buscar produto por ID");

        var id = ConsoleHelper.LerInteiro("Informe o ID", 1);
        var produto = _repository.BuscarPorId(id);

        if (produto is null)
        {
            ConsoleHelper.Aviso($"Produto com ID {id} não encontrado.");
            return;
        }

        ExibirDetalhes(produto);
    }

    // 4 - ATUALIZAR ----------------------------------------------------
    private void AtualizarProduto()
    {
        ConsoleHelper.Titulo("Atualizar produto");

        var id = ConsoleHelper.LerInteiro("Informe o ID do produto", 1);
        var produto = _repository.BuscarPorId(id);

        if (produto is null)
        {
            ConsoleHelper.Aviso($"Produto com ID {id} não encontrado.");
            return;
        }

        ExibirDetalhes(produto);
        Console.WriteLine();
        Console.WriteLine("Informe os novos valores (pressione ENTER para manter o valor atual):");

        produto.Nome = ConsoleHelper.LerTexto("Nome", Produto.TamanhoMaximoNome, produto.Nome);
        produto.Preco = ConsoleHelper.LerDecimal("Preço (R$)", 0m, produto.Preco);
        produto.Estoque = ConsoleHelper.LerInteiro("Estoque", 0, produto.Estoque);
        produto.Categoria = ConsoleHelper.LerTexto("Categoria", Produto.TamanhoMaximoCategoria, produto.Categoria);

        if (!ConsoleHelper.Confirmar("Confirma a atualização?"))
        {
            ConsoleHelper.Aviso("Atualização cancelada.");
            return;
        }

        if (_repository.Atualizar(produto))
            ConsoleHelper.Sucesso("Produto atualizado com sucesso!");
        else
            ConsoleHelper.Aviso("Nenhum registro foi atualizado (o produto pode ter sido excluído).");
    }

    // 5 - EXCLUIR ------------------------------------------------------
    private void ExcluirProduto()
    {
        ConsoleHelper.Titulo("Excluir produto");

        Console.WriteLine("  1 - Exclusão lógica (soft delete - pode ser restaurado)");
        Console.WriteLine("  2 - Exclusão definitiva (remove do banco)");
        Console.WriteLine("  3 - Restaurar produto excluído logicamente");
        Console.WriteLine("  0 - Voltar");
        var tipo = ConsoleHelper.LerInteiro("Escolha", 0);

        switch (tipo)
        {
            case 0:
                return;
            case 1:
                ExcluirLogicamente();
                break;
            case 2:
                ExcluirDefinitivamente();
                break;
            case 3:
                RestaurarProduto();
                break;
            default:
                ConsoleHelper.Erro("Opção inválida.");
                break;
        }
    }

    private void ExcluirLogicamente()
    {
        var id = ConsoleHelper.LerInteiro("Informe o ID do produto", 1);
        var produto = _repository.BuscarPorId(id);
        if (produto is null)
        {
            ConsoleHelper.Aviso($"Produto com ID {id} não encontrado.");
            return;
        }

        ExibirDetalhes(produto);
        if (!ConsoleHelper.Confirmar("Deseja realmente excluir este produto?"))
        {
            ConsoleHelper.Aviso("Exclusão cancelada.");
            return;
        }

        if (_repository.Excluir(id))
            ConsoleHelper.Sucesso("Produto excluído com sucesso (exclusão lógica).");
        else
            ConsoleHelper.Aviso("Nenhum registro foi excluído.");
    }

    private void ExcluirDefinitivamente()
    {
        var id = ConsoleHelper.LerInteiro("Informe o ID do produto", 1);
        var produto = _repository.BuscarPorId(id, incluirExcluidos: true);
        if (produto is null)
        {
            ConsoleHelper.Aviso($"Produto com ID {id} não encontrado.");
            return;
        }

        ExibirDetalhes(produto);
        ConsoleHelper.Aviso("ATENÇÃO: esta operação não pode ser desfeita!");
        if (!ConsoleHelper.Confirmar("Deseja realmente remover definitivamente?"))
        {
            ConsoleHelper.Aviso("Exclusão cancelada.");
            return;
        }

        if (_repository.ExcluirDefinitivamente(id))
            ConsoleHelper.Sucesso("Produto removido definitivamente do banco.");
        else
            ConsoleHelper.Aviso("Nenhum registro foi removido.");
    }

    private void RestaurarProduto()
    {
        var id = ConsoleHelper.LerInteiro("Informe o ID do produto", 1);

        if (_repository.Restaurar(id))
            ConsoleHelper.Sucesso($"Produto {id} restaurado com sucesso.");
        else
            ConsoleHelper.Aviso($"Não há produto excluído com ID {id}.");
    }

    // Exibição ---------------------------------------------------------
    private static void ExibirTabela(IEnumerable<Produto> produtos)
    {
        const string formato = "{0,-5} {1,-32} {2,14} {3,8}  {4,-18} {5,-8}";
        var cabecalho = string.Format(formato, "ID", "NOME", "PREÇO", "ESTOQUE", "CATEGORIA", "STATUS");

        Console.WriteLine();
        ConsoleHelper.Escrever(cabecalho, ConsoleColor.White);
        Console.WriteLine(new string('-', cabecalho.Length));

        foreach (var p in produtos)
        {
            var linha = string.Format(formato,
                p.Id,
                Truncar(p.Nome, 32),
                p.Preco.ToString("C", PtBr),
                p.Estoque,
                Truncar(p.Categoria, 18),
                p.Ativo ? "Ativo" : "Excluído");

            if (p.Ativo)
                Console.WriteLine(linha);
            else
                ConsoleHelper.Escrever(linha, ConsoleColor.DarkGray);
        }

        Console.WriteLine(new string('-', cabecalho.Length));
    }

    private static void ExibirDetalhes(Produto p)
    {
        Console.WriteLine();
        Console.WriteLine($"  ID ............: {p.Id}");
        Console.WriteLine($"  Nome ..........: {p.Nome}");
        Console.WriteLine($"  Preço .........: {p.Preco.ToString("C", PtBr)}");
        Console.WriteLine($"  Estoque .......: {p.Estoque}");
        Console.WriteLine($"  Categoria .....: {p.Categoria}");
        Console.WriteLine($"  Cadastrado em .: {p.DataCadastro:dd/MM/yyyy HH:mm:ss}");
        Console.WriteLine($"  Status ........: {(p.Ativo ? "Ativo" : $"Excluído em {p.DataExclusao:dd/MM/yyyy HH:mm:ss}")}");
    }

    private static string Truncar(string texto, int tamanho) =>
        texto.Length <= tamanho ? texto : texto[..(tamanho - 3)] + "...";
}
