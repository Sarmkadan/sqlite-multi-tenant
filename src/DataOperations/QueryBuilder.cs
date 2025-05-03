// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace SqliteMultiTenant.DataOperations
{
    // Fluent SQL query builder for type-safe database operations
    // Supports WHERE, ORDER BY, LIMIT, and JOIN clauses
    public class QueryBuilder
    {
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

            _tableName = tableName;
            _query = new StringBuilder();
            _parameters = new List<(string, object)>();
            _columns = new List<string>();
            _joins = new List<string>();
            _orderBy = new List<(string, string)>();
        }

        // Selects specific columns (if empty, selects all)
        public QueryBuilder Select(params string[] columns)
        {
            if (columns.Length > 0)
            {
                _columns.AddRange(columns);
            }

            return this;
        }

        // Adds a WHERE condition
        public QueryBuilder Where(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            _whereClause = condition;

            if (parameters.Length > 0)
            {
                _parameters.AddRange(parameters);
            }

            return this;
        }

        // Adds AND condition to existing WHERE
        public QueryBuilder And(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            if (!string.IsNullOrEmpty(_whereClause))
            {
                _whereClause = $"({_whereClause}) AND ({condition})";
            }
            else
            {
                _whereClause = condition;
            }

            if (parameters.Length > 0)
            {
                _parameters.AddRange(parameters);
            }

            return this;
        }

        // Adds OR condition to existing WHERE
        public QueryBuilder Or(string condition, params (string name, object value)[] parameters)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Condition cannot be empty", nameof(condition));

            if (!string.IsNullOrEmpty(_whereClause))
            {
                _whereClause = $"({_whereClause}) OR ({condition})";
            }
            else
            {
                _whereClause = condition;
            }

            if (parameters.Length > 0)
            {
                _parameters.AddRange(parameters);
            }

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
                throw new ArgumentException("Direction must be ASC or DESC");

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

            // SELECT clause
            if (_columns.Count > 0)
            {
                _query.Append("SELECT ");
                _query.Append(string.Join(", ", _columns.Select(c => $"[{c}]")));
            }
            else
            {
                _query.Append("SELECT *");
            }

            // FROM clause
            _query.Append($" FROM [{_tableName}]");

            // JOIN clauses
            foreach (var join in _joins)
            {
                _query.Append(" ");
                _query.Append(join);
            }

            // WHERE clause
            if (!string.IsNullOrEmpty(_whereClause))
            {
                _query.Append($" WHERE {_whereClause}");
            }

            // ORDER BY clause
            if (_orderBy.Count > 0)
            {
                _query.Append(" ORDER BY ");
                _query.Append(string.Join(", ",
                    _orderBy.Select(o => $"[{o.column}] {o.direction}")));
            }

            // LIMIT clause
            if (_limit.HasValue)
            {
                _query.Append($" LIMIT {_limit.Value}");
            }

            // OFFSET clause
            if (_offset.HasValue)
            {
                _query.Append($" OFFSET {_offset.Value}");
            }

            return _query.ToString();
        }

        // Applies parameters to a command
        public void ApplyParameters(SQLiteCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            foreach (var param in _parameters)
            {
                command.Parameters.AddWithValue($"@{param.name}", param.value ?? DBNull.Value);
            }
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
            _limit = null;
            _offset = null;

            return this;
        }

        public override string ToString() => Build();
    }

    // Helper builder for INSERT operations
    public class InsertBuilder
    {
        private readonly string _tableName;
        private readonly Dictionary<string, object> _values;

        public InsertBuilder(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _tableName = tableName;
            _values = new Dictionary<string, object>();
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

            var columns = string.Join(", ", _values.Keys.Select(c => $"[{c}]"));
            var paramList = string.Join(", ", _values.Keys.Select(c => $"@{c}"));
            var query = $"INSERT INTO [{_tableName}] ({columns}) VALUES ({paramList})";

            return (query, _values);
        }
    }

    // Helper builder for UPDATE operations
    public class UpdateBuilder
    {
        private readonly string _tableName;
        private readonly Dictionary<string, object> _values;
        private string _whereClause;

        public UpdateBuilder(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _tableName = tableName;
            _values = new Dictionary<string, object>();
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

            var setList = string.Join(", ", _values.Keys.Select(c => $"[{c}] = @{c}"));
            var query = $"UPDATE [{_tableName}] SET {setList} WHERE {_whereClause}";

            return (query, _values);
        }
    }
}
