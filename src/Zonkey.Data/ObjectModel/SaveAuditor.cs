using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace Zonkey.ObjectModel
{
	/// <summary>
	/// This class does auditing of saves to objects
	/// </summary>
	public class SaveAuditor : IDisposable
	{
		private DataClassAdapter _adapter;
		private readonly EventHandler<BeforeSaveEventArgs> _saveHandler;

		/// <summary>
		/// Gets or sets the audit handler.
		/// </summary>
		/// <value>The audit handler.</value>
		public Action<SaveAudit> AuditHandler { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SaveAuditor"/> class.
		/// </summary>
		/// <param name="adapter">The adapter.</param>
		public SaveAuditor(DataClassAdapter adapter)
		{
			_adapter = adapter;

			_saveHandler = OnBeforeSave;
			_adapter.BeforeSave += _saveHandler;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SaveAuditor"/> class.
		/// </summary>
		/// <param name="adapter">The adapter.</param>
		/// <param name="auditHandler">The audit handler.</param>
		public SaveAuditor(DataClassAdapter adapter, Action<SaveAudit> auditHandler) 
			: this(adapter)
		{
			AuditHandler = auditHandler;
		}

		~SaveAuditor()
		{
			if (_adapter != null)	
				Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (_adapter != null)
			{
				lock (this)
				if (_adapter != null)
				{
					_adapter.BeforeSave -= _saveHandler;
					_adapter = null;
				}
			}

			if (disposing)
				GC.SuppressFinalize(this);

		}

		void OnBeforeSave(object sender, BeforeSaveEventArgs e)
		{
			DataMap map = e.DataMap;
			Type t = map.ObjectType;
		    TypeInfo ti = t.GetTypeInfo();
			ISavable obj = e.DataObject;

			var audit = new SaveAudit
			            	{
								Action = e.SaveType,
			            		TableName = e.DataMap.DataItem.SaveToTable
			            	};

			if (! string.IsNullOrEmpty(e.DataMap.DataItem.SchemaName))
				audit.SchemaName = e.DataMap.DataItem.SchemaName;

			if (e.SaveType == SaveType.Insert)
			{
				foreach (PropertyInfo pi in ti.GetProperties())
				{
					if ((pi.Name == "DataRowState") || (pi.Name == "OriginalValues"))
						continue;

					audit.Properties.Add( new SaveAuditProperty(pi.Name, null, pi.GetValue(obj, null)) );
				}
			}
			else if (e.SaveType == SaveType.Update)
			{				
				foreach (IDataMapField field in e.DataMap.KeyFields)
					audit.Keys.Add( new SaveAuditProperty(field.FieldName, field.Property.GetValue(obj, null), null) );

				foreach (var kv in obj.OriginalValues)
				{
					PropertyInfo pi = ti.GetProperty(kv.Key);
					object v = pi.GetValue(obj, null);

					if (((kv.Value != null) && (!kv.Value.Equals(v))) || ((kv.Value == null) && (v != null)))
					{
						audit.Properties.Add( new SaveAuditProperty(kv.Key, kv.Value, v) );
					}
				}
			}

			if ( (AuditHandler != null) && (audit.Properties.Count > 0) )
				AuditHandler(audit);
		}
	}

	/// <summary>
	/// A Capture of an audited Save
	/// </summary>
	public class SaveAudit
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SaveAudit"/> class.
		/// </summary>
		public SaveAudit()
		{
			Keys = new List<SaveAuditProperty>();
			Properties = new List<SaveAuditProperty>();
		}

		/// <summary>
		/// Gets or sets the action.
		/// </summary>
		/// <value>
		/// The action.
		/// </value>
		public SaveType Action { get; set; }

		/// <summary>
		/// Gets or sets the name of the table.
		/// </summary>
		/// <value>
		/// The name of the table.
		/// </value>
		public string TableName { get; set; }

		/// <summary>
		/// Gets or sets the name of the schema.
		/// </summary>
		/// <value>
		/// The name of the schema.
		/// </value>
		public string SchemaName { get; set; }

		/// <summary>
		/// Gets or sets the keys.
		/// </summary>
		/// <value>
		/// The keys.
		/// </value>
		public IList<SaveAuditProperty> Keys { get; private set; }

		/// <summary>
		/// Gets or sets the properties.
		/// </summary>
		/// <value>
		/// The properties.
		/// </value>
		public IList<SaveAuditProperty> Properties { get; private set; }

		/// <summary>
		/// Gets an XML representation of the change .
		/// </summary>
		/// <returns></returns>
		public XElement ToXml()
		{
			var x = new XElement("save_audit",
				new XAttribute("action", Action.ToString()),
				new XAttribute("table", TableName)
			);

			if (!string.IsNullOrEmpty(SchemaName))
				x.Add(new XAttribute("schema", SchemaName));

			foreach (var p in Keys)
			{
				x.Add(
					new XElement("key",
						new XAttribute("name", p.PropertyName),
						new XElement("value", p.OldValue)
						)
					);
			}

			foreach (var p in Properties)
			{
				var xe = new XElement("property", new XAttribute("name", p.PropertyName) );
				
				if (Action == SaveType.Update)
					xe.Add(new XElement("old_value", p.OldValue));

				xe.Add(new XElement("new_value", p.NewValue));

				x.Add(xe);
			}

			return x;
		}

		public override string ToString()
		{
			return ToXml().ToString();
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <param name="options">The options.</param>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public string ToString(SaveOptions options)
		{
			return ToXml().ToString(options);
		}
	}

	/// <summary>
	/// A property captured in a save audit
	/// </summary>
	public class SaveAuditProperty
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SaveAuditProperty"/> class.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		public SaveAuditProperty(string propertyName, object oldValue, object newValue)
		{
			PropertyName = propertyName;
			OldValue = oldValue;
			NewValue = newValue;
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		/// <value>
		/// The name of the property.
		/// </value>
		public string PropertyName { get; private set; }

		/// <summary>
		/// Gets the old value.
		/// </summary>
		public object OldValue { get; private set; }

		/// <summary>
		/// Gets the new value.
		/// </summary>
		public object NewValue { get; private set; }
	}
}
