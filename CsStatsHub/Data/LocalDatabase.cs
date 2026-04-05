using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using CsStatsHub.Models;

namespace CsStatsHub.Data;

public sealed class LocalDatabase
{
    private readonly string _dbPath;

    public LocalDatabase()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "csstatshub.db3");
    }

    public void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Players (
                SteamId TEXT NOT NULL PRIMARY KEY,
                PersonaName TEXT NOT NULL,
                AvatarUrl TEXT NOT NULL,
                Kills REAL NOT NULL DEFAULT 0,
                Deaths REAL NOT NULL DEFAULT 1,
                HeadshotKills REAL NOT NULL DEFAULT 0,
                Wins INTEGER NOT NULL DEFAULT 0,
                MatchesPlayed INTEGER NOT NULL DEFAULT 0,
                UpdatedUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Friends (
                OwnerSteamId TEXT NOT NULL,
                FriendSteamId TEXT NOT NULL,
                PersonaName TEXT NOT NULL,
                AvatarUrl TEXT NOT NULL,
                PersonaState INTEGER NOT NULL,
                UpdatedUtc TEXT NOT NULL,
                PRIMARY KEY (OwnerSteamId, FriendSteamId)
            );

            CREATE TABLE IF NOT EXISTS Matches (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SteamId TEXT NOT NULL,
                MapName TEXT NOT NULL,
                ScoreText TEXT NOT NULL,
                IsWin INTEGER NOT NULL,
                Kills INTEGER NOT NULL,
                Deaths INTEGER NOT NULL,
                Assists INTEGER NOT NULL,
                SortIndex INTEGER NOT NULL,
                UpdatedUtc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Matches_SteamId ON Matches(SteamId);
            CREATE INDEX IF NOT EXISTS IX_Friends_Owner ON Friends(OwnerSteamId);
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    public PlayerCacheRow? GetPlayer(string steamId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT SteamId, PersonaName, AvatarUrl, Kills, Deaths, HeadshotKills, Wins, MatchesPlayed, UpdatedUtc
            FROM Players WHERE SteamId = $id
            """;
        cmd.Parameters.AddWithValue("$id", steamId);
        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return null;

        return new PlayerCacheRow(
            r.GetString(0),
            r.GetString(1),
            r.GetString(2),
            r.GetDouble(3),
            r.GetDouble(4),
            r.GetDouble(5),
            r.GetInt32(6),
            r.GetInt32(7),
            DateTime.Parse(r.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    public void UpsertPlayer(PlayerCacheRow row)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Players (SteamId, PersonaName, AvatarUrl, Kills, Deaths, HeadshotKills, Wins, MatchesPlayed, UpdatedUtc)
            VALUES ($sid, $name, $avatar, $kills, $deaths, $hs, $wins, $mp, $u)
            ON CONFLICT(SteamId) DO UPDATE SET
                PersonaName = excluded.PersonaName,
                AvatarUrl = excluded.AvatarUrl,
                Kills = excluded.Kills,
                Deaths = excluded.Deaths,
                HeadshotKills = excluded.HeadshotKills,
                Wins = excluded.Wins,
                MatchesPlayed = excluded.MatchesPlayed,
                UpdatedUtc = excluded.UpdatedUtc
            """;
        cmd.Parameters.AddWithValue("$sid", row.SteamId);
        cmd.Parameters.AddWithValue("$name", row.PersonaName);
        cmd.Parameters.AddWithValue("$avatar", row.AvatarUrl);
        cmd.Parameters.AddWithValue("$kills", row.Kills);
        cmd.Parameters.AddWithValue("$deaths", row.Deaths <= 0 ? 1 : row.Deaths);
        cmd.Parameters.AddWithValue("$hs", row.HeadshotKills);
        cmd.Parameters.AddWithValue("$wins", row.Wins);
        cmd.Parameters.AddWithValue("$mp", row.MatchesPlayed);
        cmd.Parameters.AddWithValue("$u", row.UpdatedUtc.ToString("o", CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<FriendCacheRow> GetFriends(string ownerSteamId)
    {
        var list = new List<FriendCacheRow>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT FriendSteamId, PersonaName, AvatarUrl, PersonaState, UpdatedUtc
            FROM Friends WHERE OwnerSteamId = $o ORDER BY PersonaName COLLATE NOCASE
            """;
        cmd.Parameters.AddWithValue("$o", ownerSteamId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new FriendCacheRow(
                r.GetString(0),
                r.GetString(1),
                r.GetString(2),
                r.GetInt32(3),
                DateTime.Parse(r.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return list;
    }

    public void ReplaceFriends(string ownerSteamId, IEnumerable<FriendCacheRow> friends)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM Friends WHERE OwnerSteamId = $o";
            del.Parameters.AddWithValue("$o", ownerSteamId);
            del.ExecuteNonQuery();
        }

        foreach (var f in friends)
        {
            using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = """
                INSERT INTO Friends (OwnerSteamId, FriendSteamId, PersonaName, AvatarUrl, PersonaState, UpdatedUtc)
                VALUES ($o, $f, $n, $a, $s, $u)
                """;
            ins.Parameters.AddWithValue("$o", ownerSteamId);
            ins.Parameters.AddWithValue("$f", f.SteamId);
            ins.Parameters.AddWithValue("$n", f.PersonaName);
            ins.Parameters.AddWithValue("$a", f.AvatarUrl);
            ins.Parameters.AddWithValue("$s", f.PersonaState);
            ins.Parameters.AddWithValue("$u", f.UpdatedUtc.ToString("o", CultureInfo.InvariantCulture));
            ins.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public IReadOnlyList<MatchCacheRow> GetMatches(string steamId)
    {
        var list = new List<MatchCacheRow>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT MapName, ScoreText, IsWin, Kills, Deaths, Assists, SortIndex
            FROM Matches WHERE SteamId = $s ORDER BY SortIndex DESC
            """;
        cmd.Parameters.AddWithValue("$s", steamId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new MatchCacheRow(
                r.GetString(0),
                r.GetString(1),
                r.GetInt32(2) != 0,
                r.GetInt32(3),
                r.GetInt32(4),
                r.GetInt32(5),
                r.GetInt32(6)));
        }

        return list;
    }

    public void ReplaceMatches(string steamId, IReadOnlyList<MatchCacheRow> matches)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM Matches WHERE SteamId = $s";
            del.Parameters.AddWithValue("$s", steamId);
            del.ExecuteNonQuery();
        }

        var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        foreach (var m in matches)
        {
            using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = """
                INSERT INTO Matches (SteamId, MapName, ScoreText, IsWin, Kills, Deaths, Assists, SortIndex, UpdatedUtc)
                VALUES ($s, $map, $score, $win, $k, $d, $a, $i, $u)
                """;
            ins.Parameters.AddWithValue("$s", steamId);
            ins.Parameters.AddWithValue("$map", m.MapName);
            ins.Parameters.AddWithValue("$score", m.ScoreText);
            ins.Parameters.AddWithValue("$win", m.IsWin ? 1 : 0);
            ins.Parameters.AddWithValue("$k", m.Kills);
            ins.Parameters.AddWithValue("$d", m.Deaths);
            ins.Parameters.AddWithValue("$a", m.Assists);
            ins.Parameters.AddWithValue("$i", m.SortIndex);
            ins.Parameters.AddWithValue("$u", now);
            ins.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
