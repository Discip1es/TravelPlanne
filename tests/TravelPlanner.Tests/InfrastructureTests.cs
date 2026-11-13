using Microsoft.Extensions.Caching.Memory;
using TravelPlanner.Core.Domain;
using TravelPlanner.Infrastructure.Security;
using TravelPlanner.Infrastructure.Services;
using Xunit;

namespace TravelPlanner.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_AcceptsCorrectPassword()
    {
        var hash = _hasher.Hash("Demo#12345");
        Assert.True(_hasher.Verify("Demo#12345", hash));
    }

    [Fact]
    public void Verify_RejectsWrongPassword() => Assert.False(_hasher.Verify("wrong", _hasher.Hash("Demo#12345")));

    [Fact]
    public void Hash_UsesRandomSalt() => Assert.NotEqual(_hasher.Hash("Demo#12345"), _hasher.Hash("Demo#12345"));

    [Fact]
    public void Verify_HandlesMalformedHash() => Assert.False(_hasher.Verify("Demo#12345", "not-a-hash"));
}

public class CurrencyServiceTests
{
    private readonly CurrencyService _service = new(new MemoryCache(new MemoryCacheOptions()));
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Convert_SameCurrency_ReturnsSameAmount()
        => Assert.Equal(1000m, await _service.ConvertAsync(1000m, "RUB", "RUB", Ct));

    [Fact]
    public async Task Convert_RubToUsd_UsesRate()
        => Assert.Equal(100m, await _service.ConvertAsync(9250m, "RUB", "USD", Ct));

    /// <summary>Регрессия: обратное направление не должно округляться до 9249,84.</summary>
    [Fact]
    public async Task Convert_UsdToRub_IsExact()
        => Assert.Equal(9250m, await _service.ConvertAsync(100m, "USD", "RUB", Ct));

    /// <summary>Регрессия: кросс-курс без промежуточного округления (25000 * 7,25 / 92,5).</summary>
    [Fact]
    public async Task Convert_CrossRate_RubToCny_IsExact()
        => Assert.Equal(1959.46m, await _service.ConvertAsync(25000m, "RUB", "CNY", Ct));

    /// <summary>Инвариант: замкнутый пересчёт возвращает исходную сумму.</summary>
    [Fact]
    public async Task Convert_RoundTrip_PreservesAmount()
    {
        var usd = await _service.ConvertAsync(9250m, "RUB", "USD", Ct);
        var back = await _service.ConvertAsync(usd, "USD", "RUB", Ct);
        Assert.Equal(9250m, back);
    }

    [Fact]
    public async Task Convert_IsCaseInsensitive()
        => Assert.Equal(100m, await _service.ConvertAsync(9250m, "rub", "usd", Ct));

    [Fact]
    public async Task Convert_Zero_ReturnsZero()
        => Assert.Equal(0m, await _service.ConvertAsync(0m, "RUB", "CNY", Ct));

    [Fact]
    public async Task Convert_UnknownCurrency_Throws()
        => await Assert.ThrowsAsync<ValidationException>(() => _service.ConvertAsync(1m, "XYZ", "RUB", Ct));

    [Fact]
    public async Task Rates_AreRelativeToBaseCurrency()
    {
        var rates = await _service.GetRatesAsync("RUB", Ct);
        Assert.Equal(1m, rates["RUB"]);
        Assert.True(rates["USD"] > 0);
    }

    /// <summary>Согласованность справочника и конвертера: 1 USD→RUB совпадает с rates(USD)["RUB"].</summary>
    [Fact]
    public async Task Rates_AndConverter_AreConsistent()
    {
        var rates = await _service.GetRatesAsync("USD", Ct);
        var converted = await _service.ConvertAsync(1m, "USD", "RUB", Ct);
        Assert.Equal(rates["RUB"], converted);
    }

    [Fact]
    public async Task Rates_UnknownBaseCurrency_Throws()
        => await Assert.ThrowsAsync<ValidationException>(() => _service.GetRatesAsync("XYZ", Ct));
}