using System;

namespace Common
{
    public interface INormalDistribution : ICloneable
    {
        decimal Next(Random rand, decimal mean, decimal stdDev);
    }
}