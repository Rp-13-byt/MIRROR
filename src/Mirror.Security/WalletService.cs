using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Security;

public class WalletService : IWalletService
{
    private readonly IMirrorRepository _repository;
    private readonly string? _dbPath;

    public WalletService(IMirrorRepository repository, string? dbPath = null)
    {
        _repository = repository;
        _dbPath = dbPath;
    }

    public async Task<string> ExportWalletAsync(string destinationFilePath, string password, CancellationToken ct = default)
    {
        var sessions = await _repository.GetSessionsAsync(DateTime.UtcNow.AddYears(-10), DateTime.UtcNow.AddDays(1), ct);
        var patterns = await _repository.GetPatternEventsAsync(DateTime.UtcNow.AddYears(-10), DateTime.UtcNow.AddDays(1), ct);
        var flows = await _repository.GetFlowStateSessionsAsync(DateTime.UtcNow.AddYears(-10), DateTime.UtcNow.AddDays(1), ct);
        var baselines = await _repository.GetAllAdaptiveBaselinesAsync(14, ct);

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Sessions JSON
            var sessionEntry = archive.CreateEntry("sessions.json");
            using (var writer = new StreamWriter(sessionEntry.Open(), Encoding.UTF8))
            {
                await writer.WriteAsync(JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true }));
            }

            // Patterns JSON
            var patternEntry = archive.CreateEntry("patterns.json");
            using (var writer = new StreamWriter(patternEntry.Open(), Encoding.UTF8))
            {
                await writer.WriteAsync(JsonSerializer.Serialize(patterns, new JsonSerializerOptions { WriteIndented = true }));
            }

            // Flow sessions JSON
            var flowEntry = archive.CreateEntry("flow_sessions.json");
            using (var writer = new StreamWriter(flowEntry.Open(), Encoding.UTF8))
            {
                await writer.WriteAsync(JsonSerializer.Serialize(flows, new JsonSerializerOptions { WriteIndented = true }));
            }

            // Baselines JSON
            var baselineEntry = archive.CreateEntry("baselines.json");
            using (var writer = new StreamWriter(baselineEntry.Open(), Encoding.UTF8))
            {
                await writer.WriteAsync(JsonSerializer.Serialize(baselines, new JsonSerializerOptions { WriteIndented = true }));
            }

            // Raw SQLite copy if available
            if (!string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath))
            {
                var dbEntry = archive.CreateEntry("mirror.db");
                using var dbSource = new FileStream(_dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var dbDest = dbEntry.Open();
                await dbSource.CopyToAsync(dbDest, ct);
            }

            // Manifest JSON
            var manifestEntry = archive.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
            {
                var manifest = new
                {
                    version = "1.0",
                    exported_utc = DateTime.UtcNow.ToString("o"),
                    total_sessions = sessions.Count,
                    total_patterns = patterns.Count,
                    total_flow_sessions = flows.Count
                };
                await writer.WriteAsync(JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        zipStream.Seek(0, SeekOrigin.Begin);
        byte[] rawZip = zipStream.ToArray();

        // Encrypt with AES-256-GCM + PBKDF2
        byte[] encrypted = AesGcmEncryptor.Encrypt(rawZip, password);

        string? dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(destinationFilePath, encrypted, ct);
        return destinationFilePath;
    }

    public async Task<bool> ImportWalletAsync(string sourceWalletFilePath, string password, CancellationToken ct = default)
    {
        if (!File.Exists(sourceWalletFilePath))
        {
            throw new FileNotFoundException("Wallet file not found", sourceWalletFilePath);
        }

        byte[] encrypted = await File.ReadAllBytesAsync(sourceWalletFilePath, ct);
        byte[] decryptedZip = AesGcmEncryptor.Decrypt(encrypted, password);

        using var zipStream = new MemoryStream(decryptedZip);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var manifestEntry = archive.GetEntry("manifest.json");
        if (manifestEntry == null)
        {
            throw new InvalidDataException("Archive is missing manifest.json");
        }

        // Import sessions if present
        var sessionsEntry = archive.GetEntry("sessions.json");
        if (sessionsEntry != null)
        {
            using var reader = new StreamReader(sessionsEntry.Open(), Encoding.UTF8);
            string json = await reader.ReadToEndAsync(ct);
            var sessions = JsonSerializer.Deserialize<List<ActivitySession>>(json);
            if (sessions != null && sessions.Count > 0)
            {
                await _repository.InsertSessionsBatchAsync(sessions, ct);
            }
        }

        // Import patterns if present
        var patternsEntry = archive.GetEntry("patterns.json");
        if (patternsEntry != null)
        {
            using var reader = new StreamReader(patternsEntry.Open(), Encoding.UTF8);
            string json = await reader.ReadToEndAsync(ct);
            var patterns = JsonSerializer.Deserialize<List<PatternEvent>>(json);
            if (patterns != null)
            {
                foreach (var p in patterns)
                {
                    await _repository.InsertPatternEventAsync(p, ct);
                }
            }
        }

        // Import flow states if present
        var flowEntry = archive.GetEntry("flow_sessions.json");
        if (flowEntry != null)
        {
            using var reader = new StreamReader(flowEntry.Open(), Encoding.UTF8);
            string json = await reader.ReadToEndAsync(ct);
            var flows = JsonSerializer.Deserialize<List<FlowStateSession>>(json);
            if (flows != null)
            {
                foreach (var f in flows)
                {
                    await _repository.InsertFlowStateSessionAsync(f, ct);
                }
            }
        }

        // Import baselines if present
        var baselineEntry = archive.GetEntry("baselines.json");
        if (baselineEntry != null)
        {
            using var reader = new StreamReader(baselineEntry.Open(), Encoding.UTF8);
            string json = await reader.ReadToEndAsync(ct);
            var baselines = JsonSerializer.Deserialize<List<AdaptiveBaseline>>(json);
            if (baselines != null)
            {
                foreach (var b in baselines)
                {
                    await _repository.UpsertAdaptiveBaselineAsync(b, ct);
                }
            }
        }

 return true;
 }
}