using System;

namespace Common
{
    public interface INormalDistribution : ICloneable
    {
        double Next(Random rand, double mean, double stdDev);
    }
}