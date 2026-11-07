using System;
using System.IO;
using Worms.Core;

namespace Worms.Monetization
{
    /// <summary>A recoverable offline cache; the store remains the ownership authority.</summary>
    public sealed class EntitlementFileStorage : IEntitlementStorage
    {
        private readonly string path;
        public EntitlementFileStorage(string directory) { path = Path.Combine(directory, "remove-ads-v1.txt"); }
        public bool Load()
        {
            if (!File.Exists(path)) return false;
            string value = File.ReadAllText(path);
            if (value == "owned-v1") return true;
            if (value == "not-owned-v1") return false;
            throw new InvalidDataException("The purchase cache could not be read. Restore Purchases to recover ownership.");
        }
        public void Save(bool owned)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, owned ? "owned-v1" : "not-owned-v1");
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
    }
}
