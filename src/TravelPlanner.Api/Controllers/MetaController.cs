using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Api.Controllers;

/// <summary>
/// Справочники и конвертер валют. Доступны без токена — нужны на формах входа/регистрации.
/// ВНИМАНИЕ: не называйте действия именами типов BCL (Convert, Task, Path, Environment и т. п.) —
/// это затеняет имена внутри класса и приводит к CS0119/CS8422.
/// </summary>
[ApiController]
[Route("api/meta")]
public sealed class MetaController(ICurrencyService currencies) : ControllerBase
{
    /// <summary>Перечисления домена и список поддерживаемых валют (кэш 10 минут).</summary>
    [HttpGet]
    [OutputCache(PolicyName = "meta")]
    public ActionResult<MetaDto> Get() => Ok(new MetaDto(
        Items<TripStatus>(),
        Items<ActivityCategory>(),
        Items<ActivityStatus>(),
        currencies.SupportedCurrencies.ToList()));

    /// <summary>Курсы относительно указанной базовой валюты.</summary>
    [HttpGet("rates/{baseCurrency}")]
    public async Task<ActionResult<IReadOnlyDictionary<string, decimal>>> Rates(
        string baseCurrency, CancellationToken ct)
        => Ok(await currencies.GetRatesAsync(baseCurrency, ct));

    /// <summary>Конвертация суммы. Маршрут прежний: api/meta/convert.</summary>
    [HttpGet("convert")]
    public async Task<ActionResult<ConversionDto>> ConvertCurrency(
        [FromQuery] decimal amount,
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            throw new ValidationException("from/to",
                "Укажите параметры from и to, например: ?amount=1000&from=RUB&to=CNY.");

        var rates = await currencies.GetRatesAsync(from, ct);   // база — исходная валюта
        if (!rates.TryGetValue(to.ToUpperInvariant(), out var rate))
            throw new ValidationException(nameof(to), $"Валюта «{to}» не поддерживается.");

        var converted = await currencies.ConvertAsync(amount, from, to, ct);
        return Ok(new ConversionDto(amount, from.ToUpperInvariant(), to.ToUpperInvariant(), rate, converted));
    }

    /// <summary>
    /// Универсальная выгрузка значений перечисления.
    /// System.Convert указан полностью — защита от затенения имени даже при будущем переименовании действий.
    /// </summary>
    private static IReadOnlyList<EnumItemDto> Items<T>() where T : struct, Enum =>
        Enum.GetValues<T>()
            .Select(value => new EnumItemDto(value.ToString(), System.Convert.ToInt32(value)))
            .ToList();
}