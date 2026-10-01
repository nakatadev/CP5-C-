using System.Globalization;
using System.Text;
using CadastroProdutos.Data;
using CadastroProdutos.Logging;
using CadastroProdutos.UI;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

// ------------------------------------------------------------------
// Cadastro de Produtos - ADO.NET
// Aluno: Rodrigo Nakata - RM 556417
// ------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;
if (Console.IsInputRedirected)
    Console.SetIn(new StreamReader(Console.OpenStandardInput(), Encoding.UTF8));
else
    Console.InputEncoding = Encoding.UTF8;

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

var pastaBase = AppContext.BaseDirectory;

// 1) Lê as configurações do appsettings.json
IConfiguration configuracao;
try
{
    configuracao = new ConfigurationBuilder()
        .SetBasePath(pastaBase)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .Build();
}
catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or FormatException)
{
    ConsoleHelper.Erro($"Não foi possível ler o appsettings.json: {ex.Message}");
    return 1;
}

var connectionString = configuracao.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    ConsoleHelper.Erro("A connection string 'DefaultConnection' não foi encontrada no appsettings.json.");
    return 1;
}

// Caminhos relativos (banco, log e script) são resolvidos a partir da pasta do executável
connectionString = ResolverCaminhoBanco(connectionString, pastaBase);
var caminhoLog = Path.Combine(pastaBase, configuracao["Logging:FilePath"] ?? "logs/operacoes.log");
var caminhoScript = Path.Combine(pastaBase, configuracao["Database:ScriptPath"] ?? "database/script.sql");

var logger = new FileLogger(caminhoLog);

// 2) Cria a tabela (se ainda não existir) executando o script .sql
try
{
    new DatabaseInitializer(connectionString, logger).Inicializar(caminhoScript);
}
catch (DataAccessException ex)
{
    ConsoleHelper.Erro(ex.Message);
    if (ex.InnerException is not null)
        ConsoleHelper.Erro($"Detalhe técnico: {ex.InnerException.Message}");
    return 1;
}

// 3) Monta as camadas: Repository (dados) -> Menu (interface)
var repository = new ProdutoRepository(connectionString, logger);
var menu = new MenuProdutos(repository, logger);

try
{
    menu.Executar();
}
catch (Exception ex)
{
    // Última barreira: qualquer erro inesperado é registrado e exibido sem "quebrar" a aplicação
    logger.Erro("Erro inesperado na aplicação.", ex);
    ConsoleHelper.Erro($"Erro inesperado: {ex.Message}");
    return 1;
}

return 0;

static string ResolverCaminhoBanco(string connectionString, string pastaBase)
{
    var builder = new SqliteConnectionStringBuilder(connectionString);
    if (!string.IsNullOrWhiteSpace(builder.DataSource)
        && builder.DataSource != ":memory:"
        && !Path.IsPathRooted(builder.DataSource))
    {
        builder.DataSource = Path.Combine(pastaBase, builder.DataSource);
    }
    return builder.ToString();
}
