#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using SqliteMultiTenant.Utilities;

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
private List<string> _groupBy;
private string _havingClause;
        private int? _limit;
        private int? _offset;

        /// <summary>
        /// Initializes a new instance of the QueryBuilder class for the specified table.
        /// </summary>
        /// <param name="tableName">The name of the table to query.</param>
        /// <exception cref="ArgumentException">Thrown when tableName is empty or whitespace.</exception>
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
        _groupBy = new List<string>();
        }

        /// <summary>
        /// Appends a SELECT clause with the specified columns, or SELECT * if none are provided.
        /// </summary>
        /// <param name="columns">The columns to select.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder Select(params string[] columns)
        {
            if (columns.Length > 0)
                _columns.AddRange(columns);

            return this;
        }

        /// <summary>
        /// Appends a WHERE clause with the specified condition and parameters.
        /// </summary>
        /// <param name="condition">The condition string (e.g., "Age > @age").</param>
        /// <param name="parameters">The parameters for the condition.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder Where(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _whereClause = condition;

            if (parameters.Length > 0)
                _parameters.AddRange(parameters);

            return this;
        }

        /// <summary>
        /// Appends an AND condition to the existing WHERE clause.
        /// </summary>
        /// <param name="condition">The condition string to append with AND.</param>
        /// <param name="parameters">The parameters for the condition.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
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

        /// <summary>
        /// Appends a WHERE IN condition for the specified column and values.
        /// </summary>
        /// <param name="column">The column name to check.</param>
        /// <param name="values">The values to check for in the column.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder WhereIn(string column, IEnumerable<object> values)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column cannot be empty", nameof(column));

            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                throw new ArgumentException("Values collection cannot be empty", nameof(values));

            var paramNames = new List<string>();
            foreach (var value in valuesList)
            {
                var paramName = $"p_{column}_{_parameters.Count}";
                paramNames.Add(paramName);
                _parameters.Add((paramName, value ?? DBNull.Value));
            }

            var placeholders = string.Join(", ", paramNames.Select(p => $"@{p}"));
            _whereClause = !string.IsNullOrEmpty(_whereClause)
                ? "(" + _whereClause + ") AND ([" + column + "] IN (" + placeholders + "))"
                : "[" + column + "] IN (" + placeholders + ")";

            return this;
        }

        /// <summary>
        /// Appends an OR condition to the existing WHERE clause.
        /// </summary>
        /// <param name="condition">The condition string to append with OR.</param>
        /// <param name="parameters">The parameters for the condition.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
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

        /// <summary>
        /// Appends an INNER JOIN clause to the query.
        /// </summary>
        /// <param name="table">The table to join.</param>
        /// <param name="condition">The join condition (e.g., "Users.Id = Orders.UserId").</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder InnerJoin(string table, string condition)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Table and condition cannot be empty");

            _joins.Add($"INNER JOIN {table} ON {condition}");
            return this;
        }

        /// <summary>
        /// Appends a LEFT JOIN clause to the query.
        /// </summary>
        /// <param name="table">The table to join.</param>
        /// <param name="condition">The join condition (e.g., "Users.Id = Orders.UserId").</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder LeftJoin(string table, string condition)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Table and condition cannot be empty");

            _joins.Add($"LEFT JOIN {table} ON {condition}");
            return this;
        }

        /// <summary>
        /// Appends an ORDER BY clause for the specified column and direction.
        /// </summary>
        /// <param name="column">The column to order by.</param>
        /// <param name="direction">The direction (ASC or DESC). Default is ASC.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
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

        /// <summary>
        /// Appends a GROUP BY clause with the specified columns.
        /// </summary>
        /// <param name="columns">The columns to group by.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder GroupBy(params string[] columns)
        {
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("At least one column must be specified", nameof(columns));

            _groupBy.AddRange(columns);
            return this;
        }


        /// <summary>
        /// Appends a LIMIT clause to restrict the number of returned rows.
        /// </summary>
        /// <param name="limit">The maximum number of rows to return.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder Limit(int limit)
        {
            if (limit <= 0)
                throw new ArgumentException("Limit must be greater than 0", nameof(limit));

            _limit = limit;
            return this;
        }

        /// <summary>
        /// Appends a HAVING clause with the specified condition and parameters.
        /// </summary>
        /// <param name="condition">The condition string (e.g., "COUNT(*) > @count").</param>
        /// <param name="parameters">The parameters for the condition.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder Having(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _havingClause = condition;
            if (parameters.Length > 0)
                _parameters.AddRange(parameters);
            return this;
        }


        /// <summary>
        /// Appends an OFFSET clause to skip a specified number of rows.
        /// </summary>
        /// <param name="offset">The number of rows to skip.</param>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder Offset(int offset)
        {
            if (offset < 0)
                throw new ArgumentException("Offset cannot be negative", nameof(offset));

            _offset = offset;
            return this;
        }

        /// <summary>
        /// Appends all configured clauses to form the complete SQL query string.
        /// </summary>
        /// <returns>The final constructed SQL query string.</returns>
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

// GROUP BY clause
if (_groupBy.Count > 0)
{
    _query.Append(" GROUP BY ");
    for (int i = 0; i < _groupBy.Count; i++)
    {
        if (i > 0) _query.Append(", ");
        _query.Append('[').Append(_groupBy[i]).Append(']');
    }
}

// HAVING clause
if (!string.IsNullOrEmpty(_havingClause))
    _query.Append(" HAVING ").Append(_havingClause);

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

        /// <summary>
        /// Appends parameters to the provided SQLiteCommand.
        /// </summary>
        /// <param name="command">The SQLiteCommand to apply parameters to.</param>
        public void ApplyParameters(SQLiteCommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            foreach (var param in _parameters)
                command.Parameters.AddWithValue($"@{param.name}", param.value ?? DBNull.Value);
        }

        /// <summary>
        /// Clears all configured clauses and parameters to allow reuse.
        /// </summary>
        /// <returns>The current QueryBuilder instance for method chaining.</returns>
        public QueryBuilder Reset()
        {
            _query.Clear();
            _parameters.Clear();
            _columns.Clear();
            _whereClause = null;
            _joins.Clear();
            _orderBy.Clear();
        _groupBy.Clear();
        _havingClause = null;
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

        /// <summary>
        /// Initializes a new instance of the InsertBuilder class for the specified table.
        /// </summary>
        /// <param name="tableName">The name of the table to insert into.</param>
        /// <exception cref="ArgumentException">Thrown when tableName is empty or whitespace.</exception>
        public InsertBuilder(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _tableName = tableName;
            _values    = new Dictionary<string, object>();
        }

        /// <summary>
        /// Appends a column-value pair to the insert statement.
        /// </summary>
        /// <param name="column">The column name.</param>
        /// <param name="value">The value for the column.</param>
        /// <returns>The current InsertBuilder instance for method chaining.</returns>
        public InsertBuilder Value(string column, object value)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column cannot be empty", nameof(column));

            _values[column] = value ?? DBNull.Value;
            return this;
        }

        /// <summary>
        /// Appends an INSERT INTO clause with the specified columns and parameterized values.
        /// </summary>
        /// <returns>A tuple containing the SQL query and the parameters dictionary.</returns>
        public (string query, Dictionary<string, object> parameters) Build()
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("No values specified for insert");

            var sb = StringBuilderPool.Rent(128);
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

            string query = sb.ToString();
            StringBuilderPool.Return(sb);
            return (query, _values);
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

        /// <summary>
        /// Initializes a new instance of the UpdateBuilder class for the specified table.
        /// </summary>
        /// <param name="tableName">The name of the table to update.</param>
        /// <exception cref="ArgumentException">Thrown when tableName is empty or whitespace.</exception>
        public UpdateBuilder(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _tableName = tableName;
            _values    = new Dictionary<string, object>();
        }

        /// <summary>
        /// Appends a SET clause with the specified column-value pair.
        /// </summary>
        /// <param name="column">The column name.</param>
        /// <param name="value">The value for the column.</param>
        /// <returns>The current UpdateBuilder instance for method chaining.</returns>
        public UpdateBuilder Set(string column, object value)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column cannot be empty", nameof(column));

            _values[column] = value ?? DBNull.Value;
            return this;
        }

        /// <summary>
        /// Appends a WHERE clause to the update statement.
        /// </summary>
        /// <param name="condition">The condition string.</param>
        /// <returns>The current UpdateBuilder instance for method chaining.</returns>
        public UpdateBuilder Where(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _whereClause = condition;
            return this;
        }

        /// <summary>
        /// Appends an UPDATE clause with the specified SET values and WHERE condition.
        /// </summary>
        /// <returns>A tuple containing the SQL query and the parameters dictionary.</returns>
        public (string query, Dictionary<string, object> parameters) Build()
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("No values specified for update");

            if (string.IsNullOrEmpty(_whereClause))
                throw new InvalidOperationException("WHERE condition is required for safety");

            var sb = StringBuilderPool.Rent(128);
            sb.Append("UPDATE [").Append(_tableName).Append("] SET ");

            bool first = true;
            foreach (var col in _values.Keys)
            {
                if (!first) sb.Append(", ");
                sb.Append('[').Append(col).Append("] = @").Append(col);
                first = false;
            }

            sb.Append(" WHERE ").Append(_whereClause);

            string query = sb.ToString();
            StringBuilderPool.Return(sb);
            return (query, _values);
        }
    }
}
