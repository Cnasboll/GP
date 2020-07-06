using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace GP
{
    public class Output : IEquatable<Output>
    {
        #region Public

        public Output(double result, IEnumerable<double> variables)
        {
            _result = result;
            _variables = new List<double>(variables);

        }

        public Output(Output output) : this(output._result, output._variables)
        {
            
        }


        public double Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
            }
        }

        public double Distance(Output output)
        {

            return Formulae.CalculateEcludianDistance(GetEnumerable(), output.GetEnumerable());
        }

        private IEnumerable<double> GetEnumerable()
        {
            return _result.AsEnumerable().Concat(_variables).Select(_ => double.IsNaN(_) ? 0 : _);
        }

        public double this[int index]
        {
            get
            {
                return _variables[index];
            }
            set
            {
                _variables[index] = value;
            }
        }

        public int Length
        {
            get
            {
                return _variables.Count;
            }
        }

        /// <summary>
        ///                     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="obj" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="obj">
        ///                     An object to compare with this object.
        ///                 </param>
        public bool Equals(Output obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj._result == _result && Equals(obj._variables, _variables);
        }

        /// <summary>
        ///                     Determines whether the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />; otherwise, false.
        /// </returns>
        /// <param name="obj">
        ///                     The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />. 
        ///                 </param>
        /// <exception cref="T:System.NullReferenceException">
        ///                     The <paramref name="obj" /> parameter is null.
        ///                 </exception><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Output)) return false;
            return Equals((Output) obj);
        }

        /// <summary>
        ///                     Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        ///                     A hash code for the current <see cref="T:System.Object" />.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                return (_result.GetHashCode()*397) ^ (_variables != null ? _variables.GetHashCode() : 0);
            }
        }

        public static bool operator ==(Output left, Output right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Output left, Output right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region Private

        private double _result;
        private readonly List<double> _variables;

        #endregion
    }
}
