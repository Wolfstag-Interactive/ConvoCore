---
sidebar_position: 4
title: Presence Types
---

# Presence Types

A **presence** is a ScriptableObject that decides where 3D prefab characters are positioned during a conversation and how long they remain in the scene. The 3D UI calls into the presence at conversation start, per-line, and at conversation end — the presence handles the rest.

All presence types extend `ConvoCoreCharacterPresence`. ConvoCore ships with six built-in types covering the most common placement needs.

---

## Choosing a Presence

| Situation | Presence to use |
|---|---|
| Characters are already hand-placed in the scene | `ExternalPresence` |
| Characters should appear at specific authored world coordinates | `WorldPointPresence` |
| A character should follow a moving scene object | `FollowTargetPresence` |
| Character is positioned relative to the active camera | `CameraRelativePresence` |
| A scene character walks or turns to a spot at conversation start | `TransformLerpPresence` |
| A scene character with an Animator needs its parameters driven | `AnimatorPresence` |
| Different dialogue modes need different placement strategies | `SequencedPresence` |

---

## ExternalPresence

Characters are fully managed by the developer. ConvoCore does not spawn, position, move, or destroy anything.

**How it works:**
- `ResolvePresence()` looks up the character in `ConvoCoreSceneCharacterRegistry` by ID and returns the `IConvoCoreCharacterDisplay` component on that GameObject.
- If the character is not found in the registry, `ResolvePresence()` returns `null` and expression application is skipped for that character.
- `OnConversationEnd()` is a no-op — cleanup is entirely your responsibility.

**Inspector fields:** None beyond the base class.

**Use when:** characters are already standing in the scene, placed by the designer or spawned by your own systems, and you want ConvoCore to drive only their expressions.

:::warning
If you use `ExternalPresence`, every character in your conversation must be registered with `ConvoCoreSceneCharacterRegistry` at runtime before the conversation starts. If a character is missing, you'll see a warning and that character's expressions will be skipped silently.
:::

---

## WorldPointPresence

Characters are spawned at authored world positions when they first appear in a line. Positions are set in the inspector as a list of world-space `Vector3` and `Quaternion` pairs. Characters are cached after their first line — a character that appears on multiple lines is not re-spawned.

**How it works:**
- `OnConversationBegin()` creates lightweight marker `Transform` objects at each authored position.
- `ResolvePresence()` checks whether the character has already been spawned. If not, it spawns a prefab instance via the spawner and parents it under the marker at the index matching `CharacterPresenceContext.CharacterIndex`.
- On subsequent lines with the same character, the cached `IConvoCoreCharacterDisplay` is returned directly.
- `OnConversationEnd()` releases all spawned instances via the spawner and destroys the marker Transforms.

**Inspector fields:**

| Field | Description |
|---|---|
| **Spawn Points** | A list of world position and rotation entries. Index 0 is used for the first character on a line (`CharacterIndex == 0`), index 1 for the second, and so on. |

**Use when:** characters should appear at specific positions in the world that you can author in the ScriptableObject inspector — for example, two NPCs standing at a campfire, or a character who always appears in front of a doorway.

:::tip
You can author these positions in the inspector by typing world coordinates directly. If you need to set them from scene positions while working, temporarily use `TransformLerpPresence` or your own editor tooling to capture positions, then switch to `WorldPointPresence` once the coordinates are locked in.
:::

---

## FollowTargetPresence

A character is spawned at conversation start and follows a scene `Transform` for the duration of the conversation. The follow target is resolved via `ConvoCoreSceneCharacterRegistry` by ID.

**How it works:**
- `OnConversationBegin()` looks up the target Transform by registry ID and spawns the character prefab via the spawner.
- Each frame (or on a configurable interval), the spawned instance is moved to match the target's position and rotation.
- `OnConversationEnd()` releases the spawned instance.

**Inspector fields:**

| Field | Description |
|---|---|
| **Target Registry Id** | The registry ID of the scene object to follow. Must be registered with `ConvoCoreSceneCharacterRegistry`. |
| **Offset** | A local-space offset applied to the follow target's position. Useful for placing the character slightly ahead of or beside the target. |

**Use when:** a companion character should stay near the player during dialogue, or a character should appear attached to a moving vehicle or platform.

---

## CameraRelativePresence

A character is positioned at an authored offset from the active camera. Useful for first-person games or VR, where characters should always appear at a consistent position in front of the player regardless of where they are in the world.

**How it works:**
- `OnConversationBegin()` resolves the current camera (via `Camera.main`) and spawns the character prefab.
- The character's position is calculated from the camera's position and forward/right/up vectors using the authored offset values.
- Position can be recalculated once at conversation start or updated each frame, depending on the **Update Mode** setting.
- `OnConversationEnd()` releases the spawned instance.

**Inspector fields:**

| Field | Description |
|---|---|
| **Forward Distance** | How far in front of the camera the character appears, in metres. |
| **Lateral Offset** | Left/right offset from camera centre. Negative values place the character to the left. |
| **Height Offset** | Up/down offset from camera height. |
| **Update Mode** | `Once` — position is set at conversation start only. `EveryFrame` — position tracks the camera continuously. |

**Use when:** the game is first-person or VR, or whenever a character's screen position should stay fixed relative to the player's view rather than a world coordinate.

---

## AnimatorPresence

A scene-resident character with an Animator. The presence drives Animator parameters as part of the expression system, bridging ConvoCore's expression pipeline into an existing animation state machine.

**How it works:**
- `ResolvePresence()` looks up the character via `ConvoCoreSceneCharacterRegistry` and returns its `IConvoCoreCharacterDisplay` component.
- The display component on the character must be `ConvoCoreAnimatorDisplay` (or a compatible implementation) for Animator parameter driving to work.
- `OnConversationEnd()` is a no-op — the character's Animator state is left as-is.

**Inspector fields:** None beyond the base class. All Animator parameter configuration lives on the `ConvoCoreAnimatorDisplay` component on the character prefab.

**Use when:** characters are fully animated scene objects with existing Animator controllers, and you want ConvoCore to set Animator parameters in response to dialogue expression changes.

:::note
`AnimatorPresence` is effectively `ExternalPresence` with a documented expectation that the resolved display is `ConvoCoreAnimatorDisplay`. You could achieve the same result with `ExternalPresence`. The distinction is clarity of intent — use `AnimatorPresence` when the primary purpose of the presence is Animator parameter driving.
:::

---

## TransformLerpPresence

A scene-resident character moves to an authored offset position at conversation start and returns to their original position at conversation end. Useful for an NPC who turns to face the player or walks a few steps to a conversation spot.

**How it works:**
- `OnConversationBegin()` stores the character's current world position and rotation, then smoothly interpolates to the authored target position over a configurable duration.
- `ResolvePresence()` looks up the character via registry and returns its `IConvoCoreCharacterDisplay`.
- `OnConversationEnd()` interpolates the character back to their original position and rotation.

**Inspector fields:**

| Field | Description |
|---|---|
| **Character Registry Id** | The registry ID of the scene character to move. |
| **Target Position** | The world position to move to at conversation start. |
| **Target Rotation** | The world rotation to adopt at conversation start. |
| **Move Duration** | Time in seconds for the lerp, applied both at start and end of the conversation. |

**Use when:** an NPC should step forward or turn to face the player at conversation start, then return to their idle position when the conversation ends.

---

## SequencedPresence

Wraps a list of other presence assets and selects between them based on a conversation index or a named condition. Useful when the same scene has multiple dialogue modes with different placement needs — for example, most conversations use `ExternalPresence`, but a specific cutscene conversation uses `WorldPointPresence`.

**How it works:**
- At conversation start, `SequencedPresence` evaluates its selection condition and delegates all `OnConversationBegin()`, `ResolvePresence()`, and `OnConversationEnd()` calls to the selected sub-presence.
- The selected sub-presence operates exactly as if it were assigned directly.

**Inspector fields:**

| Field | Description |
|---|---|
| **Presences** | A list of `ConvoCoreCharacterPresence` assets to choose from. |
| **Selection Mode** | `ByIndex` — use a fixed index from the list. `ByCondition` — evaluate a string-keyed condition. |
| **Index** | The fixed list index to use when `Selection Mode` is `ByIndex`. |

**Use when:** the same character has different placement behaviour depending on which conversation is active, or when you want to swap presence types at runtime without changing the assignment on `ConvoCoreSampleUI3D`.

---

## Writing a Custom Presence

:::info[For Advanced Users]
Extend `ConvoCoreCharacterPresence` (a `ScriptableObject`) to create your own placement strategy:

```csharp
using WolfstagInteractive.ConvoCore;
using UnityEngine;

[CreateAssetMenu(menuName = "ConvoCore/Presence/My Custom Presence")]
public class MyCustomPresence : ConvoCoreCharacterPresence
{
    public override IConvoCoreCharacterDisplay ResolvePresence(
        PrefabCharacterRepresentationData representation,
        CharacterPresenceContext context,
        ConvoCorePrefabRepresentationSpawner spawner)
    {
        // Spawn, locate, or return a character display here.
        // Return null to skip expression application for this character.
        var instance = spawner.ResolveCharacter(representation, null, null, myTransform);
        return instance;
    }

    public override void OnConversationBegin()
    {
        // Optional. Set up scene objects, markers, or state here.
    }

    public override void OnConversationEnd()
    {
        // Optional. Release spawned instances, destroy markers, restore state here.
    }
}
```

Because presences are ScriptableObjects, they cannot hold direct references to scene objects. Use `ConvoCoreSceneCharacterRegistry` to resolve scene objects by ID at runtime, or store authored data (positions, offsets, IDs) as fields on the ScriptableObject and resolve live references inside `OnConversationBegin()`.
:::

---

## Next Steps

| I want to… | Go here |
|---|---|
| Set up the 3D UI to use a presence | [3D UI →](3d-ui) |
| Configure expression driving on a prefab | [Display Components →](display-components) |
