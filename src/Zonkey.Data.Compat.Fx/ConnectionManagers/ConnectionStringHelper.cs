using System;
using System.Configuration;
using System.Reflection;

namespace Zonkey.ConnectionManagers
{
	/// <summary>
	/// Contains static helper methods for manipulating and interpreting connection strings
	/// </summary>
	public static class ConnectionStringHelper
	{
		/// <summary>
		/// Enables adding connection strings to at runtime to the ConfiguationManager.ConnectionStrings collection.
		/// </summary>
		public static bool EnableAddingConnectionStrings()
		{
			Type configSectionType = typeof(ConfigurationElementCollection);
			FieldInfo readOnlyField = configSectionType.GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
			if (readOnlyField == null) return false;

			readOnlyField.SetValue(ConfigurationManager.ConnectionStrings, false);
			return true;
		}
	}
}
