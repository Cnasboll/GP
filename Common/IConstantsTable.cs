using System.Collections.Generic;

namespace Common
{
    public interface IConstantsTable<T> : IList<T>
    {
        IConstantsTable<T> Merge(IConstantsTable<T> rhs, out IList<int> rhs2MergedMapping);
        IList<T> Constants { get; }        
        int Include(T constant);
    }
}