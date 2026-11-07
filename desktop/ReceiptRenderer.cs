using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Drawing.Printing;
using QRCoder;

namespace BarbaCupom;

public static class ReceiptRenderer
{
    public const int Width = 576; // 72 mm × 8 pontos/mm na cabeça de 203 dpi.
    static readonly Brush Ink = Brushes.Black;
    static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static Bitmap Render(Receipt receipt, string? logoPath = null, string? pixPayload = null)
    {
        using var canvas = new Bitmap(Width, 2600, PixelFormat.Format24bppRgb);
        canvas.SetResolution(203, 203);
        using var g = Graphics.FromImage(canvas);
        g.Clear(Color.White);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        g.SmoothingMode = SmoothingMode.HighQuality;
        using var body = new Font("Arial", 18, FontStyle.Regular, GraphicsUnit.Pixel);
        using var bold = new Font("Arial", 19, FontStyle.Bold, GraphicsUnit.Pixel);
        using var small = new Font("Arial", 15, FontStyle.Regular, GraphicsUnit.Pixel);
        using var title = new Font("Arial", 25, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brand = new Font("Georgia", 24, FontStyle.Bold, GraphicsUnit.Pixel);
        using var center = new StringFormat { Alignment = StringAlignment.Center };
        using var right = new StringFormat { Alignment = StringAlignment.Far };
        using var pen = new Pen(Color.Black, 2);
        float y = 16;
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            using var logo = BuildThermalLogo(logoPath);
            g.DrawImage(logo, (Width - 140) / 2, (int)y, 140, 140);
            y += 150;
        }
        else { g.DrawEllipse(pen, 222, (int)y, 132, 105); g.DrawString("BD", brand, Ink, new RectangleF(222, y + 30, 132, 50), center); y += 115; }
        g.DrawString("BARBA DEFUMADOS", brand, Ink, new RectangleF(10, y, 556, 32), center); y += 32;
        g.DrawString("B B Q", small, Ink, new RectangleF(10, y, 556, 24), center); y += 38;
        g.DrawString("CUPOM NÃO FISCAL", title, Ink, new RectangleF(10, y, 556, 34), center); y += 34;
        g.DrawString("COMPROVANTE DE PEDIDO", bold, Ink, new RectangleF(10, y, 556, 28), center); y += 37;
        y = DrawWrap(g, $"Cliente: {receipt.CustomerName}", body, 16, y, 544);
        if (!string.IsNullOrWhiteSpace(receipt.Phone)) y = DrawWrap(g, $"Telefone: {receipt.Phone}", body, 16, y, 544);
        var stamp = receipt.IssuedAt == DateTime.MinValue ? "Data e hora: ao emitir" : $"Data: {receipt.IssuedAt:dd/MM/yyyy}    Hora: {receipt.IssuedAt:HH:mm}";
        y = DrawWrap(g, stamp, body, 16, y, 544);
        if (!string.IsNullOrWhiteSpace(receipt.Address)) y = DrawWrap(g, $"Endereço: {receipt.Address}", body, 16, y, 544);
        y += 10;
        g.DrawLine(pen, 16, y, 560, y); y += 7;
        g.DrawString("QTD.  PRODUTO", bold, Ink, 16, y);
        g.DrawString("TOTAL", bold, Ink, new RectangleF(442, y, 118, 28), right); y += 33;
        foreach (var line in receipt.Lines)
        {
            var name = $"{line.QuantityLabel}  {line.Name}";
            var h = Math.Max(32, g.MeasureString(name, body, 405).Height + 4);
            g.DrawString(name, body, Ink, new RectangleF(16, y, 405, h));
            g.DrawString((line.TotalCents / 100m).ToString("N2", Br), body, Ink, new RectangleF(425, y, 135, 30), right);
            y += h + 3;
        }
        y += 4; g.DrawLine(pen, 16, y, 560, y); y += 12;
        if(receipt.DeliveryCents>0)y=DrawWrap(g,$"Entrega: {Money.Format(receipt.DeliveryCents)}",body,16,y,544);
        g.DrawString($"VALOR TOTAL: {Money.Format(receipt.TotalCents)}", title, Ink, new RectangleF(12, y, 552, 35), center); y += 45;
        y = DrawWrap(g, $"Pix / Dinheiro: {Money.Format(receipt.BaseTotalCents)}", body, 16, y, 544);
        y = DrawWrap(g, $"Cartão: {Money.Format(receipt.CardTotalCents)} (acréscimo da taxa da maquininha)", body, 16, y, 544);
        y = DrawWrap(g, receipt.Payment switch { PaymentKind.Pix => "Forma usada: Pix aguardando confirmação", PaymentKind.Cash => "Forma usada: Dinheiro", _ => "Forma usada: Cartão" }, body, 16, y, 544);
        if (receipt.Payment == PaymentKind.Pix && !string.IsNullOrEmpty(pixPayload))
        {
            y = DrawWrap(g, $"Pix para pagar: {Money.Format(receipt.TotalCents)}", bold, 16, y + 8, 544);
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(pixPayload, QRCodeGenerator.ECCLevel.M);
            using var qr = new QRCode(data);
            var pixelsPerModule = Math.Clamp(340 / data.ModuleMatrix.Count, 4, 7);
            using var qrImage = qr.GetGraphic(pixelsPerModule, Color.Black, Color.White, true);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.DrawImage(qrImage,
                new Rectangle((Width - qrImage.Width) / 2, (int)y, qrImage.Width, qrImage.Height),
                new Rectangle(0, 0, qrImage.Width, qrImage.Height), GraphicsUnit.Pixel);
            y += qrImage.Height + 8;
            if (!string.IsNullOrWhiteSpace(receipt.PixTxId))
                y = DrawWrap(g, $"Pedido Pix: {receipt.PixTxId}", small, 16, y, 544);
            y = DrawWrap(g, "Confira o pagamento no app do banco.", small, 16, y, 544);
        }
        if (receipt.Payment == PaymentKind.Cash)
        {
            y = DrawWrap(g, $"Recebido: {Money.Format(receipt.ReceivedCents!.Value)}", body, 16, y, 544);
            y = DrawWrap(g, $"Troco: {Money.Format(receipt.ChangeCents!.Value)}", bold, 16, y, 544);
        }
        y += 16;
        y = DrawWrap(g, "Documento para controle interno. Não substitui a nota fiscal.", small, 36, y, 504);
        y = DrawWrap(g, "Sem valor fiscal.", small, 36, y, 504) + 22;
        if (y > canvas.Height) throw new InvalidOperationException("Cupom excede a altura permitida.");
        var result = canvas.Clone(new Rectangle(0, 0, Width, (int)Math.Ceiling(y)), PixelFormat.Format24bppRgb);
        result.SetResolution(203, 203);
        return result;
    }

    static float DrawWrap(Graphics g, string text, Font font, float x, float y, float width)
    {
        var h = g.MeasureString(text, font, (int)width).Height + 3;
        g.DrawString(text, font, Ink, new RectangleF(x, y, width, h));
        return y + h + 2;
    }

    static Bitmap BuildThermalLogo(string path)
    {
        using var source = new Bitmap(path);
        using var scaled = new Bitmap(source, 140, 140);
        var result = new Bitmap(140, 140);
        for (var y = 0; y < 140; y++) for (var x = 0; x < 140; x++)
        {
            var color = scaled.GetPixel(x, y);
            var luma = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
            result.SetPixel(x, y, color.A > 80 && luma < 190 ? Color.Black : Color.White);
        }
        return result;
    }

    public static void Print(Receipt receipt, string logoPath, string printerName = "ELGIN i7(USB)", string? pixPayload = null)
    {
        using var image = Render(receipt, logoPath, pixPayload);
        using var doc = new PrintDocument();
        doc.PrinterSettings.PrinterName = printerName;
        if (!doc.PrinterSettings.IsValid) throw new InvalidOperationException($"Impressora {printerName} não encontrada.");
        doc.DocumentName = "Barba Defumados - Cupom não fiscal";
        doc.OriginAtMargins = false;
        doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        var height = (int)Math.Ceiling(image.Height * 283.0 / Width) + 8;
        doc.DefaultPageSettings.PaperSize = new PaperSize("Bobina 80 mm - área 72 mm", 283, height);
        doc.PrintPage += (_, e) =>
        {
            if (e.Graphics is null) return;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(image, 0, 0, 283, height - 8);
            e.HasMorePages = false;
        };
        doc.Print();
    }
}
