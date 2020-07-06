using System.Collections;
using System.Collections.Generic;
using Common;

namespace Common
{
    class ConstantsTable<T> : IConstantsTable<T>
    {
        private readonly IList<T> _constants;

        public ConstantsTable(IConstantsTable<T> constants)
        {
            _constants = new List<T>(constants.Constants);
        }
        public ConstantsTable(IList<T> list)
        {
            _constants = list;
        }

        public ConstantsTable()
        {
            _constants = new List<T>();
        }

        /// <summary>
        /// Merges two sets of _constants and generates a rhs2MergedMapping table translating indexes in rhs to indexes in merged.

        /// Elements of lhs keep their original index when added to merged.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lhs">List to merge with rhs</param>
        /// <param name="rhs">List to merge with lhs</param>
        /// <param name="merged">New list containing all elements in lhs followed by any elements in rhs that was not present in lhs</param>

        /// <param name="rhs2MergedMapping">Mapping of each index in rhs to each index in merged</param>
        private static void Merge(IList<T> lhs, IEnumerable<T> rhs, out IList<T> merged, out IList<int> rhs2MergedMapping)
        {
            //First the merged equals lhs
            merged = new List<T>(lhs);

            //We then create table mapping each element in rhs to exactly one element in the merged table.
            rhs2MergedMapping = new List<int>();

            foreach (T t in rhs)
            {
                int ix = lhs.IndexOf(t);
                if (ix < 0)
                {
                    //This entry did not exist in lhs, add the mapping and to the merged table.
                    rhs2MergedMapping.Add(merged.Count);
                    merged.Add(t);
                }
                else
                {
                    //t was already present in lhs, add a mapping to that index.
                    rhs2MergedMapping.Add(ix);
                }
            }
        }

        public IConstantsTable<T> Merge(IConstantsTable<T> rhs, out IList<int> rhs2MergedMapping)
        {
            IList<T> merged;            
            Merge(_constants, rhs.Constants, out merged, out rhs2MergedMapping);
            return new ConstantsTable<T>(merged);
        }

        public IList<T> Constants
        {
            get { return _constants; }
        }

        public int RecodeRhsIndex(int qualifier)
        {
            return qualifier;
        }

        public int Include(T constant)
        {
            int index = _constants.IndexOf(constant);
            if (index < 0)
            {
                index = _constants.Count;
                _constants.Add(constant);
            }
            return index;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _constants.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            _constants.Add(item);
        }

        public void Clear()
        {
            _constants.Clear();
        }

        public bool Contains(T item)
        {
            return _constants.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _constants.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _constants.Remove(item);
        }

        public int Count
        {
            get { return _constants.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(T item)
        {
            return _constants.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _constants.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _constants.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return _constants[index]; }
            set { _constants[index] = value; }
        }
    }
}