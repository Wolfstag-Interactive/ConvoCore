---
sidebar_position: 6
title: Snapshot Migration
---

# Snapshot Migration

As you develop and ship updates to your game, the structure of your save data may change: new fields added, variables renamed, conversation GUIDs regenerated, or scope assignments altered. `ConvoCoreSnapshotMigrator` ensures that save files created with an older version of the schema can still be loaded correctly after an update.

---

## How it works

`ConvoCoreSnapshotMigrator.Migrate()` is called automatically by `ConvoCoreSaveManager` inside `Load()` and `InitializeSettings()`, before the snapshot is distributed to the rest of the system. The migrator reads the `SchemaVersion` string from the snapshot and applies any registered migration steps in version order until the snapshot is current.

The migration pipeline is transparent to your gameplay code - you never need to call `Migrate()` directly.

---

## Schema version field

Both `ConvoCoreGameSnapshot` and `ConvoCoreSettingsSnapshot` carry a `SchemaVersion` string property. When the save manager writes a snapshot, it stamps the current schema version into this field. When it reads a snapshot back, the stamped version tells the migrator how many steps (if any) need to be applied.

The current schema version is `"1.0"`.

:::note
For the majority of projects, the migrator requires no configuration whatsoever. It is infrastructure for forward-compatibility, a safety net that costs nothing until you need it. You only need to register migration steps if you deliberately change the shape of the save schema between shipped versions.
:::

---

## When to write a migration step

You need a migration step when a **shipped** version of your game wrote save files in a format that the current version no longer reads correctly. Common triggers:

- Renaming a variable key that was previously saved to disk.
- Changing a variable's scope (e.g. moving a key from Conversation scope to Global scope).
- Regenerating the GUID on a `ConvoCoreConversationData` asset after it was already shipped (avoids this where possible - see the warning below).
- Adding a required field to `ConvoCoreGameSnapshot` or `ConvoCoreSettingsSnapshot` that has no sensible default value.
- Removing a field whose presence in old saves would cause a deserialization conflict.

:::warning
Regenerating a `ConversationGuid` after the game has shipped is a destructive operation. All existing save files reference the old GUID. If you must regenerate, write a migration step that renames the old GUID key to the new one in every `ConversationSnapshot`. In general, treat `ConversationGuid` as immutable once the asset is shipped.
:::

---

## Adding a migration step

Register steps at startup, before `Initialize()` is called. The typical location is your bootstrapper's `Awake()` method.

```csharp
using WolfstagInteractive.ConvoCore.SaveSystem;
using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private ConvoCoreSaveManager _saveManager;

    private void Awake()
    {
        RegisterMigrationSteps();

        _saveManager.Initialize();
        _saveManager.InitializeSettings();
    }

    private void RegisterMigrationSteps()
    {
        // Migration from schema 1.0 to 2.0:
        // - Renamed global variable "quest_started" to "main_quest_active"
        // - Moved "player_class" from Conversation scope to Global scope
        ConvoCoreSnapshotMigrator.Register("1.0", "2.0", snapshot =>
        {
            // Rename a global variable key
            var questStarted = snapshot.GlobalVariables
                .Find(v => v.Key == "quest_started");
            if (questStarted != null)
                questStarted.Key = "main_quest_active";

            // Promote a conversation-scoped variable to global scope
            foreach (var conv in snapshot.Conversations)
            {
                var playerClass = conv.Variables.Find(v => v.Key == "player_class");
                if (playerClass != null)
                {
                    conv.Variables.Remove(playerClass);
                    snapshot.GlobalVariables.Add(playerClass);
                    break; // Only need one copy at global scope
                }
            }

            return snapshot;
        });

        // Migration from schema 2.0 to 3.0:
        // - Added a default value for the new "faction_standing" global variable
        ConvoCoreSnapshotMigrator.Register("2.0", "3.0", snapshot =>
        {
            bool alreadyExists = snapshot.GlobalVariables
                .Exists(v => v.Key == "faction_standing");

            if (!alreadyExists)
            {
                snapshot.GlobalVariables.Add(new ConvoCoreVariableEntry
                {
                    Key = "faction_standing",
                    TypedValue = "0",
                    VariableType = ConvoCoreVariableType.Int,
                    Scope = ConvoVariableScope.Global
                });
            }

            return snapshot;
        });
    }
}
```

After registering these steps, a save file stamped `"1.0"` will have **both** steps applied in sequence before the snapshot is used. A save file stamped `"2.0"` will only have the second step applied. A save file already at `"3.0"` passes through with no changes.

---

## Settings migration

Settings snapshots (`ConvoCoreSettingsSnapshot`) are migrated separately. Use `ConvoCoreSnapshotMigrator.RegisterSettings()` to register steps for settings schema changes:

```csharp
ConvoCoreSnapshotMigrator.RegisterSettings("1.0", "2.0", settingsSnapshot =>
{
    // Example: migrate a renamed field
    if (string.IsNullOrEmpty(settingsSnapshot.LanguageCode))
        settingsSnapshot.LanguageCode = settingsSnapshot.LegacyLocale ?? "EN";

    return settingsSnapshot;
});
```

---

## Versioning conventions

When you make a schema change:

1. Increment the schema version string in the `ConvoCoreGameSnapshot` class (and/or `ConvoCoreSettingsSnapshot` if settings changed). Keep the version as a `"major.minor"` string: increment `major` for breaking changes, `minor` for additive changes.

2. Add a comment at the class declaration noting what changed and when:

```csharp
/// <summary>
/// Schema history:
/// 1.0 - initial release
/// 2.0 - renamed quest_started to main_quest_active; promoted player_class to Global scope
/// 3.0 - added faction_standing global variable with default 0
/// </summary>
public class ConvoCoreGameSnapshot
{
    public string SchemaVersion = "3.0";
    // ...
}
```

3. Register the corresponding migration step (see above).

4. Test the migration by manually editing a save file's `SchemaVersion` field back to an older version and loading it in Play Mode, verifying the migrated values are correct.

---

## Migration step requirements

:::info[For Advanced Users]
Migration steps are applied in ascending version order. If a save file is multiple versions behind, all steps in the chain are applied sequentially. For example, a `"1.0"` save with steps registered for `1.0→2.0`, `2.0→3.0`, and `3.0→4.0` will have all three steps applied in that order.

**Idempotency**: Each migration step should be safe to apply more than once. Guard all mutations with existence checks (as shown in the examples above). This protects against edge cases where the migrator is accidentally called twice on the same snapshot during development.

**Statelessness**: Migration steps receive only the snapshot - they do not have access to Unity assets, the variable store, or any other runtime state. If your migration needs to look up a GUID from a `ConvoCoreConversationData` asset, bake the GUID into a constant in your migration code at the time you write the step. Do not rely on loading the asset at migration time, as it may not be available in all contexts (e.g. headless builds).

**Null safety**: Always check for `null` before accessing nested collections. Old save files may be missing fields that were added in later schema versions - the deserializer initialises missing collections as `null`, not as empty lists.
:::

---

## Detecting missing migration steps

If `ConvoCoreSaveManager.Load()` reads a snapshot whose `SchemaVersion` is greater than the current version registered in the code, it logs a warning:

```
[ConvoCore] Warning: save file schema version "3.0" is newer than the current schema "2.0".
The save was created with a newer version of the game. Some data may not be loaded correctly.
```

This typically indicates a player is running an older build after saving with a newer one. There is no automatic fix for downgrade scenarios; handle this case by displaying a warning to the player or preventing the load.
