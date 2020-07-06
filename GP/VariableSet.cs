using System;
using System.Collections;
using System.Collections.Generic;

namespace gp
{
    public class VariableSet : IList<double>
    {
        private readonly List<double> _variables;
        public VariableSet()
        {
            _variables = new List<double>();
        }

        public VariableSet(VariableSet variables)
        {
            _variables = new List<double>(variables._variables);
        }

        public int IndexOf(double item)
        {
            return _variables.IndexOf(item);
        }

        public void Insert(int index, double item)
        {
            _variables.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _variables.RemoveAt(index);
        }

        public double this[int x]
        {
            get
            {
                if (x >= _variables.Count)
                {                       
                    return 0.0;
                }

                return _variables[x];
            }
            set
            {
                if (_variables.Capacity <= x)
                {
                    _variables.Capacity = x + 1;
                }
                while (x > _variables.Count)
                {
                    _variables.Add(0.0);
                }
                if (x == _variables.Count)
                {
                    _variables.Add(value);
                }
                else
                {
                    _variables[x] = value;
                }
            }
        }

        public IEnumerator<double> GetEnumerator()
        {
            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(double item)
        {
            _variables.Add(item);
        }

        public void Clear()
        {
            _variables.Clear();
        }

        public bool Contains(double item)
        {
            return _variables.Contains(item);
        }

        public void CopyTo(double[] array, int arrayIndex)
        {
            _variables.CopyTo(array, arrayIndex);
        }

        public bool Remove(double item)
        {
            return _variables.Remove(item);
        }

        public int Count
        {
            get { return _variables.Count; }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(VariableSet)) return false;
            return Equals((VariableSet)obj);
        }

        public bool Equals(VariableSet obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            //Cannot call _variables.Equals(obj._variables) as trailing zeroes are implicit
            for (int i = 0; i < Math.Max(_variables.Count, obj._variables.Count); ++i)
            {
                if (this[i] != obj[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return(_variables != null ? _variables.GetHashCode() : 0);
            }
        }
    }
}