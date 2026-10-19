namespace NWSDB.BillingService.Services;

public interface ITariffCalculator
{
    decimal Calculate(int unitsUsed);
}

/// <summary>
/// Simplified sample domestic tariff used for this student project (not the official NWSDB tariff):
/// fixed service charge LKR 300, units 1-10 at LKR 60, units 11-20 at LKR 80, units above 20 at LKR 110.
/// </summary>
public class TariffCalculator : ITariffCalculator
{
    public const decimal ServiceCharge = 300m;

    public decimal Calculate(int unitsUsed)
    {
        if (unitsUsed < 0) throw new ArgumentOutOfRangeException(nameof(unitsUsed), "Units used cannot be negative.");

        var firstBlock = Math.Min(unitsUsed, 10);
        var secondBlock = Math.Clamp(unitsUsed - 10, 0, 10);
        var thirdBlock = Math.Max(unitsUsed - 20, 0);

        return ServiceCharge + firstBlock * 60m + secondBlock * 80m + thirdBlock * 110m;
    }
}
