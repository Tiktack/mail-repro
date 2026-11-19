namespace MailPageRepro.Extensions;

public class Utils
{
    public static string ToPerc(double perc)
    {
        return $"{Math.Round(perc, 1).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%";
    }
}
