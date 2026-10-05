/*
Shapiro-Wilk coefficients and numerical helpers adapted from SciPy 1.17.0
scipy/stats/_ansari_swilk_statistics.pyx (uncensored AS R94 implementation).
https://github.com/scipy/scipy/blob/v1.17.0/scipy/stats/_ansari_swilk_statistics.pyx
Royston (1995), Applied Statistics 44, DOI: 10.2307/2986146.

Copyright (c) 2001-2002 Enthought, Inc. 2003, SciPy Developers.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above
   copyright notice, this list of conditions and the following
   disclaimer in the documentation and/or other materials provided
   with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived
   from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Lilliefors normal critical values and asymptotic coefficients adapted from
statsmodels/stats/_lilliefors_critical_values.py, based on 10,000,000 simulations.
https://github.com/statsmodels/statsmodels/blob/main/statsmodels/stats/_lilliefors_critical_values.py
https://www.statsmodels.org/stable/_modules/statsmodels/stats/_lilliefors.html

Copyright (C) 2006, Jonathan E. Taylor
All rights reserved.

Copyright (c) 2006-2008 Scipy Developers.
All rights reserved.

Copyright (c) 2009-2018 statsmodels Developers.
All rights reserved.


Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  a. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.
  b. Redistributions in binary form must reproduce the above copyright
     notice, this list of conditions and the following disclaimer in the
     documentation and/or other materials provided with the distribution.
  c. Neither the name of statsmodels nor the names of its contributors
     may be used to endorse or promote products derived from this software
     without specific prior written permission.


THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL STATSMODELS OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace MediHiStat
{
    internal enum NormalityPValueBound { None, LessThanOrEqual, GreaterThanOrEqual }

    internal sealed record NormalityResult(
        int Count, double Statistic, double PValue, string Method, string Notes)
    {
        public NormalityPValueBound PValueBound { get; init; }
        public double BoundValue { get; init; } = double.NaN;
    }

    internal static class NormalityCalculator
    {
        private static readonly double[] C1 = { 0, 0.221157, -0.147981, -2.07119, 4.434685, -2.706056 };
        private static readonly double[] C2 = { 0, 0.042981, -0.293762, -1.752461, 5.682633, -3.582633 };
        private static readonly double[] C3 = { 0.5440, -0.39978, 0.025054, -0.0006714 };
        private static readonly double[] C4 = { 1.3822, -0.77857, 0.062767, -0.0020322 };
        private static readonly double[] C5 = { -1.5861, -0.31082, -0.083751, 0.0038915 };
        private static readonly double[] C6 = { -0.4803, -0.082676, 0.0030302 };

        public static NormalityResult ShapiroWilk(IEnumerable<double> values)
        {
            double[] x = PrepareSample(values, 3, "Шапиро–Уилка");
            int n = x.Length;
            int half = n / 2;
            double[] a = new double[half];
            if (n == 3)
            {
                a[0] = Math.Sqrt(0.5);
            }
            else
            {
                double sumSquares = 0;
                for (int i = 0; i < half; i++)
                {
                    a[i] = NormalQuantile((i + 1 - 0.375) / (n + 0.25));
                    sumSquares += a[i] * a[i];
                }
                sumSquares *= 2;
                double sumRoot = Math.Sqrt(sumSquares);
                double inverseRootN = 1 / Math.Sqrt(n);
                double a1 = Polynomial(C1, inverseRootN) - a[0] / sumRoot;
                int start;
                double factor;
                if (n > 5)
                {
                    start = 2;
                    double a2 = Polynomial(C2, inverseRootN) - a[1] / sumRoot;
                    factor = Math.Sqrt((sumSquares - 2 * a[0] * a[0] - 2 * a[1] * a[1])
                        / (1 - 2 * a1 * a1 - 2 * a2 * a2));
                    a[1] = a2;
                }
                else
                {
                    start = 1;
                    factor = Math.Sqrt((sumSquares - 2 * a[0] * a[0]) / (1 - 2 * a1 * a1));
                }
                a[0] = a1;
                for (int i = start; i < half; i++) a[i] *= -1 / factor;
            }

            // Centered sums reproduce SciPy's stable calculation of 1 - W.
            double meanX = x.Average();
            double meanA = 0;
            for (int i = 0; i < n; i++)
            {
                int opposite = n - 1 - i;
                if (i != opposite) meanA += (i < opposite ? -1 : 1) * a[Math.Min(i, opposite)];
            }
            meanA /= n;
            double ssa = 0, ssx = 0, sax = 0;
            for (int i = 0; i < n; i++)
            {
                int opposite = n - 1 - i;
                double ai = i == opposite ? -meanA
                    : (i < opposite ? -1 : 1) * a[Math.Min(i, opposite)] - meanA;
                double xi = x[i] - meanX;
                ssa += ai * ai;
                ssx += xi * xi;
                sax += ai * xi;
            }
            double productRoot = Math.Sqrt(ssa * ssx);
            double w1 = Math.Clamp((productRoot - sax) * (productRoot + sax) / (ssa * ssx), 0, 1);
            double w = 1 - w1;
            double p;
            if (n == 3)
            {
                w = Math.Max(0.75, w);
                p = 1 - (6 / Math.PI) * Math.Acos(Math.Sqrt(w));
            }
            else if (w1 == 0)
            {
                p = 1;
            }
            else
            {
                double y = Math.Log(w1);
                double m, s;
                if (n <= 11)
                {
                    double gamma = -2.273 + 0.459 * n;
                    if (y >= gamma)
                        return new NormalityResult(n, w, 1e-19, "Шапиро–Уилка", ShapiroNotes(x));
                    y = -Math.Log(gamma - y);
                    m = Polynomial(C3, n);
                    s = Math.Exp(Polynomial(C4, n));
                }
                else
                {
                    double logN = Math.Log(n);
                    m = Polynomial(C5, logN);
                    s = Math.Exp(Polynomial(C6, logN));
                }
                p = NormalSurvival((y - m) / s);
            }
            return new NormalityResult(n, w, Math.Clamp(p, 0, 1), "Шапиро–Уилка", ShapiroNotes(x));
        }

        public static NormalityResult Lilliefors(IEnumerable<double> values)
        {
            double[] x = PrepareSample(values, 4, "Колмогорова–Смирнова — Лиллиефорса");
            int n = x.Length;
            double mean = x.Average();
            double sumSquares = x.Sum(value => (value - mean) * (value - mean));
            double standardDeviation = Math.Sqrt(sumSquares / (n - 1));
            double d = 0;
            for (int i = 0; i < n; i++)
            {
                double cdf = NormalSurvival(-(x[i] - mean) / standardDeviation);
                d = Math.Max(d, Math.Max((i + 1.0) / n - cdf, cdf - (double)i / n));
            }

            double[] criticalValues = LillieforsCriticalValues(n);
            string notes = "Параметры нормального распределения оценены по выборке; SD с делителем n−1. "
                + "p приближённое: интерполяция таблицы statsmodels на основе 10 000 000 моделирований";
            if (n > LillieforsSizes[^1]) notes += "; асимптотическая калибровка";
            notes += ".";
            double p;
            NormalityPValueBound bound = NormalityPValueBound.None;
            double boundValue = double.NaN;
            if (d < criticalValues[0])
            {
                // The table only identifies the lower bound p >= .990. Use an
                // upper value for subsequent multiplicity correction to stay conservative.
                p = 1;
                bound = NormalityPValueBound.GreaterThanOrEqual;
                boundValue = 0.990;
                notes += " Табличная граница p ≥ 0,990; для поправок использовано консервативное p = 1.";
            }
            else if (d > criticalValues[^1])
            {
                p = 0.001;
                bound = NormalityPValueBound.LessThanOrEqual;
                boundValue = 0.001;
                notes += " Табличная граница p ≤ 0,001; для поправок использовано консервативное p = 0,001.";
            }
            else
            {
                int upper = 1;
                while (upper < criticalValues.Length - 1 && d > criticalValues[upper]) upper++;
                double proportion = (d - criticalValues[upper - 1])
                    / (criticalValues[upper] - criticalValues[upper - 1]);
                p = LillieforsTailProbabilities[upper - 1]
                    + proportion * (LillieforsTailProbabilities[upper] - LillieforsTailProbabilities[upper - 1]);
            }
            if (HasTies(x)) notes += " Есть совпадающие значения: калибровка предполагает непрерывные данные; учитывайте округление.";
            return new NormalityResult(n, d, p, "Колмогорова–Смирнова — Лиллиефорса", notes)
            {
                PValueBound = bound,
                BoundValue = boundValue
            };
        }

        private static double[] PrepareSample(IEnumerable<double> values, int minimum, string method)
        {
            if (values is null) throw new ArgumentNullException(nameof(values));
            double[] x = values.ToArray();
            if (x.Any(value => !double.IsFinite(value)))
                throw new ArgumentException("Проверка нормальности требует конечных числовых значений; NaN и бесконечность недопустимы.", nameof(values));
            if (x.Length < minimum)
                throw new ArgumentException($"Для критерия {method} требуется не менее {minimum} наблюдений.", nameof(values));
            Array.Sort(x);
            if (x[^1] == x[0])
                throw new ArgumentException("Проверка нормальности невозможна: все значения одинаковы, дисперсия равна нулю.", nameof(values));

            // Location/scale invariance avoids overflow with large finite values
            // and underflow with tiny nonzero ranges. Prefer direct differences
            // to retain precision when a large common offset is present.
            double minimumValue = x[0];
            double range = x[^1] - minimumValue;
            if (double.IsFinite(range))
            {
                for (int i = 0; i < x.Length; i++) x[i] = (x[i] - minimumValue) / range;
            }
            else
            {
                double scale = Math.Max(Math.Abs(x[0]), Math.Abs(x[^1]));
                double scaledMinimum = minimumValue / scale;
                double scaledRange = x[^1] / scale - scaledMinimum;
                for (int i = 0; i < x.Length; i++) x[i] = (x[i] / scale - scaledMinimum) / scaledRange;
            }
            return x;
        }

        private static bool HasTies(double[] x)
        {
            for (int i = 1; i < x.Length; i++) if (x[i] == x[i - 1]) return true;
            return false;
        }

        private static string ShapiroNotes(double[] x)
        {
            string notes = "Проверка нормальности; алгоритм AS R94 (Royston), калибровка SciPy.";
            if (x.Length > 5000) notes += " При n > 5000 точность приближённого p не гарантируется.";
            if (HasTies(x)) notes += " Есть совпадающие значения: учитывайте округление непрерывных данных.";
            return notes;
        }

        private static double Polynomial(double[] coefficients, double x)
        {
            double value = coefficients[^1];
            for (int i = coefficients.Length - 2; i >= 0; i--) value = value * x + coefficients[i];
            return value;
        }

        // AS 111 normal quantile, as used in SciPy's AS R94 coefficient construction.
        private static double NormalQuantile(double p)
        {
            double q = p - 0.5;
            if (Math.Abs(q) <= 0.42)
            {
                double r = q * q;
                return q * (((-25.44106049637 * r + 41.39119773534) * r - 18.61500062529) * r + 2.50662823884)
                    / ((((3.13082909833 * r - 21.06224101826) * r + 23.08336743743) * r - 8.47351093090) * r + 1);
            }
            double tail = Math.Sqrt(-Math.Log(q > 0 ? 1 - p : p));
            double result = (((2.32121276858 * tail + 4.85014127135) * tail - 2.29796479134) * tail - 2.78718931138)
                / ((1.63706781897 * tail + 3.54388924762) * tail + 1);
            return q < 0 ? -result : result;
        }

        // AS 66 normal survival function, including SciPy's extended upper tail.
        private static double NormalSurvival(double x)
        {
            bool upper = x > 0;
            double z = Math.Abs(x);
            if (!(z <= 7 || (upper && z <= 38))) return upper ? 0 : 1;
            double y = 0.5 * z * z;
            double tail;
            if (z <= 1.28)
                tail = 0.5 - z * (0.398942280444 - 0.399903438504 * y
                    / (y + 5.75885480458 - 29.8213557808 / (y + 2.62433121679 + 48.6959930692 / (y + 5.92885724438))));
            else
                tail = 0.398942280385 * Math.Exp(-y)
                    / (z - 3.8052e-8 + 1.00000615302 / (z + 3.98064794e-4 + 1.98615381364
                        / (z - 0.151679116635 + 5.29330324926 / (z + 4.8385912808
                            - 15.1508972451 / (z + 0.742380924027 + 30.789933034 / (z + 3.99019417011))))));
            return upper ? tail : 1 - tail;
        }

        private static double[] LillieforsCriticalValues(int n)
        {
            double[] result = new double[LillieforsTailProbabilities.Length];
            if (n > LillieforsSizes[^1])
            {
                double logN = Math.Log(n);
                double adjustment = -0.45068579 * logN - 0.00356741 * logN * logN;
                for (int j = 0; j < result.Length; j++)
                    result[j] = Math.Exp(LillieforsAsymptoticIntercepts[j] + adjustment);
                return result;
            }
            int upper = Array.BinarySearch(LillieforsSizes, n);
            if (upper >= 0) return (double[])LillieforsTable[upper].Clone();
            upper = ~upper;
            int lower = upper - 1;
            double proportion = (double)(n - LillieforsSizes[lower]) / (LillieforsSizes[upper] - LillieforsSizes[lower]);
            for (int j = 0; j < result.Length; j++)
                result[j] = LillieforsTable[lower][j]
                    + proportion * (LillieforsTable[upper][j] - LillieforsTable[lower][j]);
            return result;
        }

        private static readonly int[] LillieforsSizes = { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 25, 30, 40, 50, 100, 200, 400, 800, 1600 };
        private static readonly double[] LillieforsTailProbabilities = { 0.990, 0.950, 0.900, 0.750, 0.500, 0.250, 0.100, 0.075, 0.050, 0.025, 0.010, 0.005, 0.003, 0.001 };
        private static readonly double[] LillieforsAsymptoticIntercepts = { -1.17114969, -1.03298277, -0.95518114, -0.81912169, -0.6607348, -0.49861004, -0.35446139, -0.31737193, -0.26969888, -0.1979977, -0.11709649, -0.06398777, -0.0281863, 0.04129756 };
        private static readonly double[][] LillieforsTable =
        {
            new double[] { 0.14467854, 0.16876575, 0.18664724, 0.22120362, 0.25828924, 0.29341032, 0.34532673, 0.35917374, 0.37521968, 0.39563998, 0.41307904, 0.42157653, 0.4261507, 0.43265213 }, // n = 4
            new double[] { 0.13587046, 0.16098893, 0.17638354, 0.20235666, 0.2333944, 0.27766941, 0.31900772, 0.32936832, 0.34309223, 0.36727643, 0.39671728, 0.41322814, 0.42293504, 0.4386304 }, // n = 5
            new double[] { 0.12919635, 0.15139467, 0.16384021, 0.18597849, 0.2186713, 0.2585473, 0.29713753, 0.30829444, 0.32338252, 0.3456671, 0.37038945, 0.38760943, 0.40001813, 0.42304439 }, // n = 6
            new double[] { 0.12263812, 0.14163065, 0.15238656, 0.17435948, 0.20617949, 0.24243592, 0.28031415, 0.29068512, 0.3042307, 0.32532967, 0.35070348, 0.3678189, 0.37908881, 0.40078798 }, // n = 7
            new double[] { 0.11633728, 0.13297288, 0.14353311, 0.16537078, 0.19477376, 0.22936164, 0.2652001, 0.27501697, 0.28804474, 0.30862157, 0.33279908, 0.34911188, 0.3603517, 0.38252055 }, // n = 8
            new double[] { 0.11029593, 0.126086, 0.1365291, 0.15748048, 0.18510669, 0.21822055, 0.25223085, 0.26161129, 0.27415243, 0.29383667, 0.31708299, 0.33312043, 0.34406772, 0.36524182 }, // n = 9
            new double[] { 0.10487398, 0.12044377, 0.13065174, 0.15042835, 0.17679904, 0.20848607, 0.24098605, 0.25001629, 0.26202351, 0.2809226, 0.30341763, 0.31888089, 0.32960742, 0.35061378 }, // n = 10
            new double[] { 0.10036835, 0.11563925, 0.12540948, 0.14421024, 0.1695621, 0.19993088, 0.23119563, 0.23987246, 0.25142048, 0.26961902, 0.29148756, 0.30646087, 0.31678589, 0.33743951 }, // n = 11
            new double[] { 0.09649147, 0.11137479, 0.12069511, 0.13877133, 0.16313672, 0.19233706, 0.22244871, 0.23082937, 0.24194831, 0.25959488, 0.28077229, 0.29526801, 0.30538827, 0.32558601 }, // n = 12
            new double[] { 0.09318309, 0.10753337, 0.11647389, 0.13389325, 0.15739248, 0.18555597, 0.21463087, 0.22267463, 0.23346007, 0.25054975, 0.27100552, 0.28519234, 0.29504428, 0.31445229 }, // n = 13
            new double[] { 0.09024138, 0.1040431, 0.11266368, 0.12948657, 0.15221479, 0.17944478, 0.2075745, 0.21536969, 0.22585144, 0.2423819, 0.26231339, 0.2760657, 0.28555592, 0.30466148 }, // n = 14
            new double[] { 0.08750578, 0.10085666, 0.1092263, 0.12552356, 0.14751778, 0.17390304, 0.2011939, 0.20879449, 0.21891697, 0.23499116, 0.25427802, 0.26772298, 0.27706522, 0.29551017 }, // n = 15
            new double[] { 0.08501529, 0.09795236, 0.10607182, 0.1219047, 0.14327014, 0.16885124, 0.19537444, 0.20274039, 0.21257643, 0.22822187, 0.24703504, 0.26003621, 0.26913735, 0.28745211 }, // n = 16
            new double[] { 0.0827393, 0.09529635, 0.10320903, 0.11859867, 0.13936135, 0.16422592, 0.1900207, 0.19722541, 0.20683438, 0.22204864, 0.2402864, 0.25306469, 0.26200104, 0.2798316 }, // n = 17
            new double[] { 0.08063198, 0.09285546, 0.10055901, 0.11554003, 0.13575222, 0.15996707, 0.18509495, 0.1921063, 0.20145911, 0.2163409, 0.23420221, 0.24665892, 0.25527246, 0.27285099 }, // n = 18
            new double[] { 0.0786934, 0.09059543, 0.09810264, 0.11271608, 0.1324079, 0.15602767, 0.18052002, 0.18736281, 0.19652196, 0.21105313, 0.22854301, 0.24068403, 0.24912401, 0.26624557 }, // n = 19
            new double[] { 0.07686372, 0.08852309, 0.09582876, 0.11008398, 0.12930804, 0.15236687, 0.1762829, 0.18297149, 0.19190794, 0.20610189, 0.22327259, 0.23513834, 0.24349604, 0.2602868 }, // n = 20
            new double[] { 0.06933943, 0.07985746, 0.08645091, 0.09927555, 0.11656692, 0.1373056, 0.15889286, 0.16493761, 0.17300926, 0.18582499, 0.2013265, 0.21221932, 0.21979198, 0.2350391 }, // n = 25
            new double[] { 0.06380332, 0.07339662, 0.07942847, 0.09118039, 0.10701841, 0.12603791, 0.14586121, 0.15138566, 0.15878307, 0.17058849, 0.18492078, 0.19492393, 0.20198353, 0.21612086 }, // n = 30
            new double[] { 0.055784, 0.06414642, 0.06940064, 0.07961554, 0.09339439, 0.10994266, 0.12719016, 0.13202416, 0.13850018, 0.14877502, 0.16131916, 0.17009968, 0.17633078, 0.18870328 }, // n = 40
            new double[] { 0.05022994, 0.05773701, 0.06243507, 0.07160481, 0.08395875, 0.09882109, 0.11431303, 0.11863768, 0.12444044, 0.1337458, 0.14504504, 0.15299605, 0.15854061, 0.16985345 }, // n = 50
            new double[] { 0.0361047, 0.04146075, 0.0448043, 0.05131964, 0.06011456, 0.07068396, 0.08173283, 0.08483991, 0.08900091, 0.09564506, 0.10373761, 0.10937278, 0.11338394, 0.12161442 }, // n = 100
            new double[] { 0.02584151, 0.02963511, 0.03200533, 0.0366259, 0.04286406, 0.05036816, 0.05820564, 0.06041126, 0.06336215, 0.06809025, 0.0738379, 0.07788879, 0.08074047, 0.08663425 }, // n = 200
            new double[] { 0.01844162, 0.02112065, 0.02280475, 0.02607295, 0.03049198, 0.03580284, 0.04135097, 0.04290933, 0.04500619, 0.04834792, 0.0524239, 0.05530289, 0.05733374, 0.06145404 }, // n = 400
            new double[] { 0.0131231, 0.01501723, 0.01620598, 0.01852257, 0.02164634, 0.02540595, 0.02933599, 0.03043873, 0.03192094, 0.03429131, 0.03718247, 0.03922491, 0.0406419, 0.04361741 }, // n = 800
            new double[] { 0.00932049, 0.01066126, 0.01150366, 0.01314135, 0.0153528, 0.0180115, 0.02079512, 0.02157469, 0.02262168, 0.02429563, 0.02634302, 0.02777611, 0.02879721, 0.03088286 }, // n = 1600
        };
    }
}
