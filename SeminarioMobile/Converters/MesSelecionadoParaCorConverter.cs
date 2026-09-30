using System.Globalization;

namespace SeminarioMobile.Converters;

// Usado nas abas de mês (Seminários e Banner): value é o MesOpcao.Selecionado do
// próprio chip. ConverterParameter "bg" ou "texto" escolhe qual cor devolver, para
// reaproveitar o mesmo converter nos dois binds do chip (fundo e texto).
public class SelecionadoParaCorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selecionado = value is true;
        var ehTexto = string.Equals(parameter as string, "texto", StringComparison.OrdinalIgnoreCase);
        if (ehTexto)
            return selecionado ? Colors.White : Color.FromArgb("#1e293b");

        return selecionado ? Color.FromArgb("#2563EB") : Color.FromArgb("#E2E8F0");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
