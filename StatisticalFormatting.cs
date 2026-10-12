using System.Globalization;

namespace MediHiStat
{
    internal static class StatisticalFormatting
    {
        private static readonly double[] SignificanceThresholds = { 0.001, 0.01, 0.05 };

        /// <summary>Format only for presentation; corrections always use the original numeric p.</summary>
        internal static string PValue(double value, CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            if (!double.IsFinite(value) || value < 0 || value > 1)
            {
                return "—";
            }
            if (value < 0.0001)
            {
                return "<" + 0.0001.ToString("0.0000", culture);
            }

            // Four decimal places are normally enough. Add precision when the
            // displayed value would hide which side of a common threshold p lies on.
            for (int digits = 4; digits <= 15; digits++)
            {
                string formatted = value.ToString("0." + new string('#', digits), culture);
                double displayed = double.Parse(formatted, NumberStyles.Float, culture);
                if (SignificanceThresholds.All(threshold =>
                    Math.Sign(value - threshold) == Math.Sign(displayed - threshold)))
                {
                    return formatted;
                }
            }
            return value.ToString("R", culture);
        }
    }
}
