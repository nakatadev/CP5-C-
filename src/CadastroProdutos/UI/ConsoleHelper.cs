using System.Globalization;

namespace CadastroProdutos.UI;

/// <summary>
/// Leitura e escrita no console com validação de entrada.
/// </summary>
public static class ConsoleHelper
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>Lançada quando a entrada padrão termina (ex.: Ctrl+Z / fim do arquivo redirecionado).</summary>
    public class EntradaEncerradaException : Exception
    {
    }

    public static string LerLinha()
    {
        var linha = Console.ReadLine();
        if (linha is null)
            throw new EntradaEncerradaException();

        // Com entrada redirecionada (script), o texto não aparece na tela: repete para manter o histórico legível
        if (Console.IsInputRedirected)
            Console.WriteLine(linha);

        return linha.Trim();
    }

    public static string LerTexto(string rotulo, int tamanhoMaximo, string? valorAtual = null)
    {
        while (true)
        {
            Console.Write(Rotulo(rotulo, valorAtual));
            var texto = LerLinha();

            if (texto.Length == 0 && valorAtual is not null)
                return valorAtual;

            if (texto.Length == 0)
            {
                Erro("Campo obrigatório.");
                continue;
            }

            if (texto.Length > tamanhoMaximo)
            {
                Erro($"Informe no máximo {tamanhoMaximo} caracteres.");
                continue;
            }

            return texto;
        }
    }

    public static decimal LerDecimal(string rotulo, decimal minimo, decimal? valorAtual = null)
    {
        while (true)
        {
            Console.Write(Rotulo(rotulo, valorAtual?.ToString("N2", PtBr)));
            var texto = LerLinha();

            if (texto.Length == 0 && valorAtual.HasValue)
                return valorAtual.Value;

            // Aceita "10,50" (pt-BR) e "10.50"
            texto = texto.Replace("R$", "").Trim();
            if (!texto.Contains(',') && texto.Contains('.'))
                texto = texto.Replace('.', ',');

            if (decimal.TryParse(texto, NumberStyles.Number, PtBr, out var valor) && valor >= minimo)
                return Math.Round(valor, 2);

            Erro($"Valor inválido. Informe um número maior ou igual a {minimo.ToString("N2", PtBr)}.");
        }
    }

    public static int LerInteiro(string rotulo, int minimo, int? valorAtual = null)
    {
        while (true)
        {
            Console.Write(Rotulo(rotulo, valorAtual?.ToString()));
            var texto = LerLinha();

            if (texto.Length == 0 && valorAtual.HasValue)
                return valorAtual.Value;

            if (int.TryParse(texto, NumberStyles.Integer, PtBr, out var valor) && valor >= minimo)
                return valor;

            Erro($"Valor inválido. Informe um número inteiro maior ou igual a {minimo}.");
        }
    }

    public static bool Confirmar(string pergunta)
    {
        while (true)
        {
            Console.Write($"{pergunta} (S/N): ");
            var resposta = LerLinha().ToUpperInvariant();
            if (resposta is "S" or "SIM") return true;
            if (resposta is "N" or "NAO" or "NÃO") return false;
            Erro("Responda com S ou N.");
        }
    }

    public static void Titulo(string texto)
    {
        Console.WriteLine();
        Escrever($"===== {texto.ToUpperInvariant()} =====", ConsoleColor.Cyan);
    }

    public static void Sucesso(string mensagem) => Escrever($"[OK] {mensagem}", ConsoleColor.Green);

    public static void Aviso(string mensagem) => Escrever($"[!] {mensagem}", ConsoleColor.Yellow);

    public static void Erro(string mensagem) => Escrever($"[ERRO] {mensagem}", ConsoleColor.Red);

    public static void Escrever(string mensagem, ConsoleColor cor)
    {
        var corAnterior = Console.ForegroundColor;
        Console.ForegroundColor = cor;
        Console.WriteLine(mensagem);
        Console.ForegroundColor = corAnterior;
    }

    public static void Pausar()
    {
        Console.WriteLine();
        Console.Write("Pressione ENTER para voltar ao menu...");
        LerLinha();
    }

    public static void LimparTela()
    {
        // Console.Clear() falha quando a saída está redirecionada (ex.: em testes/scripts)
        if (!Console.IsOutputRedirected)
            Console.Clear();
    }

    private static string Rotulo(string rotulo, string? valorAtual) =>
        valorAtual is null ? $"{rotulo}: " : $"{rotulo} [{valorAtual}] (ENTER mantém): ";
}
