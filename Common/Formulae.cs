using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public static class Formulae
    {
        public static double CalculateEcludianDistance(IEnumerable<double> point1, IEnumerable<double> point2)
        {
            //TODO: Use Math.Sqrt(point1List.Zip(point2List, (a, b => Maths.Abs((b-a))*(b-a))).Sum())
            return CalculateEcludianDistance(Diff(point1, point2));
        }

        public static IEnumerable<double> Diff(IEnumerable<double> values1, IEnumerable<double> values2)
        {
            List<double> valueList1 = values1.ToList();
            List<double> valueList2 = values2.ToList();
            for (int i = 0; i < valueList1.Count; ++i)
            {
                yield return valueList2[i] - valueList1[i];
            }
        }

        public static double CalculateEcludianDistance(IEnumerable<double> values)
        {
            return Math.Sqrt(values.Select(_ => _ * _).Sum());
        }
    }
}
