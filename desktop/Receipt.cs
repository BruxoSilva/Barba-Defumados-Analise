using System.Globalization;
using System.Text.RegularExpressions;

namespace BarbaCupom;

public enum PaymentKind { Pix, Cash, Card }
public enum SaleUnit { Un, Kg }
public sealed record OrderLine(string Name, decimal Quantity, int UnitPriceCents, SaleUnit Unit = SaleUnit.Un)
{
    public int TotalCents => checked((int)decimal.Round(Quantity * UnitPriceCents, 0, MidpointRounding.AwayFromZero));
    public int QuantityMilli => checked((int)(Quantity * 1000));
    public string QuantityLabel => Unit == SaleUnit.Kg
        ? $"{Quantity.ToString("0.000", CultureInfo.GetCultureInfo("pt-BR"))} KG"
        : $"{Quantity:0} UN";
}

public static class QuantityInput
{
    public static decimal Parse(string value, SaleUnit unit)
    {
        value = value.Trim();
        var pattern = unit == SaleUnit.Kg ? @"^\d+(?:[,.]\d{1,3})?$" : @"^\d+$";
        if (!Regex.IsMatch(value, pattern)) throw new FormatException(unit == SaleUnit.Kg
            ? "Informe o peso em kg, por exemplo 0,350." : "Informe uma quantidade inteira.");
        return decimal.Parse(value.Replace('.', ','), NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"));
    }
}

public static class Money
{
    static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    public static int Parse(string value)
    {
        value = value.Trim().Replace("R$", "").Trim();
        if (!Regex.IsMatch(value, @"^(?:\d{1,3}(?:\.\d{3})+|\d+)(?:,\d{1,2})?$"))
            throw new FormatException("Informe um valor em reais, por exemplo 25,90.");
        var amount = decimal.Parse(value, NumberStyles.Number, Br);
        if (amount > 1_000_000m) throw new FormatException("Valor acima do limite permitido.");
        return checked((int)(amount * 100));
    }
    public static string Format(int cents) => (cents / 100m).ToString("C", Br).Replace('\u00a0', ' ');
}

public sealed class Receipt
{
    public string CustomerName { get; }
    public string Phone { get; }
    public string Address { get; }
    public IReadOnlyList<OrderLine> Lines { get; }
    public PaymentKind Payment { get; }
    public int? ReceivedCents { get; }
    public int? ChangeCents => ReceivedCents - TotalCents;
    public int TotalCents { get; }
    public int BaseTotalCents { get; }
    public int DeliveryCents { get; }
    public int CardTotalCents { get; }
    public decimal CardFeePercent { get; }
    public DateTime IssuedAt { get; }
    public string? PixTxId { get; }

    public Receipt(string name, string phone, string address, IReadOnlyList<OrderLine> lines,
        PaymentKind payment, int? receivedCents, DateTime issuedAt, string? pixTxId = null, decimal cardFeePercent = 5, int deliveryCents = 0)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new ArgumentException("Informe o nome do cliente.");
        if (!string.IsNullOrWhiteSpace(phone) && new string(phone.Where(char.IsDigit).ToArray()).Length < 8) throw new ArgumentException("Informe um telefone válido ou deixe em branco.");
        if (address.Length > 120) throw new ArgumentException("Endereço muito longo.");
        if (lines.Count is < 1 or > 12 || lines.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 60 ||
            x.UnitPriceCents < 0 || x.Quantity > 999 ||
            (x.Unit == SaleUnit.Un && (x.Quantity < 1 || x.Quantity != decimal.Truncate(x.Quantity))) ||
            (x.Unit == SaleUnit.Kg && (x.Quantity < 0.001m || x.Quantity * 1000 != decimal.Truncate(x.Quantity * 1000)))))
            throw new ArgumentException("Confira os produtos, preços e quantidades.");
        if(deliveryCents<0)throw new ArgumentException("Entrega inválida.");
        DeliveryCents=deliveryCents;
        var total = checked(lines.Sum(x => x.TotalCents)+deliveryCents);
        if (total <= 0 || total > 100_000_000) throw new ArgumentException("O total precisa ser maior que zero.");
        if (cardFeePercent < 0 || cardFeePercent >= 100) throw new ArgumentException("Taxa de cartão inválida.");
        if (payment == PaymentKind.Cash && (!receivedCents.HasValue || receivedCents < total))
            throw new ArgumentException("Valor recebido insuficiente.");
        CustomerName = name.Trim(); Phone = phone.Trim(); Address = address.Trim();
        Lines = lines.ToArray(); Payment = payment; ReceivedCents = payment == PaymentKind.Cash ? receivedCents : null;
        BaseTotalCents = total; CardFeePercent = cardFeePercent;
        CardTotalCents = checked((int)decimal.Ceiling(total / (1 - cardFeePercent / 100)));
        TotalCents = payment == PaymentKind.Card ? CardTotalCents : total; IssuedAt = issuedAt;
        PixTxId = payment == PaymentKind.Pix ? pixTxId : null;
    }
}
