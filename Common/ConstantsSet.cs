﻿using System;
using System.Collections.Generic;

namespace Common
{
    public class ConstantsSet
    {
        private readonly IConstantsTable<Int64> _integers;
        private readonly IConstantsTable<decimal> _doubles;
        private readonly INormalDistribution _normalDistribution;

        public ConstantsSet(INormalDistribution normalDistribution)
        {
            _integers = new ConstantsTable<Int64>();
            _doubles = new ConstantsTable<decimal>();
            _normalDistribution = normalDistribution;
        }

        public ConstantsSet(IConstantsTable<Int64> integers, IConstantsTable<decimal> doubles, INormalDistribution normalDistribution)
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
            _integers = new ConstantsTable<Int64>(constantsSet.Integers);
            _doubles = new ConstantsTable<decimal>(constantsSet.Doubles);
            _normalDistribution = (INormalDistribution) constantsSet.NormalDistribution.Clone();
        }

        public IConstantsTable<Int64> Integers => _integers;

        public IConstantsTable<decimal> Doubles => _doubles;

        public INormalDistribution NormalDistribution => _normalDistribution;

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

        public Int64 MutateRandomIntegerConstant(Random rd)
        {
            if (Integers.Count == 0)
            {
                return PickIntegerConstant(rd);
            }
            int ix = rd.Next(Integers.Count);
            Int64 literal;
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

        static Int64 MutateInteger(Random rd, INormalDistribution normalDistribution, Int64 literal)
        {
            return (Int64)Math.Round(literal * normalDistribution.Next(rd, 1.0, 0.25));
        }

        public int PickDoubleConstant(Random rd)
        {
            int ix = rd.Next(Doubles.Count + 1);
            if (ix >= Doubles.Count)
            {
                decimal constant;
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
            decimal constant;
            do
            {
                constant = MutateDouble(rd, NormalDistribution, Doubles[ix]);
            } while (Doubles.IndexOf(constant) >= 0);
            Doubles[ix] = constant;
            return ix;
        }

        static decimal NextRandomDouble(Random rd)
        {
            int integerPart = NextRandomInteger(rd);
            int decimalPart;
            int decimalPartPrecision;
            NextRandomInteger(rd, out decimalPart, out decimalPartPrecision);
            return new decimal(integerPart + (Math.Abs(decimalPart) / Math.Pow(10.0, decimalPartPrecision)));
        }

        static decimal MutateDouble(Random rd, INormalDistribution normalDistribution, decimal constant)
        {
            return constant * (decimal)normalDistribution.Next(rd, 1.0, 0.25);
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