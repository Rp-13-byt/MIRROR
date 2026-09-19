using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Mirror.Persistence;
using Mirror.Security;
using Xunit;

namespace Mirror.Privacy.Tests;

public class PrivacyAuditTests
{
    private readonly PrivacyGuard _guard = new();

    [Fact]
    public void DomainEntities_DoNotExposeForbiddenFields()
    {
        var coreAssembly = typeof(ActivitySession).Assembly;
        var entityTypes = coreAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && (t.Namespace?.StartsWith("Mirror.Core") ?? false))
            .ToList();

        Assert.NotEmpty(entityTypes);

        foreach (var type in entityTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                bool isForbidden = _guard.IsFieldForbidden(prop.Name);
                Assert.False(isForbidden, $"Forbidden property '{prop.Name}' found on type '{type.FullName}'! Mirror must never store or expose window titles, keystrokes, URLs, or screen captures.");
            }
        }
    }

    private class IllegalSpyEntity
    {
        public string WindowTitle { get; set; } = "Secret Bank Document";
        public string Url { get; set; } = "https://private.example.com";
    }

    [Fact]
    public void PrivacyGuard_ThrowsOnForbiddenEntity()
    {
        var illegal = new IllegalSpyEntity();
        var ex = Assert.Throws<PrivacyViolationException>(() => _guard.ValidateSafeEntity(illegal));

        Assert.Contains("WindowTitle", ex.Message);
        Assert.Contains("Url", ex.Message);
    }

    [Theory]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe", "chrome")]
    [InlineData(@"D:\Apps\vscode.exe", "vscode")]
    [InlineData("slack.exe", "slack")]
    [InlineData("explorer", "explorer")]
    [InlineData("", "Unknown")]
    [InlineData(null, "Unknown")]
    public void SanitizeProcessName_ExtractsCleanProcessNameWithoutPathsOrExtension(string? raw, string expected)
    {
        string actual = PrivacyGuard.SanitizeProcessName(raw!);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProductionAssemblies_ContainZeroNetworkingTypes()
    {
        var assembliesToAudit = new[]
        {
            typeof(Mirror.Core.Models.ActivitySession).Assembly,
            typeof(Mirror.Security.PrivacyGuard).Assembly,
            typeof(Mirror.Persistence.SqliteConnectionFactory).Assembly,
            typeof(Mirror.Tracking.ProcessResolver).Assembly,
            typeof(Mirror.Analytics.BaselineAnalyzer).Assembly,
            typeof(Mirror.Inference.CpuInferenceBackend).Assembly
        };

        foreach (var assembly in assembliesToAudit)
        {
            bool isClean = NetworkPolicy.ScanAssemblyForForbiddenTypes(assembly, out var violations);
            Assert.True(isClean, $"Assembly '{assembly.GetName().Name}' failed privacy network audit: {string.Join("; ", violations)}");
        }
    }

    [Fact]
    public void SqliteSchema_NeverStoresKeystrokesOrWindowTitles()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"mirror_privacy_schema_test_{Guid.NewGuid():N}.db");
        try
        {
            var factory = new SqliteConnectionFactory(tempDb);
            var migrator = new DatabaseMigrator(factory);
            migrator.Migrate();

            using var conn = factory.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table';";
            using var reader = cmd.ExecuteReader();

            var forbiddenSqlKeywords = new[]
            {
                "window_title", "windowtitle", "title", "url", "keystroke", "keystrokes", "keylogger",
                "clipboard", "screen_capture", "screenshot", "buffer", "raw_text"
            };

            while (reader.Read())
            {
                string sql = reader.GetString(0).ToLowerInvariant();
                foreach (var kw in forbiddenSqlKeywords)
                {
                    Assert.DoesNotContain($" {kw} ", $" {sql} ");
                }
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}

public class WellbeingWalletTests
{
    [Fact]
    public void AesGcmEncryptor_Roundtrip_SucceedsWithValidPassword()
    {
        byte[] original = System.Text.Encoding.UTF8.GetBytes("Mirror 100% Local Confidential Behavioral Data Payload");
        string password = "StrongLocalMasterPassword!2026";

        byte[] encrypted = AesGcmEncryptor.Encrypt(original, password);

        Assert.NotNull(encrypted);
        Assert.NotEqual(original, encrypted);

        byte[] decrypted = AesGcmEncryptor.Decrypt(encrypted, password);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void AesGcmEncryptor_ThrowsCryptographicException_OnWrongPassword()
    {
        byte[] original = System.Text.Encoding.UTF8.GetBytes("Sensitive Local Data");
        byte[] encrypted = AesGcmEncryptor.Encrypt(original, "CorrectPassword");

        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() =>
            AesGcmEncryptor.Decrypt(encrypted, "WrongPassword"));
    }

    [Fact]
    public async Task WalletService_ExportAndImport_RoundtripsSuccessfully()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"mirror_wallet_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string dbPath = Path.Combine(tempDir, "test.db");
        string walletPath = Path.Combine(tempDir, "backup.mirrorwallet");

        try
        {
            var factory = new SqliteConnectionFactory(dbPath);
            new DatabaseMigrator(factory).Migrate();
            var repo = new MirrorRepository(factory);

            // Seed test sessions
            await repo.InsertSessionAsync(new ActivitySession
            {
                AppKey = "vscode",
                DisplayName = "Visual Studio Code",
                Category = "Development",
                StartUtc = DateTime.UtcNow.AddHours(-2),
                EndUtc = DateTime.UtcNow.AddHours(-1),
                ActiveSeconds = 3600,
                CloseReason = SessionCloseReason.AppSwitch
            });

            var walletService = new WalletService(repo, dbPath);

            // Export
            string exportedFile = await walletService.ExportWalletAsync(walletPath, "WalletSecretPassphrase#123");
            Assert.True(File.Exists(exportedFile));

            // Clear database
            await repo.PurgeAllDataAsync();
            var sessionsAfterPurge = await repo.GetSessionsAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
            Assert.Empty(sessionsAfterPurge);

            // Import
            bool success = await walletService.ImportWalletAsync(exportedFile, "WalletSecretPassphrase#123");
            Assert.True(success);

            var restoredSessions = await repo.GetSessionsAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
            Assert.Single(restoredSessions);
            Assert.Equal("vscode", restoredSessions[0].AppKey);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}

