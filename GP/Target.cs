using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace gp
{
    public class Target : IEnumerable
    {
        private readonly List<decimal> _inputs;
        private decimal? _expectedResult;

        public decimal? ExpectedResult => _expectedResult;

        public static Target Parse(String text)
        {
            var tokens = text.Split(' ');
            var inputs = new List<decimal>();
            decimal? expectedResult;
            for (int i = 0; i < tokens.Length-1; ++i)
            {
                var trimmed = tokens[i].Trim();
                if (!trimmed.Equals(""))
                {
                    inputs.Add(decimal.Parse(trimmed, CultureInfo.GetCultureInfo("en-GB").NumberFormat));
                }
            }

            var trimmedTargetToken = tokens.Last().Trim();
            if (trimmedTargetToken == "?")
            {
                expectedResult = null;
            }
            else
            {
                expectedResult = decimal.Parse(trimmedTargetToken, CultureInfo.GetCultureInfo("en-GB").NumberFormat);
            }

            return new Target(inputs, expectedResult);
        }

        public Target(List<decimal> inputs, decimal? expectedResult = null)
        {
            this._inputs = inputs;
            this._expectedResult = expectedResult;
        }

        public IEnumerator<decimal> GetEnumerator()
        {
            return this._inputs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // Define the indexer to allow client code to use [] notation.
        public decimal this[int i] => this._inputs[i];
    }
}
