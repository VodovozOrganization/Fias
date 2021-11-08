using Npgsql;
using System;

namespace Domain
{
    public class ConnectionFactory
    {
        private readonly NpgsqlConnectionStringBuilder _connectionStringBuilder;

        public ConnectionFactory(NpgsqlConnectionStringBuilder connectionStringBuilder)
        {
            _connectionStringBuilder = connectionStringBuilder ?? throw new ArgumentNullException(nameof(connectionStringBuilder));
        }

        public virtual NpgsqlConnection OpenConnection()
        {
            return new NpgsqlConnection(_connectionStringBuilder.ConnectionString);
        }
    }
}
