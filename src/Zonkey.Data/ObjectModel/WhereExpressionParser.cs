using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Zonkey.Dialects;

namespace Zonkey.ObjectModel
{
    class WhereExpressionParser<T> : WhereExpressionParser
    {
        public WhereExpressionParser()
        { }

        public WhereExpressionParser(DataMap map) : this(map, null)
        { }

        public WhereExpressionParser(DataMap map, SqlDialect dialect) : base(new[] { map })
        {
            SqlDialect = dialect;
        }

        public SqlWhereClause Parse(Expression<Func<T, bool>> expression)
        {
            return base.Parse(expression);
        }
    }

    class WhereExpressionParser
    {        
        private ArrayList _parmList;
        private List<string> _boolFields;

        private Dictionary<string, DataMap> _dataMaps;
        private readonly IEnumerable<DataMap> _mapHints;

        public bool? UseQuotedIdentifier { get; set; }

        public SqlDialect SqlDialect { get; set; }

        public bool ParameterizeLiterals { get; set; }

        public char ParameterPrefix { get; set; }

        public bool? UseTableWithFieldNames { get; set; }

        public int ParameterIndexModifier { get; set; }

        public bool AnsiNullCompensation { get; set; }

        public bool NoLock { get; set; }

        public WhereExpressionParser()
        {
            _mapHints = new DataMap[0];

            AnsiNullCompensation = true;
            ParameterizeLiterals = true;
            ParameterPrefix = '$';
            NoLock = false;
        }

        public WhereExpressionParser(IEnumerable<DataMap> maps)
        {
            _mapHints = maps;

            AnsiNullCompensation = true;
            ParameterizeLiterals = true;
            ParameterPrefix = '$';
        }

        public SqlWhereClause Parse(LambdaExpression expression)
        {
            return Parse(expression, new ArrayList());
        }

        internal SqlWhereClause Parse(LambdaExpression expression, ArrayList paramList)
        {
            _parmList = paramList;
            _dataMaps = new Dictionary<string, DataMap>();
            _boolFields = new List<string>();

            foreach (var p in expression.Parameters)
            {
                var got = false;
                foreach (DataMap hint in _mapHints)
                {
                    if (hint.ObjectType != p.Type) continue;

                    _dataMaps.Add(p.Name, hint);
                    got = true;
                    break;
                }

                if (! got) _dataMaps.Add(p.Name, DataMap.GenerateCached(p.Type));
            }

            string sql = ParseExpression(expression.Body);

            // fix unary bools
            if ((_boolFields.Count == 1) && (sql == _boolFields[0]))
            {
                sql = string.Format("({0} = 1)", _boolFields[0]);
            }
            else
            {
                foreach (string f in _boolFields)
                {
                    sql = sql.Replace(string.Format("({0})", f), string.Format("({0} = 1)", f));
                }
            }

            // ANSI NULL Compensation
            if (AnsiNullCompensation)
            {
                for (int p = _parmList.Count - 1; p >= 0; p--)
                {
                    if (_parmList[p] == null)
                    {
                        string origSql = sql;
                        sql = sql.Replace("!= $" + p + ")", "IS NOT NULL)");
                        sql = sql.Replace("= $" + p + ")", "IS NULL)");

                        if (sql != origSql)
                        {
                            for (int q = p + 1; q < _parmList.Count; q++)
                                sql = sql.Replace("$" + q, "$" + (q - 1));

                            _parmList.RemoveAt(p);
                        }
                    }
                }   
            }

            // return
            return new SqlWhereClause
                       {
                           SqlText = sql, 
                           Parameters = _parmList.ToArray()
                       };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private string ParseExpression(Expression op)
        {
            switch (op.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return ParseAndOrExpression(op as BinaryExpression, " AND ");
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return ParseAndOrExpression(op as BinaryExpression, " OR ");
                case ExpressionType.Equal:
                    return ParseBinaryExpression(op as BinaryExpression, " = ");
                case ExpressionType.NotEqual:
                    return ParseBinaryExpression(op as BinaryExpression, " != ");
                case ExpressionType.LessThan:
                    return ParseBinaryExpression(op as BinaryExpression, " < ");
                case ExpressionType.LessThanOrEqual:
                    return ParseBinaryExpression(op as BinaryExpression, " <= ");
                case ExpressionType.GreaterThan:
                    return ParseBinaryExpression(op as BinaryExpression, " > ");
                case ExpressionType.GreaterThanOrEqual:
                    return ParseBinaryExpression(op as BinaryExpression, " >= ");
                /*case ExpressionType.ExclusiveOr:*/
                case ExpressionType.Add:
                    return ParseBinaryExpression(op as BinaryExpression, " + ");
                case ExpressionType.Subtract:
                    return ParseBinaryExpression(op as BinaryExpression, " - ");
                case ExpressionType.Multiply:
                    return ParseBinaryExpression(op as BinaryExpression, " * ");
                case ExpressionType.Divide:
                    return ParseBinaryExpression(op as BinaryExpression, @" / ");
                case ExpressionType.Modulo:
                    return ParseBinaryExpression(op as BinaryExpression, " % ");
                case ExpressionType.Not:
                    return ParseUnaryExpression(op as UnaryExpression, " NOT ");
                /*case ExpressionType.Coalesce:*/
                case ExpressionType.Convert:
                    return ParseConvertExpression(op as UnaryExpression);
                //case ExpressionType.Lambda:
                //    GetLambda(op as LambdaExpression);
                //    break;
                //case ExpressionType.New:
                //    GetNew(op as NewExpression);
                //    break;
                case ExpressionType.MemberAccess:
                    return ParseMemberExpression(op as MemberExpression);
                case ExpressionType.Parameter:
                    return ParseParameterExpression(op as ParameterExpression);
                case ExpressionType.Constant:
                    return ParseConstantExpression(op as ConstantExpression);
                case ExpressionType.Call:
                    return ParseMethodCallExpression(op as MethodCallExpression);
                default:
                    throw new NotSupportedException(string.Format("The operator '{0}' is not supported", op.NodeType));
            }
        }

        private string ParseAndOrExpression(BinaryExpression op, string operatorSymbol)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            if (NeedsExtraParens(op.Left))
            {
                sb.Append('(');
                sb.Append(ParseExpression(op.Left));    
                sb.Append(')');                
            }            
            else
                sb.Append(ParseExpression(op.Left));    

            sb.Append(operatorSymbol);

            if (NeedsExtraParens(op.Right))
            {
                sb.Append('(');
                sb.Append(ParseExpression(op.Right));
                sb.Append(')');
            }            
            else
                sb.Append(ParseExpression(op.Right));

            sb.Append(')');                
            return sb.ToString();
        }

        private string ParseBinaryExpression(BinaryExpression op, string operatorSymbol)
        {
            var sb = new StringBuilder();
            sb.Append('(');
            sb.Append(ParseExpression(op.Left));

            // deal with is/not null
            string sRight = op.Right.ToString();
            if ( (sRight == "null") || (sRight == "Convert(null)") )
            {
                if (op.NodeType == ExpressionType.Equal)
                {
                    sb.Append(" IS NULL)");
                    return sb.ToString();
                }
                
                if (op.NodeType == ExpressionType.NotEqual)
                {
                    sb.Append(" IS NOT NULL)");
                    return sb.ToString();
                }                
            }

            sb.Append(operatorSymbol);
            sb.Append(ParseExpression(op.Right));
            sb.Append(')');

            return sb.ToString();
        }

        private string ParseUnaryExpression(UnaryExpression op, string operatorSymbol)
        {
            var sb = new StringBuilder();
            sb.Append('(');
            sb.Append(operatorSymbol);
            sb.Append('(');
            sb.Append(ParseExpression(op.Operand));
            sb.Append("))");

            return sb.ToString();
        }

        private string ParseConstantExpression(ConstantExpression op)
        {
            if (ParameterizeLiterals)
            {
                _parmList.Add(op.Value);
                return CurrentParameterLabel();
            }
            
            if (op.Type == typeof(string))
                return string.Format("'{0}'", op.Value);
            if ( (op.Type == typeof(Guid)) || (op.Type == typeof(Guid?)) )
                return FormatGuidLiteral(op.Value);
            
            return op.Value.ToString();
        }

        private string ParseMemberExpression(MemberExpression op)
        {
            if (op.Expression != null)
            {
                switch (op.Expression.NodeType)
                {
                    case ExpressionType.Parameter:
                        {
                            var pex = (ParameterExpression) op.Expression;
                            var map = _dataMaps[pex.Name];

                            return GetPropertyFieldName(op, map);
                        }
                    case ExpressionType.Constant:
                        _parmList.Add(GetMemberData(op, null));
                        return CurrentParameterLabel();
                    case ExpressionType.MemberAccess:
                        if ((op.Member.Name == "HasValue") && (op.Member.DeclaringType.Name == "Nullable`1"))
                        {
                            var mex = (MemberExpression) op.Expression;
                            var pex = (ParameterExpression) (mex.Expression);
                            var map = _dataMaps[pex.Name];

                            return GetPropertyFieldName(mex, map) + " IS NULL";
                        }

                        _parmList.Add(GetMemberData(op.Expression as MemberExpression, op.Member));
                        return CurrentParameterLabel();
                    default:
                        throw new NotSupportedException();
                }
            }

            // static class members happen here
            switch (op.NodeType)
            {
                case ExpressionType.MemberAccess:
                    _parmList.Add(GetMemberData(op, op.Member));
                    return CurrentParameterLabel();
                    default:
                        throw new NotSupportedException();
            }
        }

        private string GetPropertyFieldName(MemberExpression op, DataMap map)
        {
            IDataMapField field = map.GetFieldForProperty((PropertyInfo) op.Member);

            string fieldName = (SqlDialect != null) 
                ? SqlDialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier))
                : field.FieldName;

            if (UseTableWithFieldNames ?? (_dataMaps.Count > 1))
            {
                string tableName = (SqlDialect != null)
                    ? SqlDialect.FormatTableName(map.DataItem.TableName, map.DataItem.SchemaName, (map.DataItem.UseQuotedIdentifier ?? UseQuotedIdentifier))
                    : map.DataItem.TableName;

                fieldName = tableName + "." + fieldName;
            }

            // store bool fields for fixing later
            if ( (field.Property.PropertyType == typeof(bool)) && (! _boolFields.Contains(fieldName)) )
                _boolFields.Add(fieldName);

            return fieldName;
        }

        private string ParseConvertExpression(UnaryExpression op)
        {
            if (op.Operand is MemberExpression)
                return ParseMemberExpression((MemberExpression)op.Operand);
            
            if (op.Operand is UnaryExpression)
                return ParseConvertExpression((UnaryExpression) op.Operand);
 
            if (op.Operand is ConstantExpression)
                return ParseConstantExpression((ConstantExpression)op.Operand);

            throw new NotSupportedException(string.Format("Convert Operator with operand of '{0}' is not supported", op.Operand.GetType().Name));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private static object GetMemberData(MemberExpression op, MemberInfo m2)
        {
            object obj = null;
            object value = null;

            if (op.Expression != null)
            {
                if (op.Expression.NodeType == ExpressionType.MemberAccess)
                    obj = GetMemberData(op.Expression as MemberExpression, op.Member);
                else
                {
                    obj = ((ConstantExpression)op.Expression).Value;

                    if (op.Member is FieldInfo)
                        value = ((FieldInfo)op.Member).GetValue(obj);
                    else if (op.Member is PropertyInfo)
                        value = ((PropertyInfo)op.Member).GetValue(obj, null);
                    else
                        throw new NotSupportedException();
                }
            }
            else if ((op.NodeType == ExpressionType.MemberAccess) && (op.Member != m2))
                obj = GetMemberData(op, op.Member);

            if (m2 is FieldInfo)
                value = ((FieldInfo)m2).GetValue(value ?? obj);
            else if (m2 is PropertyInfo)
                value = ((PropertyInfo)m2).GetValue(value ?? obj, null);

            return value;
        }

        private static string ParseParameterExpression(ParameterExpression op)
        {
            return op.ToString();
        }

        private string ParseMethodCallExpression(MethodCallExpression op)
        {
            if (op.Method.Name == "SqlIn")
                return ParseSqlInExpression(op);
            if (op.Method.Name == "SqlInInt")
                return ParseSqlInExpression2(op);
            if (op.Method.Name == "SqlInGuid")
                return ParseSqlInExpression2(op);

            if (op.Method.DeclaringType == typeof(string))
            {
                var sb = new StringBuilder("(");
                switch (op.Method.Name)
                {
                    case "StartsWith":
                        sb.Append(ParseExpression(op.Object));
                        sb.Append(" Like (");
                        sb.Append(ParseExpression(op.Arguments[0]));
                        sb.Append("+'%')");
                        break;
                    case "EndsWith":
                        sb.Append(ParseExpression(op.Object));
                        sb.Append(" Like ('%'+");
                        sb.Append(ParseExpression(op.Arguments[0]));
                        sb.Append(")");
                        break;
                    case "Contains":
                        sb.Append(ParseExpression(op.Object));
                        sb.Append(" Like ('%'+");
                        sb.Append(ParseExpression(op.Arguments[0]));
                        sb.Append("+'%')");
                        break;
                    default:
                        throw new NotSupportedException();
                }

                sb.Append(')');
                return sb.ToString();
            }
                        
            throw new NotSupportedException();
        }

        private string ParseSqlInExpression(MethodCallExpression op)
        {
            var sb = new StringBuilder("(");
            sb.Append(ParseExpression(op.Arguments[0]));
            sb.Append(" IN (");

            if (op.Arguments.Count == 3)
            {
                var parser = new WhereExpressionParser { SqlDialect = SqlDialect, NoLock = NoLock };
                
                string selectField = parser.Parse((LambdaExpression)op.Arguments[1]).SqlText;
                string whereClause = parser.Parse((LambdaExpression)op.Arguments[2], _parmList).SqlText;
                                
                string tableParamName = ((LambdaExpression)op.Arguments[1]).Parameters[0].Name;
                IDataMapItem mapItem = parser._dataMaps[tableParamName].DataItem;
                string tableName = (SqlDialect != null)
                    ? SqlDialect.FormatTableName(mapItem.TableName, mapItem.SchemaName, (mapItem.UseQuotedIdentifier ?? UseQuotedIdentifier))
                    : mapItem.TableName;

                if (NoLock && SqlDialect.SupportsNoLock)
                    tableName += " WITH (NOLOCK)";

                sb.AppendFormat("SELECT {0} FROM {1} WHERE {2}", selectField, tableName, whereClause);
            }
            else if (op.Arguments.Count != 2)
                throw new NotSupportedException();
            else if (op.Arguments[1].NodeType == ExpressionType.Lambda)
            {
                var parser = new WhereExpressionParser { SqlDialect = SqlDialect, NoLock = NoLock };

                string rawSelectField = ((MemberExpression) op.Arguments[0]).Member.Name;
                string selectField = (SqlDialect != null)
                    ? SqlDialect.FormatFieldName(rawSelectField, UseQuotedIdentifier)
                    : rawSelectField;

                string whereClause = parser.Parse((LambdaExpression)op.Arguments[1], _parmList).SqlText;

                string tableParamName = ((LambdaExpression)op.Arguments[1]).Parameters[0].Name;
                IDataMapItem mapItem = parser._dataMaps[tableParamName].DataItem;
                string tableName = (SqlDialect != null)
                    ? SqlDialect.FormatTableName(mapItem.TableName, mapItem.SchemaName, (mapItem.UseQuotedIdentifier ?? UseQuotedIdentifier))
                    : mapItem.TableName;

                if (NoLock && SqlDialect.SupportsNoLock)
                    tableName += " WITH (NOLOCK)";

                // verify select field
                if (! parser._dataMaps[tableParamName].ContainsField(rawSelectField))
                    throw new ArgumentOutOfRangeException("fieldExpression", string.Format("Field `{0}` not found on object `{1}`", rawSelectField, mapItem.TableName));

                sb.AppendFormat("SELECT {0} FROM {1} WHERE {2}", selectField, tableName, whereClause);
            }
            else if (op.Arguments[1].NodeType == ExpressionType.MemberAccess)
            {
                int itemCount = 0;
                var valList = (IEnumerable)GetMemberData((MemberExpression)op.Arguments[1], null);
                foreach (object oVal in valList)
                {
                    if (oVal == null) continue;

                    if (itemCount > 0)
                        sb.Append(',');

                    _parmList.Add(oVal);
                    sb.Append(CurrentParameterLabel());
                    itemCount++;
                }

                if (itemCount == 0)
                    throw new ArgumentException("Attempted to call SqlIn(IEnumerable) where IEnumerable contained zero values");
                if (itemCount > 2100)
                    throw new ArgumentException("Attempted to call SqlIn(IEnumerable) where IEnumerable contained more than 2100 values, this is not supported by SQL.  Consider using SqlInInt or SqlInGuid or split the list first with SplitList() and do a looping fill");
            }
            else if (op.Arguments[1].NodeType == ExpressionType.Call)
                throw new NotSupportedException("Method calls in SqlIn() are not yet supported.  Get your Array/IEnumerable as a variable first, then pass the variable to SqlIn().");

            sb.Append("))");
            return sb.ToString();
        }

        private string ParseSqlInExpression2(MethodCallExpression op)
        {
            var sb = new StringBuilder("(");
            sb.Append(ParseExpression(op.Arguments[0]));
            sb.Append(" IN (");

            if (op.Arguments.Count != 2)
                throw new NotSupportedException();

            if (op.Arguments[1].NodeType != ExpressionType.MemberAccess)
                throw new NotSupportedException();

            bool wroteOneItem = false;
            var valList = (IEnumerable)GetMemberData((MemberExpression)op.Arguments[1], null);
            foreach (object oVal in valList)
            {
                if (oVal == null)
                    continue;

                if (wroteOneItem)
                    sb.Append(',');
                else
                    wroteOneItem = true;

                string safeValue = GetSafeLiteral(oVal);
                if (safeValue != null)
                {
                    sb.Append(safeValue);
                }
                else
                {
                    _parmList.Add(oVal);
                    sb.Append(CurrentParameterLabel());
                }
            }

            if (!wroteOneItem)
                throw new ArgumentException("Attempted to call SqlInInt/SqlInGuid(IEnumerable) where IEnumerable contained zero values");

            sb.Append("))");
            return sb.ToString();
        }


        private string GetSafeLiteral(object val)
        {
            if (val == null) return null;

            if ((val is Byte) || (val is Byte?))
                return val.ToString();
            if ((val is Int16) || (val is Int16?))
                return val.ToString();
            if ((val is Int32) || (val is Int32?))
                return val.ToString();
            if ((val is Int64) || (val is Int64?))
                return val.ToString();
            if ((val is Guid) || (val is Guid?))
                return FormatGuidLiteral(val);

            return null;
        }

        private string FormatGuidLiteral(object guid)
        {
            return (SqlDialect != null)
                       ? SqlDialect.FormatGuidLiteral((Guid?) guid)
                       : (guid == null) ? "NULL" : string.Format("'{0}'", guid);
        }

        private string CurrentParameterLabel()
        {
            return ParameterPrefix + (_parmList.Count + ParameterIndexModifier - 1).ToString(CultureInfo.InvariantCulture);
        }

        private static bool NeedsExtraParens(Expression op)
        {
            switch (op.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Call:
                case ExpressionType.Not:
                    return false;
                default:
                    return true;
            }
        }
    }

    class SqlWhereClause
    {
        public string SqlText { get; set; }
        public object[] Parameters { get; set; }
    }
}
