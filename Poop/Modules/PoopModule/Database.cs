using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Interface for poop database operations
/// </summary>
internal interface IPoopDatabase
{
    /// <summary>
    /// Gets the top players by poop count (players who placed the most poops)
    /// </summary>
    Task<PoopRecord[]> GetTopPoopersAsync(int limit = 10);

    /// <summary>
    /// Gets the top players by victim count (players who were pooped on the most)
    /// </summary>
    Task<PoopRecord[]> GetTopVictimsAsync(int limit = 10);

    /// <summary>
    /// Increments the poop count for a player
    /// </summary>
    Task IncrementPoopCountAsync(ulong steamId, string playerName);

    /// <summary>
    /// Increments the victim count for a player
    /// </summary>
    Task IncrementVictimCountAsync(ulong steamId, string playerName);

    /// <summary>
    /// Gets a player's poop statistics
    /// </summary>
    Task<PoopRecord?> GetPlayerStatsAsync(ulong steamId);

    /// <summary>
    /// Saves a player's poop color preference
    /// </summary>
    Task SaveColorPreferenceAsync(ulong steamId, PoopColorPreference preference);

    /// <summary>
    /// Loads a player's poop color preference
    /// </summary>
    Task<PoopColorPreference?> LoadColorPreferenceAsync(ulong steamId);
}

/// <summary>
/// MySQL implementation of poop database
/// </summary>
internal sealed class PoopDatabase : IPoopDatabase, IDisposable
{
    private readonly PoopModuleConfig _config;
    private readonly ILogger<PoopDatabase> _logger;
    private readonly string _connectionString;
    private readonly AsyncLocal<MySqlConnection?> _asyncConnection = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private MySqlConnection? _initConnection;

    public PoopDatabase(PoopModuleConfig config, ILogger<PoopDatabase> logger)
    {
        _config = config;
        _logger = logger;

        // Build connection string
        if (!string.IsNullOrEmpty(_config.DatabaseConnection))
        {
            _connectionString = _config.DatabaseConnection;
        }
        else
        {
            _connectionString = $"Server={_config.DatabaseHost};" +
                              $"Port={_config.DatabasePort};" +
                              $"Database={_config.DatabaseName};" +
                              $"Uid={_config.DatabaseUser};" +
                              $"Pwd={_config.DatabasePassword};" +
                              $"AllowUserVariables=True;" +
                              $"UseAffectedRows=False";
        }

        try
        {
            _logger.LogInformation("Initializing MySQL database connection to {host}:{port}/{database}",
                _config.DatabaseHost, _config.DatabasePort, _config.DatabaseName);

            // Initialize connection for setup
            _initConnection = new MySqlConnection(_connectionString);
            _initConnection.Open();

            _logger.LogInformation("Database connection established successfully");

            if (_config.DatabaseAutoMigrate)
            {
                _logger.LogInformation("Running database migrations...");
                InitializeDatabase();
                _logger.LogInformation("Database migrations completed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization error");
            throw;
        }
    }

    private void InitializeDatabase()
    {
        if (_initConnection?.State != ConnectionState.Open)
        {
            _logger.LogWarning("Cannot initialize database: connection is not open");
            return;
        }

        try
        {
            // Create poop_stats table
            _initConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS poop_stats (
                    steam_id BIGINT UNSIGNED PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    poop_count INT UNSIGNED DEFAULT 0,
                    victim_count INT UNSIGNED DEFAULT 0,
                    last_updated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    INDEX idx_poop_count (poop_count DESC),
                    INDEX idx_victim_count (victim_count DESC)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            ");

            _logger.LogInformation("Table 'poop_stats' created or verified");

            // Create poop_colors table
            _initConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS poop_colors (
                    steam_id BIGINT UNSIGNED PRIMARY KEY,
                    red TINYINT UNSIGNED NOT NULL,
                    green TINYINT UNSIGNED NOT NULL,
                    blue TINYINT UNSIGNED NOT NULL,
                    is_rainbow BOOLEAN DEFAULT FALSE,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            ");

            _logger.LogInformation("Table 'poop_colors' created or verified");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database tables");
            throw;
        }
    }

    private async Task<MySqlConnection> GetConnectionAsync()
    {
        // Get or create connection for current async context
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

    public async Task<PoopRecord[]> GetTopPoopersAsync(int limit = 10)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var results = await connection.QueryAsync<PoopRecord>(@"
                SELECT 
                    steam_id as SteamId,
                    name as Name,
                    poop_count as PoopCount,
                    victim_count as VictimCount,
                    last_updated as LastUpdated
                FROM poop_stats 
                WHERE poop_count > 0
                ORDER BY poop_count DESC, last_updated DESC
                LIMIT @Limit",
                new { Limit = limit });

            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top poopers");
            return Array.Empty<PoopRecord>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PoopRecord[]> GetTopVictimsAsync(int limit = 10)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var results = await connection.QueryAsync<PoopRecord>(@"
                SELECT 
                    steam_id as SteamId,
                    name as Name,
                    poop_count as PoopCount,
                    victim_count as VictimCount,
                    last_updated as LastUpdated
                FROM poop_stats 
                WHERE victim_count > 0
                ORDER BY victim_count DESC, last_updated DESC
                LIMIT @Limit",
                new { Limit = limit });

            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top victims");
            return Array.Empty<PoopRecord>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task IncrementPoopCountAsync(ulong steamId, string playerName)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            await connection.ExecuteAsync(@"
                INSERT INTO poop_stats (steam_id, name, poop_count, victim_count) 
                VALUES (@SteamId, @Name, 1, 0)
                ON DUPLICATE KEY UPDATE 
                    poop_count = poop_count + 1,
                    name = @Name,
                    last_updated = CURRENT_TIMESTAMP",
                new { SteamId = steamId, Name = playerName });

            if (_config.DebugMode)
            {
                _logger.LogDebug("Incremented poop count for {player} ({steamId})", playerName, steamId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing poop count for {steamId}", steamId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task IncrementVictimCountAsync(ulong steamId, string playerName)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            await connection.ExecuteAsync(@"
                INSERT INTO poop_stats (steam_id, name, poop_count, victim_count) 
                VALUES (@SteamId, @Name, 0, 1)
                ON DUPLICATE KEY UPDATE 
                    victim_count = victim_count + 1,
                    name = @Name,
                    last_updated = CURRENT_TIMESTAMP",
                new { SteamId = steamId, Name = playerName });

            if (_config.DebugMode)
            {
                _logger.LogDebug("Incremented victim count for {player} ({steamId})", playerName, steamId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing victim count for {steamId}", steamId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PoopRecord?> GetPlayerStatsAsync(ulong steamId)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var result = await connection.QueryFirstOrDefaultAsync<PoopRecord>(@"
                SELECT 
                    steam_id as SteamId,
                    name as Name,
                    poop_count as PoopCount,
                    victim_count as VictimCount,
                    last_updated as LastUpdated
                FROM poop_stats 
                WHERE steam_id = @SteamId",
                new { SteamId = steamId });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player stats for {steamId}", steamId);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveColorPreferenceAsync(ulong steamId, PoopColorPreference preference)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            await connection.ExecuteAsync(@"
                INSERT INTO poop_colors (steam_id, red, green, blue, is_rainbow) 
                VALUES (@SteamId, @Red, @Green, @Blue, @IsRainbow)
                ON DUPLICATE KEY UPDATE 
                    red = @Red,
                    green = @Green,
                    blue = @Blue,
                    is_rainbow = @IsRainbow,
                    updated_at = CURRENT_TIMESTAMP",
                new
                {
                    SteamId = steamId,
                    Red = preference.Red,
                    Green = preference.Green,
                    Blue = preference.Blue,
                    IsRainbow = preference.IsRainbow
                });

            if (_config.DebugMode)
            {
                _logger.LogDebug("Saved color preference for {steamId}: RGB({r},{g},{b}) Rainbow={rainbow}",
                    steamId, preference.Red, preference.Green, preference.Blue, preference.IsRainbow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving color preference for {steamId}", steamId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PoopColorPreference?> LoadColorPreferenceAsync(ulong steamId)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var result = await connection.QueryFirstOrDefaultAsync<PoopColorPreference>(@"
                SELECT 
                    red as Red,
                    green as Green,
                    blue as Blue,
                    is_rainbow as IsRainbow
                FROM poop_colors 
                WHERE steam_id = @SteamId",
                new { SteamId = steamId });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading color preference for {steamId}", steamId);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        // Close and dispose the initialization connection
        if (_initConnection != null)
        {
            if (_initConnection.State == ConnectionState.Open)
            {
                _initConnection.Close();
            }
            _initConnection.Dispose();
            _initConnection = null;
        }

        // Also dispose the AsyncLocal connection if it exists
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

        _logger.LogInformation("Database connections disposed");
    }
}
