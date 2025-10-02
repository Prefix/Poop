using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Prefix.Poop.Modules.PoopModule;
using PoopRecord = Prefix.Poop.Models.PoopRecord;

namespace Prefix.Poop.Database
{
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly PoopModuleConfig _config;
        private readonly ILogger<DatabaseContext> _logger;
        private readonly AsyncLocal<MySqlConnection?> _asyncConnection = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private MySqlConnection? _connection;

        public DatabaseContext(PoopModuleConfig config, ILogger<DatabaseContext> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = $"Server={config.DatabaseHost};Port={config.DatabasePort};Database={config.DatabaseName};Uid={config.DatabaseUser};Pwd={config.DatabasePassword};AllowUserVariables=True;UseAffectedRows=False";
            
            try
            {
                _logger.LogInformation("Initializing database connection to {host}:{port}/{database}", 
                    config.DatabaseHost, config.DatabasePort, config.DatabaseName);
                
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();
                
                InitializeDatabase();
                
                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization error");
            }
        }

        private void InitializeDatabase()
        {
            if (_connection?.State != ConnectionState.Open)
                return;
            
            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS PoopRecords (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    PooperName VARCHAR(255) NOT NULL,
                    PooperSteamId VARCHAR(64) NOT NULL,
                    VictimName VARCHAR(255) NOT NULL,
                    VictimSteamId VARCHAR(64) NOT NULL,
                    Timestamp DATETIME NOT NULL,
                    MapName VARCHAR(128) NOT NULL,
                    PositionX FLOAT NOT NULL,
                    PositionY FLOAT NOT NULL,
                    PositionZ FLOAT NOT NULL,
                    INDEX idx_pooper (PooperSteamId),
                    INDEX idx_victim (VictimSteamId),
                    INDEX idx_timestamp (Timestamp DESC)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            ");
            
            _logger.LogInformation("Table 'PoopRecords' created or verified");
        }

        private async Task<MySqlConnection> GetConnectionAsync()
        {
            if (_asyncConnection.Value == null)
            {
                var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                _asyncConnection.Value = connection;
            }
            else if (_asyncConnection.Value.State != ConnectionState.Open)
            {
                await _asyncConnection.Value.OpenAsync();
            }

            return _asyncConnection.Value;
        }

        public async Task InsertPoopRecordAsync(PoopRecord record)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                var connection = await GetConnectionAsync();
                await connection.ExecuteAsync(@"
                    INSERT INTO PoopRecords 
                    (PooperName, PooperSteamId, VictimName, VictimSteamId, Timestamp, MapName, PositionX, PositionY, PositionZ) 
                    VALUES 
                    (@PooperName, @PooperSteamId, @VictimName, @VictimSteamId, @Timestamp, @MapName, @PosX, @PosY, @PosZ)",
                    new { 
                        PooperName = record.PooperName, 
                        PooperSteamId = record.PooperSteamId, 
                        VictimName = record.VictimName, 
                        VictimSteamId = record.VictimSteamId, 
                        Timestamp = record.Timestamp,
                        MapName = record.MapName,
                        PosX = record.PosX,
                        PosY = record.PosY,
                        PosZ = record.PosZ
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting poop record");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public void InsertPoopRecord(PoopRecord record)
        {
            _ = InsertPoopRecordAsync(record);
        }

        public async Task<int> GetVictimPoopCountAsync(string steamId)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                var connection = await GetConnectionAsync();
                return await connection.QueryFirstOrDefaultAsync<int>(@"
                    SELECT COUNT(*) FROM PoopRecords WHERE VictimSteamId = @SteamId",
                    new { SteamId = steamId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting victim count for {steamId}", steamId);
                return 0;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<(string Name, int Count)[]> GetTopPoopersAsync(int limit = 10)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                var connection = await GetConnectionAsync();
                var results = await connection.QueryAsync<(string, int)>(@"
                    SELECT PooperName, COUNT(*) as Count 
                    FROM PoopRecords 
                    GROUP BY PooperSteamId, PooperName 
                    ORDER BY Count DESC 
                    LIMIT @Limit",
                    new { Limit = limit });
                
                return results.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top poopers");
                return Array.Empty<(string, int)>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<(string Name, int Count)[]> GetTopVictimsAsync(int limit = 10)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                var connection = await GetConnectionAsync();
                var results = await connection.QueryAsync<(string, int)>(@"
                    SELECT VictimName, COUNT(*) as Count 
                    FROM PoopRecords 
                    GROUP BY VictimSteamId, VictimName 
                    ORDER BY Count DESC 
                    LIMIT @Limit",
                    new { Limit = limit });
                
                return results.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top victims");
                return Array.Empty<(string, int)>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
                _connection.Dispose();
                _connection = null;
            }
            
            if (_asyncConnection.Value != null)
            {
                if (_asyncConnection.Value.State == ConnectionState.Open)
                {
                    _asyncConnection.Value.Close();
                }
                _asyncConnection.Value.Dispose();
                _asyncConnection.Value = null;
            }

            _semaphore.Dispose();
            
            _logger.LogInformation("Database context disposed");
        }
    }
}
