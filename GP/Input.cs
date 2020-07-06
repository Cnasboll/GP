using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GP
{
    public class Input : IEquatable<Input>
    {


        #region Public

        public Input(Input input) : this (input._input)
        {

        }

        public Input(IEnumerable<double> input)
        {
            _input = new List<double>(input);
        }

        public int Length
        {
            get { return _input.Count; }
        }

        public double this[int index]
        {
            get
            {
                return _input[index];
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
        public bool Equals(Input obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj._input, _input);
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
            if (obj.GetType() != typeof (Input)) return false;
            return Equals((Input) obj);
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
            return (_input != null ? _input.GetHashCode() : 0);
        }

        public override string ToString()
        {
            if (_input.Count == 0)
            {
                return "";
            }
            var stringBuilder = new StringBuilder(string.Format("X0={0}", _input[0]));
            for (int i = 1; i < _input.Count; ++i)
            {
                stringBuilder.Append(string.Format(", X{0}={1}", i, _input[i].ToString(CultureInfo.GetCultureInfo("en-GB").NumberFormat)));
            }
            return stringBuilder.ToString();
        }

        bool IEquatable<Input>.Equals(Input other)
        {
            return Equals(other);
        }

        public static bool operator ==(Input left, Input right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Input left, Input right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region Private
        private readonly List<double> _input;
        #endregion
    }
}
