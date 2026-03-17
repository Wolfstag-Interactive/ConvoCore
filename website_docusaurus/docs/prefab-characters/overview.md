---
sidebar_position: 1
title: Overview
---

# Prefab Characters

ConvoCore supports 3D prefab characters in addition to flat sprites. Where a sprite representation stores a set of images, a prefab representation stores a reference to a Unity prefab — a real GameObject that can be spawned into a scene, driven by an Animator, and fully integrated into your world.

This section covers the full workflow: how prefab characters are placed, how they receive expressions, and how to choose between the two display modes ConvoCore provides.

---

## The Two Paths

Prefab characters work differently depending on whether your dialogue exists in **canvas space** or **world space**. ConvoCore ships with a dedicated UI class for each case.

```
Your Conversation
       │
       ├─── 2D / Canvas ───▶  ConvoCoreSampleUICanvas
       │                           Slot anchors on a Canvas
       │                           Sprites and prefabs side-by-side
       │                           Fixed screen-space positions
       │
       └─── 3D / World ────▶  ConvoCoreSampleUI3D
                                   ConvoCoreCharacterPresence asset
                                   Characters live in the scene
                                   Placement is the presence's job
```

### Canvas path

Characters sit inside a Canvas. Each character slot is a `RectTransform` anchor point that the spawner parents prefab instances under. Sprite and prefab characters can appear on the same line without any extra configuration — the canvas UI handles both simultaneously.

**Best for:** visual novel layouts, 2D games, UI-layer portraits, any setup where characters are screen-space elements.

### 3D world path

Characters are placed in the 3D scene. A **presence** ScriptableObject decides where each character goes — at authored world positions, relative to the camera, following a target, or already standing in the scene. The UI itself has no slot concept; it delegates placement entirely to the presence.

**Best for:** RPG dialogue, cinematic scenes, first-person games, VR, any setup where characters exist as world objects.

---

## Choosing a Path

| Situation | Use |
|---|---|
| Characters rendered inside a Canvas | `ConvoCoreSampleUICanvas` |
| Characters exist as GameObjects in the scene | `ConvoCoreSampleUI3D` |
| Mixing sprite portraits and prefab characters on the same line | `ConvoCoreSampleUICanvas` |
| Characters need to move to specific world positions | `ConvoCoreSampleUI3D` + `WorldPointPresence` |
| Characters follow the player | `ConvoCoreSampleUI3D` + `FollowTargetPresence` |
| Characters are already placed in the scene by the developer | `ConvoCoreSampleUI3D` + `ExternalPresence` |
| First-person or VR — character placed relative to the camera | `ConvoCoreSampleUI3D` + `CameraRelativePresence` |

:::note
Both paths use the same `PrefabCharacterRepresentationData` asset and the same display components (`ConvoCoreAnimatorDisplay`, `ConvoCoreBlendShapeDisplay`, etc.). What differs is how the spawned character gets positioned in the world.
:::

---

## Key Components

### Shared across both paths

| Component | What it does |
|---|---|
| `PrefabCharacterRepresentationData` | ScriptableObject. Holds the prefab reference and expression mappings. |
| `ConvoCorePrefabRepresentationSpawner` | MonoBehaviour. Spawns, pools, and resolves prefab instances. One per UI. |
| `ConvoCorePrefabPool` | MonoBehaviour. Object pool for prefab instances. Managed by the spawner. |
| `ConvoCoreSceneCharacterRegistry` | MonoBehaviour. Registers scene-resident characters by ID so the spawner can find them without spawning anything. |
| `ConvoCoreSceneCharacterRegistrant` | MonoBehaviour. Drop on any scene character to register it with the registry. |

### Canvas path only

| Component | What it does |
|---|---|
| `ConvoCoreSampleUICanvas` | The full canvas UI: text, choices, sprite slots, and prefab slot anchors. |

### 3D path only

| Component | What it does |
|---|---|
| `ConvoCoreSampleUI3D` | The world-space UI: text and choices only. Delegates character placement to the presence. |
| `ConvoCoreCharacterPresence` | Abstract ScriptableObject base class. Subclass determines where and how characters appear. |

### On the character prefab

| Component | What it does |
|---|---|
| `ConvoCoreAnimatorDisplay` | Drives Animator parameters in response to expressions. Most common 3D case. |
| `ConvoCoreBlendShapeDisplay` | Drives `SkinnedMeshRenderer` blend shape weights in response to expressions. |
| `ConvoCoreActionOnlyDisplay` | Passthrough. Runs `BaseExpressionAction` ScriptableObjects only; adds no built-in visual change. |
| `ConvoCoreSimpleFade` | Implements fade-in and fade-out on a `Renderer` or `CanvasGroup`. Drop-on, configure duration. |

---

## How Expression Application Works

Regardless of which path you use, the expression application sequence is the same:

1. The ConvoCore runner processes a dialogue line and determines which characters are present.
2. The UI calls `spawner.ResolveCharacter()` (canvas) or `presence.ResolvePresence()` (3D) to obtain an `IConvoCoreCharacterDisplay` for each character.
3. The UI calls `display.BindRepresentation(representationAsset)` on the returned display. The display builds an internal lookup table keyed by expression display name at this point.
4. The UI calls `display.ApplyExpression(expressionId)` with the GUID of the expression selected in the YAML line.
5. The display component translates the GUID into a concrete visual change: an Animator parameter, a blend shape weight, or nothing (if `ConvoCoreActionOnlyDisplay`).
6. `PrefabCharacterRepresentationData.ApplyExpression()` also runs, which fires any `BaseExpressionAction` ScriptableObjects attached to the expression mapping. Display component changes and ScriptableObject actions run together.

:::note
Expression display names are used as the join key between the `PrefabCharacterRepresentationData` asset and the display component's inspector mappings. The GUID is only used at runtime lookup. This means you configure display components by typing in the same human-readable name you gave the expression in the representation asset — not by copying an opaque GUID string.
:::

---

## Next Steps

| I want to… | Go here |
|---|---|
| Set up canvas-space prefab characters | [Canvas UI →](canvas-ui) |
| Set up world-space prefab characters | [3D UI →](3d-ui) |
| Understand presence types and when to use each | [Presence Types →](presence-types) |
| Configure expression driving on a prefab | [Display Components →](display-components) |
