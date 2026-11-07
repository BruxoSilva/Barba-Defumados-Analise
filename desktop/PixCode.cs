using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BarbaCupom;

public sealed record PixSettings(string Key, string ReceiverName, string City);

public static class PixCode
{
    public static string NewTxId() => Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();

    public static void Validate(PixSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Key) || settings.Key.Length > 77 ||
            settings.Key.Any(c => c < '!' || c > '~'))
            throw new ArgumentException("Informe uma chave Pix válida, sem espaços, com até 77 caracteres.");
        var name = AsciiText(settings.ReceiverName);
        var city = AsciiText(settings.City);
        if (name.Length is < 1 or > 25) throw new ArgumentException("O nome do recebedor deve ter até 25 caracteres.");
        if (city.Length is < 1 or > 15) throw new ArgumentException("A cidade deve ter até 15 caracteres.");
    }

    public static string Build(PixSettings settings, int? amountCents, string txId)
    {
        Validate(settings);
        if (amountCents is <= 0 or > 100_000_000) throw new ArgumentException("Valor Pix inválido.");
        if (txId != "***" && !Regex.IsMatch(txId, "^[A-Za-z0-9]{1,25}$"))
            throw new ArgumentException("Identificador Pix inválido.");
        var account = Tlv("00", "br.gov.bcb.pix") + Tlv("01", settings.Key);
        var payload = Tlv("00", "01") + Tlv("26", account) + Tlv("52", "0000") + Tlv("53", "986");
        if (amountCents.HasValue) payload += Tlv("54", (amountCents.Value / 100m).ToString("0.00", CultureInfo.InvariantCulture));
        payload += Tlv("58", "BR") + Tlv("59", AsciiText(settings.ReceiverName)) +
            Tlv("60", AsciiText(settings.City).ToUpperInvariant()) + Tlv("62", Tlv("05", txId)) + "6304";
        return payload + Crc16(payload).ToString("X4", CultureInfo.InvariantCulture);
    }

    static string Tlv(string id, string value)
    {
        if (value.Length > 99) throw new ArgumentException("Campo Pix acima do limite permitido.");
        return id + value.Length.ToString("00", CultureInfo.InvariantCulture) + value;
    }

    static string AsciiText(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or ' ' or '.' or '-' or '&') builder.Append(c);
            else if (c is >= ' ' and <= '~') builder.Append(' ');
        }
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    static ushort Crc16(string value)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(value))
        {
            crc ^= (ushort)(b << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }
}
