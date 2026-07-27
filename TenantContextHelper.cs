using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;

namespace TenantContextHelper
{
    public class TenantContextHelper
    {
        private readonly ConcurrentDictionary<string, SQLiteConnection> _tenantConnections = new();
        private readonly ReaderWriterLock _lock = new();
        private readonly int _maxConnections = 10;
        private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(30);

        public void AddConnection(string tenantId, SQLiteConnection connection)
        {
            if (_tenantConnections.TryGetValue(tenantId, out var existingConnection))
            {
                if (existingConnection != connection)
                {
                    _lock.AcquireWriterLock();
                    try
                    {
                        existingConnection.Dispose();
                        _tenantConnections[tenantId] = connection;
                    }
                    finally
                    {
                        _lock.ReleaseWriterLock();
                    }
                }
            }
            else
            {
                _lock.AcquireWriterLock();
                try
                {
                    if (_tenantConnections.Count >= _maxConnections)
                    {
                        // Evict the least recently used connection
                        var lruConnection = _tenantConnections.OrderBy(x => x.Value.LastUsed).First().Value;
                        lruConnection.Dispose();
                        _tenantConnections.Remove(lruConnection.TenantId);
                    }
                    _tenantConnections[tenantId] = connection;
                }
                finally
                {
                    _lock.ReleaseWriterLock();
                }
            }
        }

        public SQLiteConnection GetConnection(string tenantId)
        {
            if (_tenantConnections.TryGetValue(tenantId, out var connection))
            {
                return connection;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }
}