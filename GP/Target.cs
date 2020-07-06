using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace gp
{
    public class Target : IEnumerable
    {
        private readonly List<double> _target;

        public static Target Parse(String text)
        {
            var tokens = text.Split(' ');
            var target = new List<double>();
            foreach (string token in tokens)
            {
                var trimmed = token.Trim();
                if (!trimmed.Equals(""))
                {
                    target.Add(trimmed == "?"
                        ? Double.NaN
                        : Double.Parse(trimmed, CultureInfo.GetCultureInfo("en-GB").NumberFormat));
                }
            }
            return new Target(target);
        }

        public Target(List<double> target)
        {
            this._target = target;
        }

        public IEnumerator<double> GetEnumerator()
        {
            return this._target.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // Define the indexer to allow client code to use [] notation.
        public double this[int i] => this._target[i];
    }
}
