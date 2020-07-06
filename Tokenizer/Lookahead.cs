using System.Collections;
using System.Collections.Generic;

namespace Tokenizer
{
    public class Lookahead<T> : IEnumerator<T>
    {
        public Lookahead(IEnumerable<T> t)
        {
            _enumerator = t.GetEnumerator();
            if (_enumerator.MoveNext())
            {
                _next = _enumerator.Current;
                _hasNext = true;
            }
        }

        private bool _hasNext;
        private readonly IEnumerator<T> _enumerator;
        private T _current;
        private T _next;

        public bool HasNext
        {
            get { return _hasNext; }
        }

        public bool MoveNext()
        {
            if (_hasNext)
            {
                _current = _next;
                _hasNext = _enumerator.MoveNext();
                if (_hasNext)
                {
                    _next = _enumerator.Current;
                }
                else
                {
                    _next = default(T);
                }
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _enumerator.Reset();
            _current = default(T);
            _next = default(T);
            _hasNext = false;
            if (_enumerator.MoveNext())
            {
                _next = _enumerator.Current;
                _hasNext = true;
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public T Current
        {
            get { return _current; }
        }

        public T Next
        {
            get { return _next; }
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
