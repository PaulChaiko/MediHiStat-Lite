using System;
using System.Linq;
using MediHiStat;

internal static class NormalityVerification
{
    // Independent statistics/p references: scipy.stats.shapiro in SciPy 1.17.0;
    // Lilliefors D: scipy.stats.kstest((x-mean)/std(ddof=1), "norm").
    // Interior Lilliefors p references: statsmodels' 10m-simulation table and
    // TableDist linear interpolation (the method explicitly reported to users).
    public static void Run()
    {
        CheckShapiro(new[] { 1.0, 2.0, 3.0 }, 1, 1);
        CheckShapiro(new[] { 1.0, 2.0, 5.0 }, 0.923076923076923, 0.46326287493379903);
        CheckShapiro(new[] { 1.0, 2.0, 3.0, 4.0 }, 0.9929120069984326, 0.9718770585603881);
        CheckShapiro(Enumerable.Range(1, 10).Select(i => (double)i).ToArray(), 0.9701646110856056, 0.8923673061902978);
        CheckShapiro(new double[] { 1, 1, 1, 2, 2, 2, 3, 3, 4, 10 }, 0.6920576412550115, 0.0007059154112118447);
        CheckShapiro(OutlierSample(5), 0.8669607110584259, 0.2543338401775038);
        CheckShapiro(OutlierSample(6), 0.8615273085309286, 0.19446778412710247);
        CheckShapiro(OutlierSample(11), 0.8663889661580897, 0.06965069894153011);
        CheckShapiro(OutlierSample(12), 0.8690918384660068, 0.06364018995347889);
        CheckShapiro(OutlierSample(50), 0.9285499372636429, 0.004872014108127756);
        CheckShapiro(OutlierSample(55), 0.9316743680619521, 0.0038485894943366054);
        CheckShapiro(OutlierSample(200), 0.9544063914441107, 5.139905788869279e-06);

        CheckLilliefors(new[] { 1.0, 2.0, 3.0, 4.0 }, 0.15073232084833066, 0.979946895720458);
        CheckLilliefors(OutlierSample(5), 0.2743990670473342, 0.26846607687195223);
        CheckLilliefors(OutlierSample(6), 0.25372013286343853, 0.2802636118000894);
        CheckLilliefors(OutlierSample(11), 0.17748631660746705, 0.4347667521755314);
        CheckLilliefors(OutlierSample(12), 0.167333137506521, 0.4640721862611788);
        CheckLilliefors(OutlierSample(50), 0.06918141455341731, 0.7896422708808979);
        CheckLilliefors(OutlierSample(55), 0.06637684171281533, 0.8038972960342421);
        CheckLilliefors(OutlierSample(200), 0.05368767885620479, 0.18646837651506373);
        CheckLilliefors(new double[] { 1, 1, 1, 2, 2, 2, 3, 3, 4, 10 }, 0.2851471906948351, 0.022182983955899353);

        NormalityResult highP = NormalityCalculator.Lilliefors(Enumerable.Range(1, 10).Select(i => (double)i));
        Require(highP.PValueBound == NormalityPValueBound.GreaterThanOrEqual && highP.BoundValue == .990 && highP.PValue == 1,
            "Lilliefors upper-bound label and conservative correction value");
        NormalityResult lowP = NormalityCalculator.Lilliefors(OutlierSample(1601));
        Near(lowP.Statistic, 0.05670262975984408, 3e-10, "Lilliefors asymptotic D");
        Require(lowP.PValueBound == NormalityPValueBound.LessThanOrEqual && lowP.BoundValue == .001 && lowP.PValue == .001,
            "Lilliefors lower-bound label and conservative correction value");
        Require(lowP.Notes.Contains("асимптотическая"), "Lilliefors asymptotic calibration note");
        Require(NormalityCalculator.ShapiroWilk(Enumerable.Range(1, 5001).Select(i => (double)i)).Notes.Contains("5000"), "Shapiro n > 5000 warning");

        double[] ordinary = OutlierSample(12);
        foreach (Func<System.Collections.Generic.IEnumerable<double>, NormalityResult> test in
            new Func<System.Collections.Generic.IEnumerable<double>, NormalityResult>[] { NormalityCalculator.ShapiroWilk, NormalityCalculator.Lilliefors })
        {
            NormalityResult original = test(ordinary);
            NormalityResult shifted = test(ordinary.Select(x => 1e12 + x * 5));
            Near(shifted.Statistic, original.Statistic, 1e-10, "large-offset affine invariance");
            NormalityResult tiny = test(ordinary.Select(x => x * 1e-200));
            Near(tiny.Statistic, original.Statistic, 1e-10, "tiny-scale affine invariance");
            NormalityResult extreme = test(new[] { -double.MaxValue, -1e308, 0, 1e308, double.MaxValue });
            Require(double.IsFinite(extreme.Statistic) && double.IsFinite(extreme.PValue), "overflow-resistant finite data");
            Throws(() => test(new double[] { 5, 5, 5, 5 }), "constant sample");
            Throws(() => test(new double[] { 1, 2, 3, double.NaN }), "NaN sample");
            Throws(() => test(new double[] { 1, 2, 3, double.PositiveInfinity }), "infinite sample");
        }
        Throws(() => NormalityCalculator.ShapiroWilk(new double[] { 1, 2 }), "Shapiro minimum n");
        Throws(() => NormalityCalculator.Lilliefors(new double[] { 1, 2, 3 }), "Lilliefors minimum n");
        Console.WriteLine("Normality verification passed: SciPy references, calibrated Lilliefors bounds, numeric edge cases.");
    }

    private static double[] OutlierSample(int n)
    {
        double[] x = Enumerable.Range(1, n).Select(i => (double)i).ToArray();
        x[^1] = n * 1.8;
        return x;
    }

    private static void CheckShapiro(double[] x, double w, double p)
    {
        NormalityResult result = NormalityCalculator.ShapiroWilk(x);
        Require(result.Count == x.Length, "Shapiro count");
        Near(result.Statistic, w, 2e-12, $"Shapiro W n={x.Length}");
        Near(result.PValue, p, 2e-9, $"Shapiro p n={x.Length}");
    }

    private static void CheckLilliefors(double[] x, double d, double p)
    {
        NormalityResult result = NormalityCalculator.Lilliefors(x);
        Require(result.Count == x.Length, "Lilliefors count");
        Near(result.Statistic, d, 3e-10, $"Lilliefors D n={x.Length}");
        Near(result.PValue, p, 2e-8, $"Lilliefors calibrated p n={x.Length}");
        Require(result.PValueBound == NormalityPValueBound.None, "interior Lilliefors result");
    }

    private static void Near(double actual, double expected, double tolerance, string description)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{description}: expected {expected:R}, actual {actual:R}.");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }

    private static void Throws(Action action, string description)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException($"Expected ArgumentException: {description}");
    }
}
