using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Prefix.Poop.Interfaces.Database;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Shared.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// MySQL implementation of poop database
/// </summary>
internal sealed class PoopDatabase : IPoopDatabase, IDisposable
{
    private readonly IConfigManager _config;
    private readonly ILogger<PoopDatabase> _logger;
    private readonly string _connectionString;
    private readonly AsyncLocal<MySqlConnection?> _asyncConnection = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private MySqlConnection? _initConnection;

    public PoopDatabase(IConfigManager config, ILogger<PoopDatabase> logger)
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
            // Create poop_colors table
            _initConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS poop_colors (
                    steam_id BIGINT UNSIGNED PRIMARY KEY,
                    red TINYINT UNSIGNED NOT NULL,
                    green TINYINT UNSIGNED NOT NULL,
                    blue TINYINT UNSIGNED NOT NULL,
                    is_rainbow BOOLEAN DEFAULT FALSE,
                    is_random BOOLEAN DEFAULT FALSE,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            ");

            _logger.LogInformation("Table 'poop_colors' created or verified");

            // Add is_random column if it doesn't exist (migration for existing databases)
            try
            {
                _initConnection.Execute(@"
                    ALTER TABLE poop_colors 
                    ADD COLUMN IF NOT EXISTS is_random BOOLEAN DEFAULT FALSE AFTER is_rainbow;
                ");
                _logger.LogInformation("Migration: 'is_random' column added to poop_colors table");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Column 'is_random' may already exist or migration not needed");
            }

            // Create poop_logs table for detailed poop event logging
            _initConnection.Execute(@"
                CREATE TABLE IF NOT EXISTS poop_logs (
                    id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
                    player_name VARCHAR(255) NOT NULL,
                    player_steamid VARCHAR(64) NOT NULL,
                    target_name VARCHAR(255) NULL,
                    target_steamid VARCHAR(64) NULL,
                    map_name VARCHAR(255) NOT NULL,
                    poop_size FLOAT NOT NULL,
                    poop_color_r TINYINT UNSIGNED NOT NULL,
                    poop_color_g TINYINT UNSIGNED NOT NULL,
                    poop_color_b TINYINT UNSIGNED NOT NULL,
                    is_rainbow BOOLEAN DEFAULT FALSE,
                    player_x FLOAT NOT NULL,
                    player_y FLOAT NOT NULL,
                    player_z FLOAT NOT NULL,
                    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    INDEX idx_player_steamid (player_steamid),
                    INDEX idx_target_steamid (target_steamid),
                    INDEX idx_map_name (map_name),
                    INDEX idx_timestamp (timestamp DESC)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            ");

            _logger.LogInformation("Table 'poop_logs' created or verified");
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

    public async Task SaveColorPreferenceAsync(ulong steamId, PoopColorPreference preference)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            await connection.ExecuteAsync(@"
                INSERT INTO poop_colors (steam_id, red, green, blue, is_rainbow, is_random) 
                VALUES (@SteamId, @Red, @Green, @Blue, @IsRainbow, @IsRandom)
                ON DUPLICATE KEY UPDATE 
                    red = @Red,
                    green = @Green,
                    blue = @Blue,
                    is_rainbow = @IsRainbow,
                    is_random = @IsRandom,
                    updated_at = CURRENT_TIMESTAMP",
                new
                {
                    SteamId = steamId,
                    Red = preference.Red,
                    Green = preference.Green,
                    Blue = preference.Blue,
                    IsRainbow = preference.IsRainbow,
                    IsRandom = preference.IsRandom
                });

            if (_config.DebugMode)
            {
                _logger.LogDebug("Saved color preference for {steamId}: RGB({r},{g},{b}) Rainbow={rainbow} Random={random}",
                    steamId, preference.Red, preference.Green, preference.Blue, preference.IsRainbow, preference.IsRandom);
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
                    is_rainbow as IsRainbow,
                    is_random as IsRandom
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

    public async Task<int> LogPoopAsync(PoopLogRecord record)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var result = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO poop_logs (
                    player_name, player_steamid, target_name, target_steamid,
                    map_name, poop_size, poop_color_r, poop_color_g, poop_color_b,
                    is_rainbow, player_x, player_y, player_z, timestamp
                ) VALUES (
                    @PlayerName, @PlayerSteamId, @TargetName, @TargetSteamId,
                    @MapName, @PoopSize, @PoopColorR, @PoopColorG, @PoopColorB,
                    @IsRainbow, @PlayerX, @PlayerY, @PlayerZ, @Timestamp
                );
                SELECT LAST_INSERT_ID();",
                new
                {
                    record.PlayerName,
                    record.PlayerSteamId,
                    record.TargetName,
                    record.TargetSteamId,
                    record.MapName,
                    record.PoopSize,
                    record.PoopColorR,
                    record.PoopColorG,
                    record.PoopColorB,
                    record.IsRainbow,
                    record.PlayerX,
                    record.PlayerY,
                    record.PlayerZ,
                    record.Timestamp
                });

            if (_config.DebugMode)
            {
                _logger.LogDebug("Logged poop #{id}: {player} -> {target} on {map} (size: {size})",
                    result, record.PlayerName, record.TargetName ?? "ground", record.MapName, record.PoopSize);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging poop for {player}", record.PlayerName);
            return -1;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PoopLogRecord[]> GetRecentPoopsAsync(int limit = 100, ulong? playerSteamId = null, string? mapName = null)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();

            var whereClause = "WHERE 1=1";
            if (playerSteamId.HasValue)
            {
                whereClause += " AND player_steamid = @PlayerSteamId";
            }
            if (!string.IsNullOrEmpty(mapName))
            {
                whereClause += " AND map_name = @MapName";
            }

            var query = $@"
                SELECT 
                    id as Id,
                    player_name as PlayerName,
                    player_steamid as PlayerSteamId,
                    target_name as TargetName,
                    target_steamid as TargetSteamId,
                    map_name as MapName,
                    poop_size as PoopSize,
                    poop_color_r as PoopColorR,
                    poop_color_g as PoopColorG,
                    poop_color_b as PoopColorB,
                    is_rainbow as IsRainbow,
                    player_x as PlayerX,
                    player_y as PlayerY,
                    player_z as PlayerZ,
                    timestamp as Timestamp
                FROM poop_logs 
                {whereClause}
                ORDER BY timestamp DESC
                LIMIT @Limit";

            var results = await connection.QueryAsync<PoopLogRecord>(query,
                new
                {
                    Limit = limit,
                    PlayerSteamId = playerSteamId?.ToString(),
                    MapName = mapName
                });

            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent poops");
            return Array.Empty<PoopLogRecord>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetTotalPoopsCountAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var result = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM poop_logs");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total poops count");
            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetVictimPoopCountAsync(string targetSteamId)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var result = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) 
                FROM poop_logs 
                WHERE target_steamid = @TargetSteamId",
                new { TargetSteamId = targetSteamId });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting victim poop count for {steamId}", targetSteamId);
            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TopPooperRecord[]> GetTopPoopersAsync(int limit = 10)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var results = await connection.QueryAsync<TopPooperRecord>(@"
                SELECT 
                    player_name as Name,
                    player_steamid as SteamId,
                    COUNT(*) as PoopCount
                FROM poop_logs
                GROUP BY player_steamid, player_name
                ORDER BY PoopCount DESC
                LIMIT @Limit",
                new { Limit = limit });

            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top poopers");
            return Array.Empty<TopPooperRecord>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TopVictimRecord[]> GetTopVictimsAsync(int limit = 10)
    {
        try
        {
            await _semaphore.WaitAsync();

            var connection = await GetConnectionAsync();
            var results = await connection.QueryAsync<TopVictimRecord>(@"
                SELECT 
                    target_name as Name,
                    target_steamid as SteamId,
                    COUNT(*) as VictimCount
                FROM poop_logs
                WHERE target_steamid IS NOT NULL
                GROUP BY target_steamid, target_name
                ORDER BY VictimCount DESC
                LIMIT @Limit",
                new { Limit = limit });

            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top victims");
            return Array.Empty<TopVictimRecord>();
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

    public bool Init()
    {
        return true;
    }
}
