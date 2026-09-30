using System.Globalization;
using SeminarioMobile.Models;

namespace SeminarioMobile.Helpers;

// Centraliza o parsing do campo livre Seminario.DataEvento ("dd/MM/yyyy" ou
// "dd/MM/yyyy a dd/MM/yyyy") e a derivação das abas de mês usadas em Seminários e Banner.
public static class DataEventoHelper
{
    private const string Formato = "dd/MM/yyyy";

    public static DateTime ParseInicio(string dataEvento)
    {
        var primeira = dataEvento.Split(" a ", StringSplitOptions.TrimEntries)[0];
        return DateTime.TryParseExact(primeira, Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : DateTime.Today;
    }

    public static string RotuloMes(int ano, int mes)
    {
        var cultura = new CultureInfo("pt-BR");
        var nome = cultura.TextInfo.ToTitleCase(new DateTime(ano, mes, 1).ToString("MMMM", cultura));
        return $"{nome}/{ano}";
    }

    public static List<MesOpcao> ExtrairMeses(IEnumerable<Seminario> seminarios, bool incluirTodos)
    {
        var meses = seminarios
            .Select(s => ParseInicio(s.DataEvento))
            .Select(d => (d.Year, d.Month))
            .Distinct()
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .Select(m => new MesOpcao { Ano = m.Year, Mes = m.Month, Rotulo = RotuloMes(m.Year, m.Month) })
            .ToList();

        if (incluirTodos)
            meses.Insert(0, new MesOpcao { EhTodos = true, Rotulo = "Todos" });

        return meses;
    }
}
