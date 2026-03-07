using UnityEngine;

namespace WolfstagInteractive.ConvoCore.SaveSystem
{
    public static class ConvoCoreSnapshotMigrator
    {
        public static ConvoCoreGameSnapshot Migrate(ConvoCoreGameSnapshot snapshot)
        {
            if (snapshot == null) return null;

            switch (snapshot.SchemaVersion)
            {
                case "1.0":
                    return MigrateGame_1_0(snapshot);
                default:
                    Debug.LogWarning($"[ConvoCoreSnapshotMigrator] Unknown game snapshot schema version '{snapshot.SchemaVersion}'. Returning unmodified.");
                    return snapshot;
            }
        }

        public static ConvoCoreSettingsSnapshot Migrate(ConvoCoreSettingsSnapshot snapshot)
        {
            if (snapshot == null) return null;

            switch (snapshot.SchemaVersion)
            {
                case "1.0":
                    return MigrateSettings_1_0(snapshot);
                default:
                    Debug.LogWarning($"[ConvoCoreSnapshotMigrator] Unknown settings snapshot schema version '{snapshot.SchemaVersion}'. Returning unmodified.");
                    return snapshot;
            }
        }

        private static ConvoCoreGameSnapshot MigrateGame_1_0(ConvoCoreGameSnapshot snapshot)
        {
            // Version 1.0 is the current version, no migration needed.
            return snapshot;
        }

        private static ConvoCoreSettingsSnapshot MigrateSettings_1_0(ConvoCoreSettingsSnapshot snapshot)
        {
            // Version 1.0 is the current version, no migration needed.
            return snapshot;
        }
    }
}