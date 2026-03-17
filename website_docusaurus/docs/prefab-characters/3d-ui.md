---
sidebar_position: 3
title: 3D UI
---

# 3D UI

`ConvoCoreSampleUI3D` is the world-space dialogue UI. It handles dialogue text, speaker names, and choice buttons, but has no concept of character slots. Instead, it delegates all character placement to a **presence** — a ScriptableObject that decides where each character goes and how long they stay there.

Use this class when your characters are GameObjects living in the scene, not elements rendered inside a Canvas.

---

## How It Works

```
ConvoCoreSampleUI3D
       │
       │  conversation starts
       ▼
ConvoCoreCharacterPresence.OnConversationBegin()
       │
       │  each line begins
       ▼
ConvoCoreCharacterPresence.ResolvePresence(rep, context, spawner)
       │                   returns IConvoCoreCharacterDisplay
       ▼
display.BindRepresentation(representationAsset)
display.ApplyDisplayOptions(lineOptions)
display.ApplyExpression(expressionId)
       │
       │  conversation ends
       ▼
ConvoCoreCharacterPresence.OnConversationEnd()
```

The UI calls `OnConversationBegin()` when the conversation starts, then calls `ResolvePresence()` for each character on each line to get an `IConvoCoreCharacterDisplay` to apply expressions to. When the conversation ends it calls `OnConversationEnd()`, which is where the presence tears down spawned instances.

---

## Setup

### 1. Create a presence asset

Right-click in the Project panel → **Create → ConvoCore → Presence** → choose the type that matches your scene setup. See [Presence Types →](presence-types) for guidance on which one to use.

### 2. Add the spawner and pool

`ConvoCoreSampleUI3D` needs a `ConvoCorePrefabRepresentationSpawner` for presences that spawn characters.

1. Select the GameObject that holds your `ConvoCoreSampleUI3D` component.
2. **Add Component → ConvoCorePrefabPool**.
3. **Add Component → ConvoCorePrefabRepresentationSpawner**. Drag `ConvoCorePrefabPool` into the **Pool** field.
4. Drag `ConvoCorePrefabRepresentationSpawner` into the **Prefab Representation Spawner** field on `ConvoCoreSampleUI3D`.

:::note
Not all presences use the spawner. `ExternalPresence` for scene-resident characters never calls it. If you're using only `ExternalPresence`, the spawner and pool are still required fields but will be idle.
:::

### 3. Assign the presence

Drag your presence asset into the **Character Presence** field on `ConvoCoreSampleUI3D`.

### 4. Assign the remaining fields

Fill in the standard dialogue UI fields: dialogue text, speaker name, choice panel, and continue button.

---

## Character Persistence Across Lines

3D characters are persistent. Once a character appears in a conversation, they remain in the scene for the entire conversation. ConvoCore does not despawn a character just because they aren't listed on the current line.

When the runner processes a line:
- Characters on that line have their expressions updated via `ApplyExpression()`.
- Characters **not** on that line are left exactly as they are — their last applied expression remains active.

Despawning is a conversation-end operation. The presence handles it in `OnConversationEnd()`.

---

## No Slot System

`ConvoCoreSampleUI3D` does not have slot anchors, a slot list, or any concept of Left/Center/Right positioning. The presence is the only thing that decides where characters stand.

`DialogueLineDisplayOptions` fields like `CharacterPosition` and `SlotId` are ignored by the 3D UI. The `DisplayOptions` struct is available to the presence via `CharacterPresenceContext` if a presence wants to read flip or scale data, but placement is never driven by those fields.

---

## The CharacterPresenceContext

When `ConvoCoreSampleUI3D` calls `ResolvePresence()`, it passes a `CharacterPresenceContext` struct alongside the representation and spawner:

| Field | Type | Description |
|---|---|---|
| `CharacterIndex` | `int` | Zero-based index of this character in the current line's representation list. |
| `TotalCharacters` | `int` | Total number of characters on this line. |
| `DisplayOptions` | `DialogueLineDisplayOptions` | The line's display options, or `null` if none were set. |

Presences can use this context for any purpose — for example, `WorldPointPresence` uses `CharacterIndex` to select which authored world position to place the character at.

---

## ResolvePresence Returns Null

Some presences return `null` from `ResolvePresence()` — for example, `ExternalPresence` for a character that isn't registered in the scene registry. When this happens:

- The spawner is not called.
- No parenting occurs.
- No display component is bound.
- Expression application is skipped for that character, with a warning in the Console.

This is intentional and safe. The warning tells you which character was skipped and why, so you can decide whether to register the character or adjust the presence configuration.

---

## Scene-Resident Characters

If your characters are already in the scene, use `ExternalPresence` or `TransformLerpPresence` and register each character with `ConvoCoreSceneCharacterRegistry`.

1. Add **ConvoCoreSceneCharacterRegistry** to any GameObject in the scene.
2. Add **ConvoCoreSceneCharacterRegistrant** to the scene character's root GameObject.
3. Set the **Character Id** on the registrant to match the `SceneCharacterId` on the character's `PrefabCharacterRepresentationData`.
4. Set `CharacterSourceMode` to **Scene Resident** on the representation asset.
5. Drag the registry into the **Scene Character Registry** field on `ConvoCorePrefabRepresentationSpawner`.

Scene-resident characters are never spawned, pooled, or destroyed by the 3D UI.

---

## Next Steps

| I want to… | Go here |
|---|---|
| Understand which presence type fits your setup | [Presence Types →](presence-types) |
| Configure expression driving on a prefab | [Display Components →](display-components) |
| Use canvas-space prefab characters instead | [Canvas UI →](canvas-ui) |
