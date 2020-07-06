using System;
using System.Collections.Generic;

namespace Common
{
    public class ConstantsSet
    {
        private readonly IConstantsTable<int> _integers;
        private readonly IConstantsTable<double> _doubles;
        private readonly INormalDistribution _normalDistribution;

        public ConstantsSet(INormalDistribution normalDistribution)
        {
            _integers = new ConstantsTable<int>();
            _doubles = new ConstantsTable<double>();
            _normalDistribution = normalDistribution;
        }

        public ConstantsSet(IConstantsTable<int> integers, IConstantsTable<double> doubles, INormalDistribution normalDistribution)
        {
            _integers = integers;
            _doubles = doubles;
            _normalDistribution = normalDistribution;
        }

        public ConstantsSet() : this(new BoxMullerTransformation())
        {
        }

        public ConstantsSet(ConstantsSet constantsSet)
        {
            _integers = new ConstantsTable<int>(constantsSet.Integers);
            _doubles = new ConstantsTable<double>(constantsSet.Doubles);
            _normalDistribution = (INormalDistribution) constantsSet.NormalDistribution.Clone();
        }

        public IConstantsTable<int> Integers
        {
            get { return _integers; }
        }

        public IConstantsTable<double> Doubles
        {
            get { return _doubles; }
        }

        public INormalDistribution NormalDistribution
        {
            get { return _normalDistribution; }
        }

        public int PickIntegerConstant(Random rd)
        {
            int ix = rd.Next(Integers.Count + 1);
            if (ix >= Integers.Count)
            {
                int literal;
                do
                {
                    literal = NextRandomInteger(rd);
                } while (Integers.IndexOf(literal) >= 0);
                Integers.Add(literal);
            }
            return ix;
        }

        private static int NextRandomInteger(Random rd)
        {
            int literal;
            int digits;
            NextRandomInteger(rd, out literal, out digits);
            return literal;
        }

        public int MutateRandomIntegerConstant(Random rd)
        {
            if (Integers.Count == 0)
            {
                return PickIntegerConstant(rd);
            }
            int ix = rd.Next(Integers.Count);
            int literal;
            do
            {
                literal = MutateInteger(rd, NormalDistribution, Integers[ix]);
            } while (Integers.IndexOf(literal) >= 0);
            Integers[ix] = literal;
            return ix;
        }

        static void NextRandomInteger(Random rd, out int result, out int digits)
        {
            result = 0;
            digits = 0;
            do
            {
                result *= 10;
                result += rd.Next(10);
                ++digits;
            } while (rd.Next(10) == 0);
        }

        static int MutateInteger(Random rd, INormalDistribution normalDistribution, int literal)
        {
            return (int)Math.Round(literal * normalDistribution.Next(rd, 1.0, 0.25));
        }

        public int PickDoubleConstant(Random rd)
        {
            int ix = rd.Next(Doubles.Count + 1);
            if (ix >= Doubles.Count)
            {
                double constant;
                do
                {
                    constant = NextRandomDouble(rd);
                } while (Doubles.IndexOf(constant) >= 0);
                Doubles.Add(constant);
            }
            return ix;
        }

        public int MutateRandomDoubleConstant(Random rd)
        {
            if (Doubles.Count == 0)
            {
                return PickDoubleConstant(rd);
            }
            int ix = rd.Next(Doubles.Count);
            double constant;
            do
            {
                constant = MutateDouble(rd, NormalDistribution, Doubles[ix]);
            } while (Doubles.IndexOf(constant) >= 0);
            Doubles[ix] = constant;
            return ix;
        }

        static double NextRandomDouble(Random rd)
        {
            int integerPart = NextRandomInteger(rd);
            int decimalPart;
            int decimalPartPrecision;
            NextRandomInteger(rd, out decimalPart, out decimalPartPrecision);
            return integerPart + (Math.Abs(decimalPart) / Math.Pow(10.0, decimalPartPrecision));
        }

        static double MutateDouble(Random rd, INormalDistribution normalDistribution, double constant)
        {
            return constant * normalDistribution.Next(rd, 1.0, 0.25);
        }

        public ConstantsSet Merge(ConstantsSet constants, out IList<int> integerRhs2MergedMapping, out IList<int> doubleRhs2MergedMapping)
        {
            return new ConstantsSet(Integers.Merge(constants.Integers, out integerRhs2MergedMapping),
                                    Doubles.Merge(constants.Doubles, out doubleRhs2MergedMapping), NormalDistribution);
        }

        public void MutateDoubleConstant(Random rd, int qualifier)
        {
            Doubles[qualifier] = MutateDouble(rd, NormalDistribution, Doubles[qualifier]);
        }

        public void MutateIntegerConstant(Random rd, int qualifier)
        {
            Integers[qualifier] = MutateInteger(rd, NormalDistribution, Integers[qualifier]);
        }
    }
}