using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Npgsql;
using ZonkeyCodeGen.CodeGen;

namespace NpgCodeGen
{
    internal class Program
    {
        const string TableClassPrefix = "";
        const string OutputNamespace = "";
        const string OutputFolder = "";
        const string ConnStr = "";

        public static string[] IgnoreTables = Array.Empty<string>();

        public static Dictionary<string,string[]> IgnoreFieldSet = new Dictionary<string, string[]>()
        {
            { "table_1", new[] {"depricated_field_1"} }
        };

        static void Main(string[] args)
        {
            using (var cnxn = new NpgsqlConnection(ConnStr))
            {
                // connect
                cnxn.Open();

                // read tables
                var tables = new DataTable("tables");
                var tablesCommand = new NpgsqlCommand("SELECT * FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'", cnxn);
                using (NpgsqlDataReader tableReader = tablesCommand.ExecuteReader(CommandBehavior.SingleResult))
                    tables.Load(tableReader);

                // read primary keys
                var keys = new DataTable("key_column_usage");
                var keysCommand = new NpgsqlCommand("SELECT * FROM information_schema.key_column_usage WHERE table_schema = 'public'", cnxn);
                using (NpgsqlDataReader keysReader = keysCommand.ExecuteReader(CommandBehavior.SingleResult))
                    keys.Load(keysReader);

                // read column information
                var columns = new DataTable("columns");
                var colsCommand = new NpgsqlCommand("SELECT * FROM information_schema.columns WHERE table_schema = 'public'", cnxn);
                using (NpgsqlDataReader colsReader = colsCommand.ExecuteReader(CommandBehavior.SingleResult))
                    columns.Load(colsReader);

                // read sequence information
                var sequences = new DataTable("sequences");
                var seqCommand = new NpgsqlCommand("SELECT * FROM information_schema.sequences WHERE sequence_schema = 'public'", cnxn);
                using (NpgsqlDataReader seqReader = seqCommand.ExecuteReader(CommandBehavior.SingleResult))
                    sequences.Load(seqReader);

                // helper to get key fields
                List<string> GetKeyFields(string tableName)
                {
                    return (
                        from DataRow r in keys.Rows 
                        where tableName == (string)r["table_name"] && r.IsNull("position_in_unique_constraint")
                        select r["column_name"].ToString()
                        ).ToList();
                }

                // helper to check for null
                bool IsNullable(string tableName, string columnName)
                {
                    return (
                        from DataRow r in columns.Rows
                        where tableName == (string)r["table_name"]
                              && columnName == (string)r["column_name"]
                        select (string)r["is_nullable"] == "YES"
                    ).FirstOrDefault();
                }

                // helper for datetime kind
                DateTimeKind GetDateTimeKind(string tableName, string columnName)
                {
                    return (
                        from DataRow r in columns.Rows
                        where tableName == (string)r["table_name"]
                              && columnName == (string)r["column_name"]
                        select (string)r["data_type"] == "timestamp with time zone" ? DateTimeKind.Utc : DateTimeKind.Unspecified
                    ).FirstOrDefault();
                }

                // helper to find sequence
                string GetSequenceName(string columnName)
                {
                    return (
                        from DataRow r in sequences.Rows
                        where $"{columnName}_seq" == (string)r["sequence_name"]
                        select (string)r["sequence_name"]
                    ).FirstOrDefault();
                }

                // build class generator
                var gen = new Sql2CSGenerator
                {
                    Connection = cnxn,
                    CsNullable = true,
                    DateTimeKindFunc = GetDateTimeKind,
                    FormatPropertyName = FormatPropertyName,
                    GenerateCollections = GenerateCollectionMode.None,
                    GenerateTypedAdapters = false,
                    Namespace = OutputNamespace,
                    NullableCheck = IsNullable,
                    PrivateFieldsAtTop = false,
                    //SequenceNameFunc = GetSequenceName,
                    VirtualProperties = false,
                    PartialClasses = true
                };


                // loop tables and generate
                foreach (DataRow table in tables.Rows)
                {
                    gen.TableName = table["table_name"].ToString();
                    if (IgnoreTables.Contains(gen.TableName, StringComparer.OrdinalIgnoreCase)) continue;

                    gen.ClassName = TableClassPrefix + WormCase2PascalCase(MakeSingular(gen.TableName));
                    gen.KeyFieldName = GetKeyFields(gen.TableName);

                    gen.IgnoreFields.Clear();
                    if (IgnoreFieldSet.TryGetValue(gen.TableName, out string[] fields))
                        gen.IgnoreFields.AddRange(fields);

                    string filePath = Path.Combine(OutputFolder, gen.ClassName + ".cs");
                    gen.Output = new StreamWriter(filePath, false, Encoding.ASCII);

                    gen.Generate();
                    gen.Output.Close();

                    Console.WriteLine("Generated: " + gen.ClassName);
                }
            }
        }
        private static string FormatPropertyName(string fieldName, string className)
        {
            string n = WormCase2PascalCase(fieldName);
            string prefix = className.Substring(TableClassPrefix.Length);
            return (n != prefix && n.StartsWith(prefix)) ? n.Substring(prefix.Length) : n;
        }

        private static string WormCase2PascalCase(string worm)
        {
            bool capNext = true;
            var sb = new StringBuilder();
            foreach (char c in worm)
            {
                if (c == '_' || c == ' ')
                {
                    capNext = true;
                }
                else if (capNext)
                {
                    sb.Append(c.ToString().ToUpper());
                    capNext = false;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static string MakeSingular(string s)
        {
            if (s.EndsWith("ies"))
                return $"{s.Substring(0, s.Length - 3)}y";

            return (s.EndsWith("s") && !s.EndsWith("ss")) ? s.TrimEnd('s') : s;
        }
    }
}
