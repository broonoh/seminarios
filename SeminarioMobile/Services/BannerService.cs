using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Data;
using SeminarioMobile.Helpers;
using SeminarioMobile.Models;
using SkiaSharp;

namespace SeminarioMobile.Services;

// Recria o banner desenhando com SkiaSharp e exporta como PNG para compartilhamento
// (imagem funciona em qualquer app — WhatsApp, redes sociais etc. — sem a fricção de um PDF).
public class BannerService(IDbContextFactory<AppDbContext> dbFactory)
{
    private const float PageWidth = 595f; // A4 a 72dpi, em pontos
    private const float Margin = 24f;
    private const int MaxLinhasClasse = 4;
    private const int MaxLinhasObs = 4;
    private const float AlturaLinhaClasse = 13f;
    private const float AlturaLinhaObs = 13f;

    private static readonly SKColor CorCabecalhoInicio = SKColor.Parse("#3B82F6");
    private static readonly SKColor CorCabecalhoFim = SKColor.Parse("#1D4ED8");
    private static readonly SKColor CorNavy = SKColor.Parse("#1e3a8a");
    private static readonly SKColor CorBordaCard = SKColor.Parse("#e2e8f0");
    private static readonly SKColor CorTagLocalBg = SKColor.Parse("#e2e8f0");
    private static readonly SKColor CorTagLocalTexto = SKColor.Parse("#475569");
    private static readonly SKColor CorValor = SKColor.Parse("#334155");
    private static readonly SKColor CorPrecoBg = SKColor.Parse("#f1f5f9");
    private static readonly SKColor CorPrecoValor = SKColor.Parse("#16a34a");
    private static readonly SKColor CorObsBg = SKColor.Parse("#fff7ed");
    private static readonly SKColor CorObsBorda = SKColor.Parse("#ea580c");
    private static readonly SKColor CorObsTexto = SKColor.Parse("#9a3412");
    private static readonly SKColor CorFooter = SKColor.Parse("#475569");
    private static readonly SKColor CorFundoPagina = SKColor.Parse("#f8fafc");
    private static readonly SKColor CorSubtitulo = SKColor.Parse("#DBEAFE");

    // Paleta usada no modelo "Colorido por Seminário" — uma cor de destaque por card,
    // em rotação, para diferenciar visualmente cada seminário no banner.
    // Só tons de azul, verde e vermelho (sem roxo/lilás/rosa).
    private static readonly SKColor[] PaletaSeminarios =
    [
        SKColor.Parse("#1e3a8a"), // azul-marinho
        SKColor.Parse("#166534"), // verde escuro
        SKColor.Parse("#7f1d1d"), // vermelho escuro
        SKColor.Parse("#1d4ed8"), // azul
        SKColor.Parse("#15803d"), // verde
        SKColor.Parse("#b91c1c"), // vermelho
        SKColor.Parse("#0369a1"), // azul petróleo
        SKColor.Parse("#4d7c0f"), // verde oliva
        SKColor.Parse("#dc2626"), // vermelho vivo
    ];

    private record DadosBanner(List<Seminario> Seminarios, string FooterTexto, string ObsTexto);

    // Layout já calculado antes de desenhar: quantas linhas cada texto ocupa e a
    // altura de cada card — evita que um nome de classe grande estoure o card
    // (a barra azul do cabeçalho) e garante que a altura total da imagem já
    // nasça correta, sem recortar conteúdo.
    private record LayoutCard(Seminario Seminario, List<string> LinhasClasse, float Altura);

    private record Layout(List<LayoutCard> Cards, List<string> LinhasObs, float AlturaObs, float AlturaTotal);

    private async Task<DadosBanner> CarregarDadosAsync(int ano, int mes)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var seminarios = (await db.Seminarios
                .Include(s => s.Local)
                .Include(s => s.Classe)
                .OrderBy(s => s.Id)
                .ToListAsync())
            .Where(s =>
            {
                var inicio = DataEventoHelper.ParseInicio(s.DataEvento);
                return inicio.Year == ano && inicio.Month == mes;
            })
            .ToList();

        var igrejaAtiva = await db.Igrejas.FirstOrDefaultAsync(i => i.Ativo);
        var footerTexto = igrejaAtiva?.Nome ?? "ICM HINTERLAND";

        var observacaoAtiva = await db.Observacoes.FirstOrDefaultAsync(o => o.Ativo);
        var obsTexto = observacaoAtiva?.Texto
            ?? "Os valores referem-se apenas à estadia, não incluindo transporte.";

        return new DadosBanner(seminarios, footerTexto, obsTexto);
    }

    // Gera o PNG do banner (sempre escopado a um mês) em um arquivo temporário pronto
    // para compartilhar/salvar. O nome do arquivo é único a cada chamada — se fosse
    // fixo por mês/modelo, o controle de Image da tela mostraria a versão em cache
    // mesmo depois dos dados mudarem (ex.: excluir um seminário) e o arquivo em disco
    // ser sobrescrito com o conteúdo novo.
    public async Task<string> GerarImagemAsync(int ano, int mes, TemplateBanner template = TemplateBanner.Padrao)
    {
        var dados = await CarregarDadosAsync(ano, mes);

        // O desenho (SkiaSharp) e a codificação do PNG são CPU-bound e demoram o
        // suficiente pra travar a UI perceptivelmente — como agora o preview é gerado
        // automaticamente a cada troca de mês/modelo (não só num botão), isso precisa
        // rodar fora da thread principal.
        return await Task.Run(() =>
        {
            var layout = CalcularLayout(dados);
            var mesRotulo = DataEventoHelper.RotuloMes(ano, mes);

            LimparImagensAntigas();
            var caminhoPng = Path.Combine(FileSystem.CacheDirectory, $"banner_{ano:D4}{mes:D2}_{template}_{DateTime.Now.Ticks}.png");
            const float escala = 3f;
            var info = new SKImageInfo((int)(PageWidth * escala), (int)(layout.AlturaTotal * escala));
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Scale(escala);
            Desenhar(canvas, dados, layout, template, mesRotulo);
            using var imagem = surface.Snapshot();
            using var dadosPng = imagem.Encode(SKEncodedImageFormat.Png, 100);
            using var arquivoPng = File.Create(caminhoPng);
            dadosPng.SaveTo(arquivoPng);

            return caminhoPng;
        });
    }

    // Evita acumular um PNG novo no cache a cada troca de mês/modelo/edição de dado —
    // só o último preview realmente precisa existir em disco.
    private static void LimparImagensAntigas()
    {
        foreach (var arquivo in Directory.EnumerateFiles(FileSystem.CacheDirectory, "banner_*.png"))
        {
            try { File.Delete(arquivo); }
            catch (IOException) { /* arquivo pode estar em uso (ex.: sendo compartilhado); ignora */ }
        }
    }

    private static Layout CalcularLayout(DadosBanner dados)
    {
        var contentWidth = PageWidth - Margin * 2;
        var largColuna1 = contentWidth * 0.42f;
        var larguraTextoClasse = largColuna1 - 20f;

        using var paintClasse = new SKPaint { TextSize = 11, Typeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold) };

        var cards = new List<LayoutCard>();
        foreach (var sem in dados.Seminarios)
        {
            var linhasClasse = QuebrarLinhas(paintClasse, sem.Classe?.Nome ?? "", larguraTextoClasse, MaxLinhasClasse);
            var alturaTextoClasse = linhasClasse.Count * AlturaLinhaClasse;
            var alturaColuna1 = alturaTextoClasse + 6 /* espaço até a tag */ + 15 /* tag do local */;
            const float alturaColuna2E3 = 36f; // data/inscrições e preço já cabem no card padrão
            var altura = MathF.Max(54f, MathF.Max(alturaColuna1, alturaColuna2E3) + 16f);
            cards.Add(new LayoutCard(sem, linhasClasse, altura));
        }

        using var paintObs = new SKPaint { TextSize = 10, Typeface = SKTypeface.FromFamilyName(null, SKFontStyle.Normal) };
        var linhasObs = QuebrarLinhas(paintObs, $"Obs: {dados.ObsTexto}", contentWidth - 24f, MaxLinhasObs);
        var alturaObs = MathF.Max(34f, linhasObs.Count * AlturaLinhaObs + 14f);

        var alturaTotal = Margin * 2 + 68 /* cabeçalho */
            + cards.Sum(c => c.Altura + 6)
            + alturaObs + 10
            + 40 /* rodapé */;

        return new Layout(cards, linhasObs, alturaObs, alturaTotal);
    }

    private static void Desenhar(SKCanvas canvas, DadosBanner dados, Layout layout, TemplateBanner template, string mesRotulo)
    {
        canvas.Clear(CorFundoPagina);
        var contentWidth = PageWidth - Margin * 2;
        var y = Margin;

        // Cabeçalho
        var rectCabecalho = new SKRect(Margin, y, Margin + contentWidth, y + 60);
        using (var paintFundo = new SKPaint { IsAntialias = true })
        {
            paintFundo.Shader = SKShader.CreateLinearGradient(
                new SKPoint(rectCabecalho.Left, rectCabecalho.Top),
                new SKPoint(rectCabecalho.Right, rectCabecalho.Bottom),
                [CorCabecalhoInicio, CorCabecalhoFim],
                null,
                SKShaderTileMode.Clamp);
            canvas.DrawRoundRect(new SKRoundRect(rectCabecalho, 6), paintFundo);
        }
        DesenharTexto(canvas, "INSCRIÇÕES PARA SEMINÁRIOS", Margin + contentWidth / 2, y + 28, 18, SKColors.White, true, SKTextAlign.Center);
        DesenharTexto(canvas, $"Mês de {mesRotulo}", Margin + contentWidth / 2, y + 46, 12, CorSubtitulo, false, SKTextAlign.Center);
        y += 60 + 8;

        var largColuna1 = contentWidth * 0.42f;
        var largColuna3 = contentWidth * 0.23f;

        for (var i = 0; i < layout.Cards.Count; i++)
        {
            var card = layout.Cards[i];
            var sem = card.Seminario;
            var alturaCard = card.Altura;
            var corDestaque = template == TemplateBanner.ColoridoPorSeminario
                ? PaletaSeminarios[i % PaletaSeminarios.Length]
                : CorNavy;

            var rect = new SKRect(Margin, y, Margin + contentWidth, y + alturaCard);
            using (var paintFundo = new SKPaint { Color = SKColors.White, IsAntialias = true })
                canvas.DrawRoundRect(new SKRoundRect(rect, 6), paintFundo);
            using (var paintBorda = new SKPaint { Color = CorBordaCard, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true })
                canvas.DrawRoundRect(new SKRoundRect(rect, 6), paintBorda);

            if (template == TemplateBanner.ColoridoPorSeminario)
            {
                var rectFaixa = new SKRect(Margin + 1, y + 3, Margin + 6, y + alturaCard - 3);
                using var paintFaixa = new SKPaint { Color = corDestaque, IsAntialias = true };
                canvas.DrawRoundRect(new SKRoundRect(rectFaixa, 2), paintFaixa);
            }

            var xColuna1 = Margin + 12;
            var xColuna2 = Margin + largColuna1 + 14;
            var xColuna3 = Margin + contentWidth - largColuna3;

            using (var paintDivisor = new SKPaint { Color = corDestaque, StrokeWidth = 2, IsAntialias = true })
                canvas.DrawLine(Margin + largColuna1, y + 8, Margin + largColuna1, y + alturaCard - 8, paintDivisor);

            var yClasse = y + 20;
            foreach (var linha in card.LinhasClasse)
            {
                DesenharTexto(canvas, linha, xColuna1, yClasse, 11, corDestaque, true, SKTextAlign.Left);
                yClasse += AlturaLinhaClasse;
            }
            DesenharTagLocal(canvas, sem.Local?.Nome ?? "", xColuna1, yClasse - AlturaLinhaClasse + 10);

            DesenharTexto(canvas, $"Data: {sem.DataEvento}", xColuna2, y + 20, 9.5f, CorValor, true, SKTextAlign.Left);
            DesenharTexto(canvas, $"Inscrições até: {sem.InscricoesAte}", xColuna2, y + 36, 9.5f, CorValor, true, SKTextAlign.Left);

            var rectPreco = new SKRect(xColuna3, y + 8, Margin + contentWidth - 8, y + alturaCard - 8);
            using (var paintPrecoFundo = new SKPaint { Color = CorPrecoBg, IsAntialias = true })
                canvas.DrawRoundRect(new SKRoundRect(rectPreco, 4), paintPrecoFundo);

            DesenharTexto(canvas, $"Adulto R$ {sem.ValorAdulto:N2}", rectPreco.Right - 8, y + 24, 10.5f, CorPrecoValor, true, SKTextAlign.Right);
            if (sem.ValorCrianca is not null)
                DesenharTexto(canvas, $"Criança R$ {sem.ValorCrianca:N2}", rectPreco.Right - 8, y + 40, 10.5f, CorPrecoValor, true, SKTextAlign.Right);

            y += alturaCard + 6;
        }

        var rectObs = new SKRect(Margin, y, Margin + contentWidth, y + layout.AlturaObs);
        using (var paintObsFundo = new SKPaint { Color = CorObsBg, IsAntialias = true })
            canvas.DrawRect(rectObs, paintObsFundo);
        using (var paintObsBorda = new SKPaint { Color = CorObsBorda, StrokeWidth = 3, IsAntialias = true })
            canvas.DrawLine(Margin, y, Margin, y + layout.AlturaObs, paintObsBorda);
        var yObs = y + 15;
        foreach (var linha in layout.LinhasObs)
        {
            DesenharTexto(canvas, linha, Margin + 12, yObs, 10, CorObsTexto, false, SKTextAlign.Left);
            yObs += AlturaLinhaObs;
        }
        y += layout.AlturaObs + 10;

        using (var paintLinha = new SKPaint { Color = CorBordaCard, StrokeWidth = 1, IsAntialias = true })
            canvas.DrawLine(Margin, y, Margin + contentWidth, y, paintLinha);
        DesenharTexto(canvas, dados.FooterTexto, Margin + contentWidth / 2, y + 22, 13, CorFooter, true, SKTextAlign.Center);
    }

    private static void DesenharTexto(SKCanvas canvas, string texto, float x, float y, float tamanho, SKColor cor, bool negrito, SKTextAlign alinhamento)
    {
        using var paint = new SKPaint
        {
            Color = cor,
            TextSize = tamanho,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, negrito ? SKFontStyle.Bold : SKFontStyle.Normal),
            TextAlign = alinhamento,
        };
        canvas.DrawText(texto, x, y, paint);
    }

    private static void DesenharTagLocal(SKCanvas canvas, string texto, float x, float y)
    {
        using var paintMedida = new SKPaint { TextSize = 8.5f, Typeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold) };
        var largura = paintMedida.MeasureText(texto) + 12;

        using var paintFundo = new SKPaint { Color = CorTagLocalBg, IsAntialias = true };
        var rect = new SKRect(x, y, x + largura, y + 15);
        canvas.DrawRoundRect(new SKRoundRect(rect, 3), paintFundo);
        DesenharTexto(canvas, texto, x + 6, y + 11, 8.5f, CorTagLocalTexto, true, SKTextAlign.Left);
    }

    // Quebra o texto em linhas que cabem em larguraMax, respeitando espaços entre
    // palavras. Uma palavra sozinha maior que larguraMax (texto colado sem espaço)
    // é cortada por caractere. Acima de maxLinhas, corta e acrescenta "…" — proteção
    // contra um texto absurdamente grande esticar o banner sem limite.
    private static List<string> QuebrarLinhas(SKPaint paint, string texto, float larguraMax, int maxLinhas)
    {
        var linhas = new List<string>();
        var linhaAtual = "";

        void FecharLinha()
        {
            linhas.Add(linhaAtual);
            linhaAtual = "";
        }

        var palavras = (texto ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var palavra in palavras)
        {
            var candidata = linhaAtual.Length == 0 ? palavra : $"{linhaAtual} {palavra}";
            if (paint.MeasureText(candidata) <= larguraMax)
            {
                linhaAtual = candidata;
                continue;
            }

            if (linhaAtual.Length > 0)
                FecharLinha();

            var restante = palavra;
            while (paint.MeasureText(restante) > larguraMax && restante.Length > 1)
            {
                var corte = restante.Length;
                while (corte > 1 && paint.MeasureText(restante[..corte]) > larguraMax)
                    corte--;
                linhas.Add(restante[..corte]);
                restante = restante[corte..];
            }
            linhaAtual = restante;
        }

        if (linhaAtual.Length > 0 || linhas.Count == 0)
            FecharLinha();

        if (linhas.Count > maxLinhas)
        {
            linhas = linhas.Take(maxLinhas).ToList();
            var ultima = linhas[^1];
            while (ultima.Length > 1 && paint.MeasureText(ultima + "…") > larguraMax)
                ultima = ultima[..^1];
            linhas[^1] = ultima + "…";
        }

        return linhas;
    }

}
