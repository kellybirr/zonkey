using System;
using System.Data;
using System.Data.Common;

namespace ZonkeyCodeGen.CodeGen
{
    /// <summary>
    /// Provides methods to generate C# code for a <see cref="Zonkey.ObjectModel.DataClass"/> from a table in a database.
    /// </summary>
    public class Sql2CSGenerator : ClassGenerator
    {
        /// <summary>
        /// Writes the <see cref="Zonkey.ObjectModel.DataClass"/> C# code to a <see cref="System.IO.TextWriter"/> instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        public override void Generate()
        {
            DbCommand command = Connection.CreateCommand();
            command.CommandText = string.Format("SELECT * FROM {0} WHERE 0=1", TableName);

            DbDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly);
            DataTable dt = reader.GetSchemaTable();
            reader.Close();

            var ds = new DataSet("schema_set");
            ds.Tables.Add(dt);
            ds.WriteXml($"C:\\Temp\\Enforce_Schema\\{TableName}.xml");

            WriteLine("using System;");
            WriteLine("using System.Data;");

            if (GenerateCollections == GenerateCollectionMode.GenericCollection)
                WriteLine("using System.Collections.ObjectModel;");
            else if (GenerateCollections == GenerateCollectionMode.BindableCollection)
                WriteLine("using System.ComponentModel;");

            WriteLine("using Zonkey.ObjectModel;");
            WriteEndLine();

            if (! String.IsNullOrEmpty(Namespace))
            {
                WriteLine("namespace {0}", Namespace);
                WriteLine("{");
                TabLevel++;
            }

            //WriteLine("[DataItem(\"{0}\")]", TableName);
            if (string.IsNullOrEmpty(SchemaName))
                WriteLine("[DataItem(\"{0}\")]", TableName);
            else
                WriteLine("[DataItem(\"{0}\", SchemaName = \"{1}\")]", (TableName.Split('.'))[1], SchemaName);

            WriteLine("public class {0} : DataClass", ClassName);

            WriteLine("{");
            TabLevel++;

            if (PrivateFieldsAtTop)
            {
                WriteLine("#region Private Fields");
                WriteEndLine();
                foreach (DataRow row in dt.Rows)
                {
                    string propertyName = FormatPropertyName(row["ColumnName"].ToString(), ClassName);

                    WriteBeginLine();
                    Write("private {0} _", GetNativeType(row));
                    Write(propertyName.Substring(0, 1).ToLower());
                    Write(propertyName.Substring(1));
                    Write(";");
                    WriteEndLine();
                }
                WriteEndLine();
                WriteLine("#endregion");
                WriteEndLine();
            }

            string guidToInit = null;

            WriteLine("#region Data Columns (Properties)");
            WriteEndLine();
            foreach (DataRow row in dt.Rows)
            {
                string propertyName = FormatPropertyName(row["ColumnName"].ToString(), ClassName);

                string sDbType = GetDbType(row);
                string sNativeType = GetNativeType(row);
                string sPrivateName = "_"+propertyName.Substring(0, 1).ToLower() + propertyName.Substring(1);
                //bool isKeyField = String.Equals((string)row["ColumnName"], KeyFieldName, StringComparison.CurrentCultureIgnoreCase);
                var isKeyField = KeyFieldName.Contains((string)row["ColumnName"]);
                
                if (isKeyField && (sNativeType == "Guid"))
                    guidToInit = sPrivateName;

                WriteBeginLine();
                Write("[DataField(\"{0}\", DbType.{1}, ", row["ColumnName"], sDbType);
                Write(AllowDbNull(row) ? "true" : "false");
                if ((sDbType == "Binary" || sDbType.IndexOf("String") >= 0) && (int)row["ColumnSize"] > 0)
                    Write(", Length = {0}", row["ColumnSize"]);
                if (isKeyField) Write(", IsKeyField = true");
                if ((bool)row["IsAutoIncrement"]) Write(", IsAutoIncrement = true");
                if ((bool)row["IsRowVersion"]) Write(", IsRowVersion = true");

                string seqName = SequenceNameFunc?.Invoke(row["ColumnName"].ToString());
                if (!string.IsNullOrEmpty(seqName))
                    Write($", SequenceName = \"{seqName}\"");

                if (sDbType.StartsWith("Date"))
                {
                    DateTimeKind? kind = DateTimeKindFunc?.Invoke(TableName, row["ColumnName"].ToString());
                    if (kind.HasValue && kind != DateTimeKind.Unspecified)
                        Write($", DateTimeKind = DateTimeKind.{kind}");
                }

                Write(")]");
                WriteEndLine();

                WriteBeginLine();
                Write("public ");
                if (VirtualProperties) Write("virtual ");
                Write(sNativeType);
                Write(" ");
                Write(propertyName);
                WriteEndLine();

                WriteLine("{");
                TabLevel++;

                WriteBeginLine();
                Write("get => ");
                Write(sPrivateName);
                Write(";");
                WriteEndLine();

                WriteBeginLine();
                Write("set => SetFieldValue(ref ");
                Write(sPrivateName);
                Write(", value);");
                WriteEndLine();

                TabLevel--;
                WriteLine("}");

                if (! PrivateFieldsAtTop)
                {
                    WriteBeginLine();
                    Write("private ");
                    Write(sNativeType);
                    Write(" ");
                    Write(sPrivateName);
                    Write(";");
                    WriteEndLine();
                }
                WriteEndLine();
            }
            WriteLine("#endregion");
            WriteEndLine();

            WriteLine("#region Constructors");
            WriteEndLine();

            WriteLine("public {0}(bool addingNew) : base(addingNew)", ClassName);
            WriteLine("{");
            TabLevel++;

            WriteLine("if (addingNew)");
            WriteLine("{");            
            if (! string.IsNullOrEmpty(guidToInit))
            {
                TabLevel++;
                WriteBeginLine();
                Write(guidToInit);
                Write(" = Guid.NewGuid();");
                WriteEndLine();
                TabLevel--;
            }

            TabLevel++;
            foreach (string line in AddConstructorCode)
                WriteLine(line);    
            TabLevel--;                

            WriteLine("}");

            TabLevel--;
            WriteLine("}");
            WriteEndLine();

            WriteLine("[Obsolete(\"This default constructor is required by the DataClassAdapter, but should never be used directly in code.\", true)]");
            WriteLine("public {0}() : this(false)", ClassName);
            WriteLine("{ }");
            WriteEndLine();

            WriteLine("#endregion");
            WriteEndLine();

            TabLevel--;
            WriteLine("}"); // end class
            WriteEndLine();

            if (GenerateCollections > GenerateCollectionMode.None)
            {
                WriteLine("#region Typed Collection");
                WriteEndLine();

                if (GenerateCollections == GenerateCollectionMode.GenericCollection)
                {
                    WriteLine("public class {0}Collection : Collection<{0}>", ClassName);
                    WriteLine("{");
                    WriteEndLine();
                }
                else if (GenerateCollections == GenerateCollectionMode.DataClassCollection)
                {
                    WriteLine("public class {0}Collection : DataClassCollection<{0}>", ClassName);
                    WriteLine("{");
                    WriteEndLine();
                }
                else if (GenerateCollections == GenerateCollectionMode.BindableCollection)
                {
                    WriteLine("public class {0}Collection : BindableCollection<{0}>", ClassName);
                    WriteLine("{");
                    TabLevel++;
                    WriteLine("public {0}Collection() {{ }}", ClassName);
                    WriteEndLine();
                    WriteLine("public {0}Collection(IContainer container)", ClassName);
                    WriteLine("\t: base(container) { }");
                    TabLevel--;
                }

                WriteLine("}");
                WriteEndLine();

                WriteLine("#endregion");
                WriteEndLine();
            }

            if (GenerateTypedAdapters)
            {
                WriteLine("#region Typed Adapter");
                WriteEndLine();

                WriteLine("public class {0}Adapter : DCAdapterBase<{0}>", ClassName);
                WriteLine("{");

                TabLevel++;
                WriteLine("public {0}Adapter(): base(ConnectionName.Core) {{ }}", ClassName);
                TabLevel--;

                WriteEndLine();
                WriteLine("}");
                WriteEndLine();

                WriteLine("#endregion");
                WriteEndLine();
            }

            if (! String.IsNullOrEmpty(Namespace))
            {
                TabLevel--;
                WriteLine("}"); // end namespace
            }

            Output.Flush();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "sType"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "local1")]
        private string GetNativeType(DataRow row)
        {
            bool allowNull = AllowDbNull(row);

            string sType = row["DataType"].ToString();
            sType = sType.Substring(sType.IndexOf('.') + 1);

            switch (sType.ToLower())
            {
                case "string":
                    return (CsNullable) ? "string?" : "string";
                case "byte[]":
                    return "byte[]";
                case "decimal":
                {
                    string numType = "decimal";
                    if ((int)row["NumericScale"] == 0)
                        numType = ((int)row["NumericPrecision"] >= 10) ? "long" : "int";

                    return (allowNull) ? $"{numType}?" : numType;
                }
                case "boolean":
                    return "bool";
                default:
                    return (allowNull) ? sType + "?" : sType;
            }
        }

        private bool AllowDbNull(DataRow row)
        {
            return NullableCheck?.Invoke(TableName, (string)row["ColumnName"]) 
                   ?? (bool)row["AllowDbNull"];
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

            if (sType == "DateTime")
            {
                switch (row["DataTypeName"].ToString().ToLower())
                {
                    case "timestamp without time zone":
                        return "DateTime2";
                    default:
                        return "DateTime";
                }
            }

            if (sType == "Byte[]")
                return "Binary";
            
            return sType;
        }
    }
}
