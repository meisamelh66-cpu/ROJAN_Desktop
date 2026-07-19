namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Formats a monetary amount for one of the currencies this platform ships with - <see cref="Currency.Toman"/>/<see cref="Currency.Rial"/> alongside <see cref="Currency.Usd"/>/<see cref="Currency.Eur"/>, future-extensible the same way languages are (a real per-currency plug-in point is future work; the four here prove the seam).</summary>
public interface ICurrencyFormatter
{
    public string Format(decimal amount, Currency currency, NumberDigits digits = NumberDigits.Latin);
}
