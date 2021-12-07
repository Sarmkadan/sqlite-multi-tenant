#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace SqliteMultiTenant.DataOperations
{
    /// <summary>
    /// Fluent SQL query builder for constructing parameterized SELECT statements.
    /// Supports WHERE, AND/OR conditions, INNER/LEFT JOIN, ORDER BY, LIMIT, and OFFSET clauses.
    /// Column names are automatically bracket-quoted for safety.
    /// </summary>
    /// <example>
    /// <code>
    /// var query = new QueryBuilder("Users")
    ///     .Select("Name", "Email")
    ///     .Where("IsActive = @active", ("active", true))
    ///     .OrderBy("Name")
    ///     .Limit(10)
    ///     .Build();
    /// </code>
    /// </example>
    public sealed class QueryBuilder {
        private readonly StringBuilder _query;
        private readonly List<(string name, object value)> _parameters;
        private string _tableName;
        private List<string> _columns;
        private string _whereClause;
        private List<string> _joins;
        private List<(string column, string direction)> _orderBy;
        private int? _limit;
        private int? _offset;

        public QueryBuilder(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _tableName  = tableName;
            _query      = new StringBuilder(256);
            _parameters = new List<(string, object)>();
            _columns    = new List<string>();
            _joins      = new List<string>();
            _orderBy    = new List<(string, string)>();
        }

        // Selects specific columns (if empty, selects all)
        public QueryBuilder Select(params string[] columns)
        {
            if (columns.Length > 0)
                _columns.AddRange(columns);

            return this;
        }

        // Adds a WHERE condition
        public QueryBuilder Where(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _whereClause = condition;

            if (parameters.Length > 0)
                _parameters.AddRange(parameters);

            return this;
        }

        // Adds AND condition to existing WHERE
        public QueryBuilder And(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _whereClause = !string.IsNullOrEmpty(_whereClause)
                ? $"({_whereClause}) AND ({condition})"
                : condition;

            if (parameters.Length > 0)
                _parameters.AddRange(parameters);

            return this;
        }

        // Adds OR condition to existing WHERE
        public QueryBuilder Or(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _whereClause = !string.IsNullOrEmpty(_whereClause)
                ? $"({_whereClause}) OR ({condition})"
                : condition;

            if (parameters.Length > 0)
                _parameters.AddRange(parameters);

            return this;
        }

        // Adds INNER JOIN clause
        public QueryBuilder InnerJoin(string table, string condition)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Table and condition cannot be empty");

            _joins.Add($"INNER JOIN {table} ON {condition}");
            return this;
        }

        // Adds LEFT JOIN clause
        public QueryBuilder LeftJoin(string table, string condition)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Table and condition cannot be empty");

            _joins.Add($"LEFT JOIN {table} ON {condition}");
            return this;
        }

        // Adds ORDER BY clause
        public QueryBuilder OrderBy(string column, string direction = "ASC")
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column cannot be empty", nameof(column));

            direction = direction?.ToUpper() ?? "ASC";
            if (direction != "ASC" && direction != "DESC")
                throw new ArgumentException("Direction must be ASC or DESC", nameof(direction));

            _orderBy.Add((column, direction));
            return this;
        }

        // Adds LIMIT clause
        public QueryBuilder Limit(int limit)
        {
            if (limit <= 0)
                throw new ArgumentException("Limit must be greater than 0", nameof(limit));

            _limit = limit;
            return this;
        }

        // Adds OFFSET clause
        public QueryBuilder Offset(int offset)
        {
            if (offset < 0)
                throw new ArgumentException("Offset cannot be negative", nameof(offset));

            _offset = offset;
            return this;
        }

        // Builds the final SQL query string
        public string Build()
        {
            _query.Clear();

            // SELECT clause — direct appends avoid string.Join + LINQ delegate allocation
            if (_columns.Count > 0)
            {
                _query.Append("SELECT ");
                for (int i = 0; i < _columns.Count; i++)
                {
                    if (i > 0) _query.Append(", ");
                    _query.Append('[').Append(_columns[i]).Append(']');
                }
            }
            else
            {
                _query.Append("SELECT *");
            }

            // FROM clause
            _query.Append(" FROM [").Append(_tableName).Append(']');

            // JOIN clauses
            foreach (var join in _joins)
                _query.Append(' ').Append(join);

            // WHERE clause
            if (!string.IsNullOrEmpty(_whereClause))
                _query.Append(" WHERE ").Append(_whereClause);

            // ORDER BY clause
            if (_orderBy.Count > 0)
            {
                _query.Append(" ORDER BY ");
                for (int i = 0; i < _orderBy.Count; i++)
                {
                    if (i > 0) _query.Append(", ");
                    _query.Append('[').Append(_orderBy[i].column).Append("] ").Append(_orderBy[i].direction);
                }
            }

            // LIMIT / OFFSET clauses
            if (_limit.HasValue)
                _query.Append(" LIMIT ").Append(_limit.Value);

            if (_offset.HasValue)
                _query.Append(" OFFSET ").Append(_offset.Value);

            return _query.ToString();
        }

        // Applies parameters to a command
        public void ApplyParameters(SQLiteCommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            foreach (var param in _parameters)
                command.Parameters.AddWithValue($"@{param.name}", param.value ?? DBNull.Value);
        }

        // Resets the builder for reuse
        public QueryBuilder Reset()
        {
            _query.Clear();
            _parameters.Clear();
            _columns.Clear();
            _whereClause = null;
            _joins.Clear();
            _orderBy.Clear();
            _limit  = null;
            _offset = null;

            return this;
        }

        public override string ToString() => Build();
    }

    /// <summary>
    /// Fluent builder for constructing parameterized INSERT statements.
    /// Values are automatically parameterized to prevent SQL injection.
    /// </summary>
    public sealed class InsertBuilder {
        private readonly string _tableName;
        private readonly Dictionary<string, object> _values;

        public InsertBuilder(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _tableName = tableName;
            _values    = new Dictionary<string, object>();
        }

        // Adds a column-value pair
        public InsertBuilder Value(string column, object value)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column cannot be empty", nameof(column));

            _values[column] = value ?? DBNull.Value;
            return this;
        }

        // Builds INSERT statement
        public (string query, Dictionary<string, object> parameters) Build()
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("No values specified for insert");

            var sb = new StringBuilder(128);
            sb.Append("INSERT INTO [").Append(_tableName).Append("] (");

            bool first = true;
            foreach (var col in _values.Keys)
            {
                if (!first) sb.Append(", ");
                sb.Append('[').Append(col).Append(']');
                first = false;
            }

            sb.Append(") VALUES (");
            first = true;
            foreach (var col in _values.Keys)
            {
                if (!first) sb.Append(", ");
                sb.Append('@').Append(col);
                first = false;
            }
            sb.Append(')');

            return (sb.ToString(), _values);
        }
    }

    /// <summary>
    /// Fluent builder for constructing parameterized UPDATE statements.
    /// Requires a WHERE clause for safety - will throw if no condition is specified.
    /// </summary>
    public sealed class UpdateBuilder {
        private readonly string _tableName;
        private readonly Dictionary<string, object> _values;
        private string _whereClause;

        public UpdateBuilder(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _tableName = tableName;
            _values    = new Dictionary<string, object>();
        }

        // Sets a column value
        public UpdateBuilder Set(string column, object value)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column cannot be empty", nameof(column));

            _values[column] = value ?? DBNull.Value;
            return this;
        }

        // Sets WHERE condition
        public UpdateBuilder Where(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _whereClause = condition;
            return this;
        }

        // Builds UPDATE statement
        public (string query, Dictionary<string, object> parameters) Build()
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("No values specified for update");

            if (string.IsNullOrEmpty(_whereClause))
                throw new InvalidOperationException("WHERE condition is required for safety");

            var sb = new StringBuilder(128);
            sb.Append("UPDATE [").Append(_tableName).Append("] SET ");

            bool first = true;
            foreach (var col in _values.Keys)
            {
                if (!first) sb.Append(", ");
                sb.Append('[').Append(col).Append("] = @").Append(col);
                first = false;
            }

            sb.Append(" WHERE ").Append(_whereClause);

            return (sb.ToString(), _values);
        }
    }
}
