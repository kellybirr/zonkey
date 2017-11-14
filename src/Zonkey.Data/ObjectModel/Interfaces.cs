using System;
using System.Collections.Generic;
using System.Data;

namespace Zonkey.ObjectModel
{
	/// <summary>
	/// 
	/// </summary>
	public delegate TResult SortFunc<TDelegate, TResult>(TDelegate arg);

	/// <summary>
	/// Provides an interface that describes a class with data that can be written to a database.
	/// </summary>
	public interface ISavable
	{
		/// <summary>
		/// Commits the values.
		/// </summary>
		void CommitValues();

		/// <summary>
		/// Gets or sets the state of a <see cref="System.Data.DataRow"/>.
		/// </summary>
		/// <value>The state of the data row.</value>
		DataRowState DataRowState { get; set; }


		/// <summary>
		/// Gets the original values.
		/// </summary>
		IDictionary<string, object> OriginalValues { get; }
	}

	/// <summary>
	/// Provides an interface that describes a class to track deleted items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ITrackDeletedItems<T>
	{
		/// <summary>
		/// Gets the collection of deleted items.
		/// </summary>
		ICollection<T> DeletedItems { get; }
	}

	/// <summary>
	/// Provides an interface that provides methods to fill a <see cref="Zonkey.ObjectModel.DataClass"/> or <see cref="T:Zonkey.ObjectModel.DataClassCollection"/>.
	/// </summary>
	public interface ICustomFill
	{
		/// <summary>
		/// Fills the object.
		/// </summary>
		/// <param name="record">The record from the database, used to fill the object.</param>
		void FillObject(IDataRecord record);
	}

	/// <summary>
	/// Provides an interface that describes the custom attributes on a class.
	/// </summary>
	public interface IDataMapItem
	{
		/// <summary>
		/// Gets or sets the type of the access.
		/// </summary>
		/// <value>The type of the access.</value>
		AccessType AccessType { get; set; }

		/// <summary>
		/// Gets or sets the name of the save to table.
		/// </summary>
		/// <value>The name of the save to table.</value>
		string SaveToTable { get; set; }

		/// <summary>
		/// Gets or sets the name of the schema.
		/// </summary>
		/// <value>The name of the schema.</value>
		string SchemaName { get; set; }

		/// <summary>
		/// Gets or sets the select back.
		/// </summary>
		/// <value>The select back value of type <see cref="Zonkey.SelectBack"/>.</value>
		SelectBack SelectBack { get; set; }

		/// <summary>
		/// Gets or sets the name of the table.
		/// </summary>
		/// <value>The name of the table.</value>
		string TableName { get; set; }

		/// <summary>
		/// Gets or sets the update criteria.
		/// </summary>
		/// <value>The update criteria value of type <see cref="Zonkey.UpdateCriteria"/>.</value>
		UpdateCriteria UpdateCriteria { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [use quoted identifier].
		/// </summary>
		/// <value><c>true</c> if [use quoted identifier]; otherwise, <c>false</c>.</value>
		bool? UseQuotedIdentifier { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether fields are implicitly defined.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [implicit field definition]; otherwise, <c>false</c>.
		/// </value>
		bool ImplicitFieldDefinition { get; set; }
	}

	/// <summary>
	/// Provides an interface that describes the custom attributes of a class that inherits from <see cref="Zonkey.ObjectModel.IDataMapItem"/>
	/// </summary>
	public interface IDataMapField
	{
		/// <summary>
		/// Gets or sets the type of the access.
		/// </summary>
		/// <value>The type of the access.</value>
		AccessType AccessType { get; set; }

		/// <summary>
		/// Gets or sets the type of the data.
		/// </summary>
		/// <value>The type of the data.</value>
		System.Data.DbType DataType { get; set; }

		/// <summary>
		/// Gets or sets the name of the field.
		/// </summary>
		/// <value>The name of the database field.</value>
		string FieldName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is auto increment.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is auto increment; otherwise, <c>false</c>.
		/// </value>
		bool IsAutoIncrement { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is key field.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is key field; otherwise, <c>false</c>.
		/// </value>
		bool IsKeyField { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is nullable.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is nullable; otherwise, <c>false</c>.
		/// </value>
		bool IsNullable { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is row version.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is row version; otherwise, <c>false</c>.
		/// </value>
		bool IsRowVersion { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this field is comparable using (=) in a where clause.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this field is comparable; otherwise, <c>false</c>.
		/// </value>
		bool IsComparable { get; set; }

		/// <summary>
		/// Gets or sets the length.
		/// </summary>
		/// <value>The length of the field in the database.</value>
		int Length { get; set; }

		/// <summary>
		/// Gets or sets the property.
		/// </summary>
		/// <value>The property.</value>
		System.Reflection.PropertyInfo Property { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [use quoted identifier].
		/// </summary>
		/// <value><c>true</c> if [use quoted identifier]; otherwise, <c>false</c>.</value>
		bool? UseQuotedIdentifier { get; set; }

		/// <summary>
		/// Gets or sets the version of the table schema that contains this field
		/// </summary>
		int SchemaVersion { get; set; }

		/// <summary>
		/// Gets or sets the name of the sequence for an auto-increment column.
		/// </summary>
		/// <value>The name of the sequence.</value>
		string SequenceName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether strigns will trim to fit the length.
		/// </summary>
		/// <value><c>true</c> if [trim to fit]; otherwise, <c>false</c>.</value>
		bool TrimToFit { get; set; }

		/// <summary>
		/// Gets or sets the kind of the date time.
		/// </summary>
		/// <value>
		/// The kind of the date time.
		/// </value>
		DateTimeKind DateTimeKind { get; set; }
	}
}