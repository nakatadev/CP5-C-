using CadastroProdutos.Logging;

namespace CadastroProdutos.Tests;

/// <summary>Logger falso que guarda as mensagens em memória para conferência nos testes.</summary>
public class LoggerEmMemoria : IOperacaoLogger
{
    public List<string> Mensagens { get; } = new();

    public void Info(string mensagem) => Mensagens.Add($"INFO {mensagem}");

    public void Aviso(string mensagem) => Mensagens.Add($"AVISO {mensagem}");

    public void Erro(string mensagem, Exception? excecao = null) => Mensagens.Add($"ERRO {mensagem}");
}
