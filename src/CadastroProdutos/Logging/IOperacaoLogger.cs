namespace CadastroProdutos.Logging;

/// <summary>
/// Contrato para registro das operações realizadas pela aplicação.
/// </summary>
public interface IOperacaoLogger
{
    void Info(string mensagem);
    void Aviso(string mensagem);
    void Erro(string mensagem, Exception? excecao = null);
}
