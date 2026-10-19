namespace NWSDB.UsageService.Models;

public class MeterReading
{
    public int ReadingId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    /// <summary>Reading month in the format YYYY-MM.</summary>
    public string Month { get; set; } = string.Empty;
    /// <summary>Meter value shown on the water meter (m³).</summary>
    public int MeterValue { get; set; }
    /// <summary>Units used in the month (m³).</summary>
    public int UnitsUsed { get; set; }
}
