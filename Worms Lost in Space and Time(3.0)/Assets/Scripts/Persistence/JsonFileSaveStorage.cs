using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Worms.Core;

namespace Worms.Persistence
{
    /// <summary>Bounded JSON adapter with durable temporary writes, backup recovery and future-version protection.</summary>
    public sealed class JsonFileSaveStorage : ISaveStorage, ISaveStorageDiagnostics
    {
        public string Notice { get; private set; }
        public const int MaximumFileBytes = 1024 * 1024;
        private readonly string path;
        private readonly string backupPath;
        private readonly string temporaryPath;
        public JsonFileSaveStorage(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A save directory is required.", nameof(directory));
            path = Path.Combine(directory, "progress-v1.json");
            backupPath = path + ".bak";
            temporaryPath = path + ".tmp";
        }

        public SaveData Load()
        {
            Notice = null;
            if (!File.Exists(path))
            {
                if (!File.Exists(backupPath)) return null;
                SaveData backup = Read(backupPath);
                File.Copy(backupPath, path);
                Notice = "Progress was recovered from its backup after an interrupted save.";
                return backup;
            }
            try { return Read(path); }
            catch (FutureSaveVersionException) { throw; }
            catch (InvalidDataException original)
            {
                if (!File.Exists(backupPath)) throw;
                SaveData backup;
                try { backup = Read(backupPath); }
                catch (IOException) { throw new InvalidDataException("Primary and backup save are unreadable; both were preserved.", original); }
                // Preserve exact corrupt bytes before restoring the last readable backup.
                string quarantine = path + ".corrupt-" + Guid.NewGuid().ToString("N");
                File.Move(path, quarantine);
                try { File.Copy(backupPath, path); }
                catch
                {
                    if (!File.Exists(path)) File.Move(quarantine, path);
                    throw;
                }
                Notice = "Damaged progress was preserved and the previous backup was recovered.";
                return backup;
            }
        }

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Version != 1) throw new InvalidDataException("Only save version 1 can be written.");
            // Recheck disk even after a successful initial load: a newer application may have written it.
            if (File.Exists(path)) Read(path);
            if (File.Exists(backupPath))
            {
                try { Read(backupPath); }
                catch (InvalidDataException) { /* A corrupt backup may be replaced by the valid primary. */ }
            }
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(data, true));
            if (bytes.Length > MaximumFileBytes) throw new InvalidDataException("Save exceeds maximum size.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporaryPath, path, backupPath);
                else File.Move(temporaryPath, path);
            }
            finally
            {
                // Failure never deletes the authoritative save or its backup.
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private static SaveData Read(string file)
        {
            var info = new FileInfo(file);
            if (info.Length == 0 || info.Length > MaximumFileBytes) throw new InvalidDataException("Save is empty or exceeds maximum size.");
            string json;
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Bound actual reads as well as metadata if the file changes concurrently.
                var bytes = new byte[MaximumFileBytes + 1];
                int total = 0;
                int read;
                while (total < bytes.Length && (read = stream.Read(bytes, total, bytes.Length - total)) > 0)
                    total += read;
                if (total > MaximumFileBytes) throw new InvalidDataException("Save exceeds maximum size.");
                try { json = new UTF8Encoding(false, true).GetString(bytes, 0, total).Trim().TrimStart('\uFEFF'); }
                catch (DecoderFallbackException error) { throw new InvalidDataException("Save is not valid UTF-8.", error); }
            }
            Match version = Regex.Match(json, @"""Version""\s*:\s*(-?\d+)");
            int versionNumber;
            if (version.Success && !version.Groups[1].Value.StartsWith("-", StringComparison.Ordinal)
                && (!int.TryParse(version.Groups[1].Value, out versionNumber) || versionNumber > 1))
                throw new FutureSaveVersionException(int.TryParse(version.Groups[1].Value, out versionNumber) ? versionNumber : int.MaxValue);
            if (!json.StartsWith("{", StringComparison.Ordinal) || !json.EndsWith("}", StringComparison.Ordinal)
                || Regex.Matches(json, @"""Version""\s*:").Count != 1
                || Regex.Matches(json, @"""Levels""\s*:").Count != 1
                || !Regex.IsMatch(json, @"""Levels""\s*:\s*\["))
                throw new InvalidDataException("Save must contain one Version and a Levels array.");
            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (ArgumentException error) { throw new InvalidDataException("Malformed save JSON; original file preserved.", error); }
            if (data == null) throw new InvalidDataException("Save JSON is null.");
            if (data.Version > 1) throw new FutureSaveVersionException(data.Version);
            if (data.Version != 1 || data.Levels == null) throw new InvalidDataException("Unsupported or incomplete save document.");
            return data;
        }
    }

    public sealed class FutureSaveVersionException : IOException
    {
        public int Version { get; }
        public FutureSaveVersionException(int version) : base("Save version " + version + " is newer than supported version 1.")
        {
            Version = version;
        }
    }
}
