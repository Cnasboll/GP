using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GP;

namespace gp
{
    public class Problem
    {
        private readonly int _varnumber;
        private readonly List<Target> _targets;

        public Problem(List<Target> targets, int varnumber)
        {
            this._targets = targets;
            this._varnumber = varnumber;
        }

        public int Varnumber => _varnumber;

        public List<Target> Targets => _targets;

        public int Count => this._targets.Count;

        public Target this[int i] => this._targets[i];

        public static Problem Read(String fname)
        {
            using (var fin = new StreamReader(fname))
            {
                return Parse(fin.ReadToEnd());
            }
        }

        public static Problem Parse(String text)
        {
            string[] lines = text.Split('\n');

            if (lines.Length > 0)
            {
                string line = lines[0];
                var tokens = line.Split(' ');
                int varnumber = Int32.Parse(tokens[0].Trim());
                var targets = new List<Target>();

                for (int i = 1; i < lines.Length; ++i)
                {
                    line = lines[i].Trim();

                    if (line.Length == 0)
                    {
                        continue;
                    }
                    targets.Add(Target.Parse(line));
                }

                return new Problem(targets, varnumber);
            }
            throw new ArgumentOutOfRangeException($"Cannot parse {text} as a problem");
        }
    }
}
