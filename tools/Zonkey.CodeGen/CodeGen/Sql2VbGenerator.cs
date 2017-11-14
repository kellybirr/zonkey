using System;
using System.Data;
using System.Data.Common;

namespace ZonkeyCodeGen.CodeGen
{
    /// <summary>
    /// Provides methods to generate VB.NET code for a <see cref="Zonkey.ObjectModel.DataClass"/> from a table in a database.
    /// </summary>
    public class Sql2VBGenerator : ClassGenerator
    {
        /// <summary>
        /// Writes the <see cref="Zonkey.ObjectModel.DataClass"/> VB.NET code to a <see cref="System.IO.TextWriter"/> instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        public override void Generate()
        {
            DbCommand command = Connection.CreateCommand();
            command.CommandText = string.Format("SELECT * FROM {0} WHERE 0=1", TableName);

            DbDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly);
            DataTable dt = reader.GetSchemaTable();
            reader.Close();

            WriteLine("Imports System");
            WriteLine("Imports System.Data");
            WriteLine("Imports Zonkey.ObjectModel");
            WriteEndLine();

            if (! String.IsNullOrEmpty(Namespace))
            {
                WriteLine("Namespace {0}", Namespace);
                TabLevel++;
            }

            //WriteLine("<DataItem(\"{0}\")> _", TableName);
            if (string.IsNullOrEmpty(SchemaName))
                WriteLine("<DataItem(\"{0}\")> _", TableName);
            else
                WriteLine("<DataItem(\"{0}\", SchemaName = \"{1}\")> _", (TableName.Split('.'))[1], SchemaName);          
            
            WriteLine("Public Class {0}", ClassName);
            TabLevel++;

            WriteLine("Inherits Zonkey.ObjectModel.DataClass");
            WriteEndLine();

            if (PrivateFieldsAtTop)
            {
                WriteLine("#Region \"Private Fields\"");
                WriteEndLine();
                foreach (DataRow row in dt.Rows)
                {
                    WriteBeginLine();
                    Write("Private m_{0}", row["ColumnName"]);
                    Write(" As {0}", GetNativeType(row));
                    WriteEndLine();
                }
                WriteEndLine();
                WriteLine("#End Region");
                WriteEndLine();
            }

            string guidToInit = null;

            WriteLine("#Region \"Data Columns (Properties)\"");
            WriteEndLine();
            foreach (DataRow row in dt.Rows)
            {
                string sDbType = GetDbType(row);
                string sNativeType = GetNativeType(row);
                string sPrivateName = "m_" + row["ColumnName"];
                //bool isKeyField = String.Equals((string)row["ColumnName"], KeyFieldName, StringComparison.CurrentCultureIgnoreCase);
				var isKeyField = KeyFieldName.Contains((string)row["ColumnName"]);

                if (isKeyField && (sNativeType == "Guid"))
                    guidToInit = sPrivateName;

                WriteBeginLine();
                Write("<DataField(\"{0}\", DbType.{1}, ", row["ColumnName"], sDbType);
                Write(((bool)row["AllowDbNull"]) ? "True" : "False");
                if ((sDbType == "Binary") || (sDbType.IndexOf("String") >= 0))
                    Write(", Length:={0}", row["ColumnSize"]);
                if (isKeyField) Write(", IsKeyField = true");
                if ((bool)row["IsAutoIncrement"]) Write(", IsAutoIncrement:=True");
                if ((bool)row["IsRowVersion"]) Write(", IsRowVersion:=True");
                Write(")> _");
                WriteEndLine();

                WriteBeginLine();
                Write("Public ");
                if (VirtualProperties) Write("Overridable ");
                Write("Property ");
                Write(row["ColumnName"].ToString());
                Write(" As ");
                Write(sNativeType);
                WriteEndLine();
                TabLevel++;

                WriteLine("Get");
                WriteLine("\tReturn m_{0}", row["ColumnName"]);
                WriteLine("End Get");

                WriteLine("Set");
                WriteLine("\tSetFieldValue(\"{0}\", m_{0}, Value)", row["ColumnName"]);
                WriteLine("End Set");

                TabLevel--;
                WriteLine("End Property");

                if (! PrivateFieldsAtTop)
                    WriteLine("Private m_{0} As {1}", row["ColumnName"], sNativeType);

                WriteEndLine();
            }
            WriteLine("#End Region");
            WriteEndLine();

            WriteLine("#Region \"Constructors\"");
            WriteEndLine();

            WriteLine("Public Sub New(ByVal addingNew As Boolean)");
            TabLevel++;

            WriteLine("MyBase.New(addingNew)");
            WriteLine("If addingNew Then");
            TabLevel++;
            if (!string.IsNullOrEmpty(guidToInit))
            {
                WriteBeginLine();
                Write(guidToInit);
                Write(" = Guid.NewGuid()");
                WriteEndLine();
            }

            foreach (string line in AddConstructorCode)
                WriteLine(line);

            TabLevel--;                
            WriteLine("End If");

            TabLevel--;
            WriteLine("End Sub");
            WriteEndLine();

            WriteLine("<Obsolete(\"This parameterless constructor is required by the DataClassAdapter, but should never be used directly in code.\", true)> _");
            WriteLine("Public Sub New()");           
            WriteLine("\tMyClass.New(false)");            
            WriteLine("End Sub");
            WriteEndLine();

            WriteLine("#End Region");
            WriteEndLine();

            TabLevel--;
            WriteLine("End Class"); // end class
            WriteEndLine();

            if (GenerateCollections > GenerateCollectionMode.None)
            {
                WriteLine("#Region \"Typed Collection\"");
                WriteEndLine();

                WriteLine("Public Class {0}Collection", ClassName);
                TabLevel++;

                if (GenerateCollections == GenerateCollectionMode.GenericCollection)
                {
                    WriteLine("Inherits System.Collections.ObjectModel.Collection(Of {0})", ClassName);
                    WriteEndLine();
                }
                else if (GenerateCollections == GenerateCollectionMode.DataClassCollection)
                {
                    WriteLine("Inherits Zonkey.ObjectModel.DataClassCollection(Of {0})", ClassName);
                    WriteEndLine();
                }
                else if (GenerateCollections == GenerateCollectionMode.BindableCollection)
                {
                    WriteLine("Inherits Zonkey.ObjectModel.BindableCollection(Of {0})", ClassName);
                    WriteEndLine();

                    WriteLine("Public Sub New()");
                    WriteLine("\tMyBase.New()");
                    WriteLine("End Sub");
                    WriteEndLine();

                    WriteLine("Public Sub New(System.ComponentModel.IContainer container)");
                    WriteLine("\tMyBase.New(container)");
                    WriteLine("End Sub");
                    WriteEndLine();
                }

                TabLevel--;
                WriteLine("End Class");
                WriteEndLine();

                WriteLine("#End Region");
                WriteEndLine();
            }

            if (GenerateTypedAdapters)
            {
                WriteLine("#Region \"Typed Adapter\"");
                WriteEndLine();

                WriteLine("Public Class {0}Adapter", ClassName);
                TabLevel++;

                WriteLine("Inherits DCAdapterBase(Of {0})", ClassName);
                WriteEndLine();

                WriteLine("Public Sub New()");
                WriteLine("\tMyBase.New(ConnectionName.Core)");
                WriteLine("End Sub");
                WriteEndLine();

                TabLevel--;
                WriteLine("End Class");
                WriteEndLine();

                WriteLine("#End Region");
                WriteEndLine();
            }

            if (! String.IsNullOrEmpty(Namespace))
            {
                TabLevel--;
                WriteLine("End Namespace"); // end namespace
            }

            Output.Flush();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "local1"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "sType")]
        private static string GetNativeType(DataRow row)
        {
            bool allowNull = (bool)row["AllowDbNull"];

            string sType = row["DataType"].ToString();
            switch (sType.ToLower())
            {
                case "system.string":
                    return "String";
                case "system.byte[]":
                    return "System.Byte()";
                case "system.guid":
                    return "Guid";
                case "system.int32":
                    return (allowNull) ? "Nullable(Of Integer)" : "Integer";
                case "system.datetime":
                    return (allowNull) ? "Nullable(Of Date)" : "Date";
                case "system.decimal":
                    return (allowNull) ? "Nullable(Of Decimal)" : "Decimal";
                default:
                    return (allowNull) ? "Nullable(Of " + sType + ")" : sType;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "stack0")]
        private static string GetDbType(DataRow row)
        {
            string sType = row["DataType"].ToString();
            sType = sType.Substring(sType.IndexOf('.') + 1);

            if ((sType == "String") && (row.Table.Columns.Contains("DataTypeName")))
            {
                switch (row["DataTypeName"].ToString().ToLower())
                {
                    case "char":
                    case "nchar":
                        return "StringFixedLength";
                    default:
                        return "String";
                }
            }
            else if (sType == "Byte[]")
                return "Binary";
            else
                return sType;
        }
    }
}