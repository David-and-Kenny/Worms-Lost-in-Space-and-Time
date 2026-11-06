using System;
using System.IO;
using UnityEngine;
using Worms.Configuration;
using Worms.Core;
using Worms.Persistence;

namespace Worms
{
    /// <summary>Composition root. Domain services never depend on scene or UI objects.</summary>
    public static class GameServices
    {
        public static ILevelCatalog Catalog { get; private set; }
        public static ProgressionService Progression { get; private set; }
        public static SaveService Saves { get; private set; }
        public static PointerPressGate PointerGate { get; } = new PointerPressGate();
        public static bool LegacySaveDetected { get; private set; }
        public static string LegacySaveNotice => LegacySaveDetected
            ? "Old playerInfo.dat progress is retained but is not imported. This version starts fresh JSON progress." : null;

        public static void EnsureInitialized()
        {
            if (Saves != null) return;
            var catalog = Resources.Load<LevelCatalog>("Configuration/LevelCatalog");
            if (catalog == null) throw new InvalidOperationException("Missing Resources/Configuration/LevelCatalog asset.");
            Configure(catalog, new JsonFileSaveStorage(Application.persistentDataPath));
            LegacySaveDetected = File.Exists(Path.Combine(Application.persistentDataPath, "playerInfo.dat"));
        }

        public static void ConfigureForTests(ILevelCatalog catalog, ISaveStorage storage)
        {
            ResetForTests();
            Configure(catalog, storage);
        }

        public static void ConfigureForTests(ILevelCatalog catalog, ISaveStorage storage, bool legacySaveDetected)
        {
            ConfigureForTests(catalog, storage);
            LegacySaveDetected = legacySaveDetected;
        }

        private static void Configure(ILevelCatalog catalog, ISaveStorage storage)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Saves = new SaveService(storage, catalog);
            Progression = new ProgressionService(catalog, Saves);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetForTests()
        {
            Catalog = null;
            Saves = null;
            Progression = null;
            LegacySaveDetected = false;
            // Keep the same object for PlayerInput adapters retained when scene/domain reload is disabled.
            PointerGate.Reset();
        }
    }
}
