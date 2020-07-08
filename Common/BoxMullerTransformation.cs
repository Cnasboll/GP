using System;

namespace Common
{
    class BoxMullerTransformation : INormalDistribution
    {
        public double Next(Random rand, double mean, double stdDev)
        {
            double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

            return randNormal;

        }

        public object Clone()
        {
            return new BoxMullerTransformation();
        }
    }
}