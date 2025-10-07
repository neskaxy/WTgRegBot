using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using TL;
using WTelegram;

public static class Back
{
    private static readonly ConcurrentDictionary<Guid, Client> Clients = new();
    private static readonly string Sessions = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions_" + AppDomain.CurrentDomain.FriendlyName);
    private const string Db = "profiles.db";
    public static Action<long, string>? Notify;

    static Back()
    {
        if (!Directory.Exists(Sessions))
            Directory.CreateDirectory(Sessions);
        InitDb();
    }
    private static void InitDb()
    {
        using var conn = new SqliteConnection($"Data Source={Db}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS profiles (
                id TEXT PRIMARY KEY,
                user_id INTEGER,
                phone TEXT,
                api_id TEXT,
                api_hash TEXT,
                session_path TEXT,
                created_at TEXT
            );";
        cmd.ExecuteNonQuery();
    }
    public static async Task<string> LoginStart(Guid profileId, long userId, string phone, string apiId, string apiHash)
    {
        phone = phone.Trim();
        apiId = apiId.Trim();
        apiHash = apiHash.Trim();

        string sessionPath = Path.Combine(Sessions, $"{profileId}.session");
        string Config(string what) => what switch
        {
            "api_id" => apiId,
            "api_hash" => apiHash,
            "session_pathname" => sessionPath,
            "phone_number" => phone,
            _ => null
        } ?? string.Empty;
        try
        {
            var client = new Client(Config);
            Clients[profileId] = client;
            var next = await client.Login(phone);
            return next ?? "Введи код из Telegram";
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
            var client = new Client(Config);
            Clients[profileId] = client;
            var next = await client.Login(phone);
            return next ?? "Введи код из Telegram";
        }
        catch (Exception ex)
        {
            return "ошибка:" + ex.Message;
        }
    }
    public static async Task<(bool ok, string nextOrError)> LoginFinish(Guid profileId, long userId, string input, string phone, string apiId, string apiHash)
    {
        if (!Clients.TryGetValue(profileId, out var client))
            return (false, "no_client");
        try
        {
            var next = await client.Login(input);
            if (client.User != null)
            {
                SaveProfile(profileId, userId, phone, apiId, apiHash, Path.Combine(Sessions, $"{profileId}.session"));
                return (true, "");
            }
            return (false, next ?? "unknown_step");
        }
        catch (RpcException ex) when (ex.Code == 400 && ex.Message.Contains("PHONE_CODE_EXPIRED"))
        {
            return (false, "Код истек, попробуй заново");
        }
        catch (Exception ex)
        {
            return (false, "ошибка:" + ex.Message);
        }
    }
    private static void SaveProfile(Guid id, long userId, string phone, string apiId, string apiHash, string sessionPath)
    {
        using var conn = new SqliteConnection($"Data Source={Db}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO profiles (id, user_id, phone, api_id, api_hash, session_path, created_at)
            VALUES ($id, $uid, $phone, $apiid, $apihash, $session, $created)";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$uid", userId);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$apiid", apiId);
        cmd.Parameters.AddWithValue("$apihash", apiHash);
        cmd.Parameters.AddWithValue("$session", sessionPath);
        cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }
    public static string GetProfiles(long userId)
    {
        using var conn = new SqliteConnection($"Data Source={Db}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT phone, id FROM profiles WHERE user_id=$uid";
        cmd.Parameters.AddWithValue("$uid", userId);
        using var reader = cmd.ExecuteReader();
        string res = "";
        while (reader.Read())
            res += $"📱 {reader.GetString(0)}\nID {reader.GetString(1)}\n\n";
        return string.IsNullOrWhiteSpace(res) ? "У тебя нет профилей :(" : res;
    }
}
