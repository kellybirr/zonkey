using System;
using System.ComponentModel;
using System.Reflection;

namespace Zonkey.ObjectModel
{
	/// <summary>
	/// Provides methods to compare properties on a class.
	/// </summary>
	/// <remarks>
	/// Inherits from <see cref="T:System.Collections.Generic.IComparer"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the properties to compare.</typeparam>
	public class PropertyComparer<T> : System.Collections.Generic.IComparer<T>
	{
		private readonly PropertyInfo _propertyInfo;
		private readonly ListSortDirection _direction;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Zonkey.ObjectModel.PropertyComparer"/> class.
		/// </summary>
		/// <param name="property">The <see cref="System.ComponentModel.PropertyDescriptor"/> instance of the property on a class.</param>
		/// <param name="direction">The direction.</param>
		public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
		{
			_direction = direction;
			_propertyInfo = typeof(T).GetTypeInfo().GetProperty(property.Name);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Zonkey.ObjectModel.PropertyComparer"/> class.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="direction">The <see cref="ListSortDirection"/> to sort.</param>
		public PropertyComparer(string propertyName, ListSortDirection direction)
		{
			_direction = direction;
			_propertyInfo = typeof(T).GetTypeInfo().GetProperty(propertyName);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Zonkey.ObjectModel.PropertyComparer"/> class.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="descending">
		/// if set to <c>true</c> then sets the sort direction to <see cref="ListSortDirection.Descending"/>, 
		/// otherwise <see cref="ListSortDirection.Ascending"/>.
		/// </param>
		public PropertyComparer(string propertyName, bool descending)
		{
			_direction = (descending) ? ListSortDirection.Descending : ListSortDirection.Ascending;
			_propertyInfo = typeof(T).GetTypeInfo().GetProperty(propertyName);
		}

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// if the sort direction is ascending, then value less than zero if <paramref name="x"/> is less than <paramref name="y"/>; zero if <paramref name="x"/> equals <paramref name="y"/>;
		/// greater than zero if <paramref name="x"/> is greater than <paramref name="y"/>.
		/// if the sort direction is descending, then value less than zero if <paramref name="x"/> is greater than <paramref name="y"/>; zero if <paramref name="x"/> equals <paramref name="y"/>;
		/// less than zero if <paramref name="x"/> is less than <paramref name="y"/>
		/// </returns>
		public int Compare(T x, T y)
		{
			// Get property values
			object xValue = _propertyInfo.GetValue(x, null);
			object yValue = _propertyInfo.GetValue(y, null);

			if (xValue == null)
			{
				if (yValue == null) return 0;
				return (_direction == ListSortDirection.Ascending) ? -1 : 1;
			}

			if (yValue == null)
				return (_direction == ListSortDirection.Ascending) ? 1 : -1;

			// Do comparison
			if (xValue is IComparable)
			{
				int resultC = ((IComparable) xValue).CompareTo(yValue);
				return (_direction == ListSortDirection.Ascending) ? resultC : -(resultC);
			}
			
			// check simple equals
			if (xValue.Equals(yValue))
				return 0;

			// string compare as last resort
			int resultS = xValue.ToString().CompareTo(yValue.ToString());
			return (_direction == ListSortDirection.Ascending) ? resultS : -(resultS);
		}
	}
}