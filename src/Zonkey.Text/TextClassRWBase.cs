using System;
using System.Collections.Generic;
using System.Reflection;

namespace Zonkey.Text
{
	/// <summary>
	/// Base class for TextClassReader and TextClassWriter
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class TextClassRWBase<T> : IDisposable, ITextRecord
		where T : class
	{
		protected bool Disposed { get; set; }
		protected bool Initialized { get; set; }

		protected ITextField[] FieldArray { get; set; }

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			Disposed = true;
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="TextClassRWBase&lt;T&gt;"/> is reclaimed by garbage collection.
		/// </summary>
		~TextClassRWBase()
		{
			Dispose(false);
		}

		/// <summary>
		/// Gets or sets the type of the record.
		/// </summary>
		/// <value>The type of the record.</value>
		public TextRecordType RecordType { get; set; }

		/// <summary>
		/// Gets or sets the length of the record for fixed width files.
		/// </summary>
		/// <value>The length of the record.</value>
		public int RecordLength { get; set; }

		/// <summary>
		/// Gets or sets the delimiter.
		/// </summary>
		/// <value>The delimiter.</value>
		public char Delimiter { get; set; }

		/// <summary>
		/// Gets or sets the text qualifier.
		/// </summary>
		/// <value>The text qualifier.</value>
		public char TextQualifier { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the properties of the class are sequential.
		/// </summary>
		/// <value><c>true</c> if [sequential properties]; otherwise, <c>false</c>.</value>
		public bool SequentialProperties { get; set; }

		/// <summary>
		/// Gets or sets the string value to be used for line termination (default: "\r\n").
		/// Only current supported by TextClassWriter<typeparam name="T"></typeparam>
		/// </summary>
		/// <value>The new line value.</value>
		public string NewLine { get; set; }

		/// <summary>
		/// Initializes the object from class metadata.
		/// </summary>
		public void Initialize()
		{
			var recAttr = TextRecordAttribute.GetFromType(typeof(T));
			RecordType = (recAttr != null) ? recAttr.RecordType : TextRecordType.Delimited;
			SequentialProperties = (recAttr != null) && recAttr.SequentialProperties;
			NewLine = (recAttr != null) ? recAttr.NewLine : null;

			if (RecordType == TextRecordType.Delimited)
			{
				if (recAttr != null)
				{
					Delimiter = (recAttr.Delimiter == default(char)) ? ',' : recAttr.Delimiter;
					TextQualifier = (recAttr.TextQualifier == default(char)) ? '\"' : recAttr.TextQualifier;
				}
				else
				{
					Delimiter = ',';
					TextQualifier = '\"';
				}
			}

			var list = new List<ITextField>();
			foreach (var pi in typeof(T).GetTypeInfo().GetProperties())
			{
				var fieldAttr = TextFieldAttribute.GetFromProperty(pi);
				if (fieldAttr == null) continue;

				list.Add(fieldAttr);
			}

			if (! SequentialProperties)
				list.Sort((x, y) => (x.Position - y.Position));

			ProcessFieldList(list);
			FieldArray = list.ToArray();

			PostInitialize();
			Initialized = true;
		}

		/// <summary>
		/// Initializes the specified fields.
		/// </summary>
		/// <param name="fields">The fields.</param>
		public void Initialize(IEnumerable<ITextField> fields)
		{
			if (fields == null) throw new ArgumentNullException(nameof(fields));

			if (RecordType == TextRecordType.Delimited)
			{
				if (Delimiter == default(char)) Delimiter = ',';
				if (TextQualifier == default(char)) TextQualifier = '\"';
			}

			var list = new List<ITextField>(fields);

			if (!SequentialProperties)
				list.Sort((x, y) => (x.Position - y.Position));

			ProcessFieldList(list);
			FieldArray = list.ToArray();

			PostInitialize();
			Initialized = true;
		}

		private void ProcessFieldList(IList<ITextField> list)
		{
			if (RecordType == TextRecordType.FixedLength)
			{
				for (int n = 0; n < list.Count; n++)
				{
					ITextField field = list[n];
					if (SequentialProperties)
					{
						if (field.Position == TextField.AutoPosition)
							field.Position = RecordLength;
						else if (field.Length == 0)
						{
							if (list.Count <= (n + 1))
								throw new ArgumentException("Invalid Length in TextField " + n);

							var fNext = list[n + 1];
							if (fNext.Position <= 0)
								throw new ArgumentException("Invalid Length in TextField " + n);

							field.Length = fNext.Position - field.Position;
						}
					}

					int testEnd = field.Position + field.Length;
					RecordLength = Math.Max(RecordLength, testEnd);
				}
			}
			else if (SequentialProperties)
			{
				for (int n = 0; n < list.Count; n++)
				{
					if (list[n].Position == TextField.AutoPosition)
						list[n].Position = n;
				}
			}                
		}

		/// <summary>
		/// initialize extra bits needed by subclasses
		/// </summary>
		protected virtual void PostInitialize()
		{
			
		}
	}
}
