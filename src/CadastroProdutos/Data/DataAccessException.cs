namespace CadastroProdutos.Data;

/// <summary>
/// Exceção da camada de dados com uma mensagem amigável para o usuário.
/// A exceção original do provedor (SqliteException) fica em InnerException.
/// </summary>
public class DataAccessException : Exception
{
    public DataAccessException(string mensagem, Exception? excecaoOriginal = null)
        : base(mensagem, excecaoOriginal)
    {
    }
}
