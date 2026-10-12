namespace MediHiStat
{
    internal static class StatisticalMath
    {
        /// <summary>Two-sided standard normal tail without subtracting a CDF close to one.</summary>
        internal static double NormalTwoSidedTail(double z)
        {
            if (double.IsNaN(z))
            {
                return double.NaN;
            }

            // erfc(|z|/sqrt(2)) = Q(1/2,z²/2). Evaluate the regularized upper
            // incomplete gamma directly in the tail to avoid subtractive cancellation.
            double x = z * z / 2;
            if (double.IsPositiveInfinity(x))
            {
                return 0;
            }
            if (x == 0)
            {
                return 1;
            }
            double factor = Math.Exp(0.5 * Math.Log(x) - x - 0.5 * Math.Log(Math.PI));
            if (x < 1.5)
            {
                double term = 2;
                double sum = term;
                double shape = 0.5;
                for (int iteration = 0; iteration < 10_000; iteration++)
                {
                    shape++;
                    term *= x / shape;
                    sum += term;
                    if (Math.Abs(term) <= Math.Abs(sum) * 1e-15)
                    {
                        break;
                    }
                }
                return Math.Clamp(1 - factor * sum, 0, 1);
            }

            const double tiny = 1e-300;
            double b = x + 0.5;
            double c = 1 / tiny;
            double d = 1 / b;
            double fraction = d;
            for (int iteration = 1; iteration <= 10_000; iteration++)
            {
                double numerator = -iteration * (iteration - 0.5);
                b += 2;
                d = numerator * d + b;
                if (Math.Abs(d) < tiny)
                {
                    d = tiny;
                }
                c = b + numerator / c;
                if (Math.Abs(c) < tiny)
                {
                    c = tiny;
                }
                d = 1 / d;
                double multiplier = d * c;
                fraction *= multiplier;
                if (Math.Abs(multiplier - 1) <= 1e-15)
                {
                    break;
                }
            }
            return Math.Clamp(factor * fraction, 0, 1);
        }
    }
}
