using System;

namespace Common
{
    class BoxMullerTransformation : INormalDistribution
    {
        public decimal Next(Random rand, decimal mean, decimal stdDev)
        {
            double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = rand.NextDouble();
            decimal randStdNormal = new decimal(Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2)); //random normal(0,1)
            decimal randNormal =
                mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

            return randNormal;

        }

        public object Clone()
        {
            return new BoxMullerTransformation();
        }
    }
}