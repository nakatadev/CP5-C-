using System.Text;

namespace CadastroProdutos.Logging;

/// <summary>
/// Grava as operações da aplicação em um arquivo texto (uma linha por evento).
/// Exemplo: 2026-09-30 14:05:12 [INFO ] Produto inserido: Id=4, Nome='Teclado'
/// </summary>
public class FileLogger : IOperacaoLogger
{
    private readonly string _caminhoArquivo;
    private readonly object _lock = new();

    public FileLogger(string caminhoArquivo)
    {
        _caminhoArquivo = caminhoArquivo;

        var pasta = Path.GetDirectoryName(_caminhoArquivo);
        if (!string.IsNullOrEmpty(pasta))
            Directory.CreateDirectory(pasta);
    }

    public string CaminhoArquivo => _caminhoArquivo;

    public void Info(string mensagem) => Escrever("INFO ", mensagem);

    public void Aviso(string mensagem) => Escrever("AVISO", mensagem);

    public void Erro(string mensagem, Exception? excecao = null)
    {
        var texto = excecao is null
            ? mensagem
            : $"{mensagem} | {excecao.GetType().Name}: {excecao.Message}";
        Escrever("ERRO ", texto);
    }

    private void Escrever(string nivel, string mensagem)
    {
        var linha = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{nivel}] {mensagem}{Environment.NewLine}";

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_caminhoArquivo, linha, Encoding.UTF8);
            }
            catch (IOException)
            {
                // Uma falha no log não pode derrubar a aplicação.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
