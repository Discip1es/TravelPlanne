using Microsoft.Extensions.Caching.Memory;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Infrastructure.Services;

/// <summary>
/// Курсы валют относительно USD. Это детерминированная «заглушка» (учебный стенд работает офлайн).
/// В промышленном варианте замените словарь на HttpClient к API ЦБ РФ/ECB, сохранив кэширование.
///
/// ВАЖНО (семантика):
///   GetRatesAsync(base)["X"] = сколько единиц X содержится в одной единице base.
///   ConvertAsync считает amount → USD → to и округляет ТОЛЬКО конечный результат:
///   промежуточное округление курсов даёт дрейф (например, 100 USD → 9249,84 RUB вместо 9250,00).
/// </summary>
public sealed class CurrencyService(IMemoryCache cache) : ICurrencyService
{
    private static readonly Dictionary<string, decimal> UsdRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1m,
        ["EUR"] = 0.92m,
        ["RUB"] = 92.50m,
        ["CNY"] = 7.25m,
        ["TRY"] = 34.20m,
        ["THB"] = 36.10m,
        ["AED"] = 3.67m,
        ["GEL"] = 2.70m,
        ["KZT"] = 470.00m
    };

    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public IReadOnlyCollection<string> SupportedCurrencies => UsdRates.Keys.OrderBy(k => k).ToList();

    /// <summary>Таблица кросс-курсов для отображения. Округление до 6 знаков здесь уместно.</summary>
    public Task<IReadOnlyDictionary<string, decimal>> GetRatesAsync(string baseCurrency, CancellationToken ct)
    {
        EnsureSupported(baseCurrency); // проверка выполняется до обращения к кэшу

        var key = $"rates:{baseCurrency.ToUpperInvariant()}";
        if (cache.TryGetValue(key, out IReadOnlyDictionary<string, decimal>? cached) && cached is not null)
            return Task.FromResult(cached);

        var divisor = UsdRates[baseCurrency];
        var rates = UsdRates.ToDictionary(
            kv => kv.Key.ToUpperInvariant(),
            kv => decimal.Round(kv.Value / divisor, 6));

        cache.Set(key, (IReadOnlyDictionary<string, decimal>)rates, Ttl);
        return Task.FromResult((IReadOnlyDictionary<string, decimal>)rates);
    }

    /// <summary>
    /// Конвертация суммы: amount / UsdRates[from] * UsdRates[to].
    /// Пример: 9250 RUB → 9250 / 92,5 = 100 USD → 100 * 1 = 100,00 USD.
    /// </summary>
    public Task<decimal> ConvertAsync(decimal amount, string from, string to, CancellationToken ct)
    {
        EnsureSupported(from);
        EnsureSupported(to);

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(decimal.Round(amount, 2));

        var amountInUsd = amount / UsdRates[from];          // полное десятичное представление, без округления
        var result = decimal.Round(amountInUsd * UsdRates[to], 2);
        return Task.FromResult(result);
    }

    private static void EnsureSupported(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || !UsdRates.ContainsKey(currency))
            throw new ValidationException("currency", $"Валюта «{currency}» не поддерживается.");
    }
}