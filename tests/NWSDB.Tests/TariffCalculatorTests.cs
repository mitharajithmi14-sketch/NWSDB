using NWSDB.BillingService.Services;
using Xunit;

namespace NWSDB.Tests;

public class TariffCalculatorTests
{
    private readonly TariffCalculator _tariff = new();

    [Theory]
    [InlineData(0, 300)]      // service charge only
    [InlineData(9, 840)]      // 300 + 9 x 60
    [InlineData(10, 900)]     // end of the first block
    [InlineData(15, 1300)]    // 300 + 600 + 5 x 80
    [InlineData(20, 1700)]    // end of the second block
    [InlineData(24, 2140)]    // 300 + 600 + 800 + 4 x 110
    public void Calculate_ReturnsExpectedAmount(int units, int expected)
    {
        Assert.Equal((decimal)expected, _tariff.Calculate(units));
    }

    [Fact]
    public void Calculate_NegativeUnits_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _tariff.Calculate(-1));
    }
}
