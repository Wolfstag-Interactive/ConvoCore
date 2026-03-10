---
sidebar_position: 11
title: Extending ConvoCore
---

# Extending ConvoCore

ConvoCore is built top-to-bottom for extensibility. Every layer -- dialogue actions, character visuals, UI display, and save storage -- uses abstract base classes or interfaces that you replace or extend without touching the core package. This page maps every extension point and links to the detailed guide for each one.

---

## Why This Matters

Out of the box, ConvoCore plays dialogue. What it does *with* that dialogue -- what appears on screen, which game systems react, how character visuals update -- is entirely your code. This is intentional: ConvoCore does not know or care whether you are making a visual novel, a 3D RPG cutscene, an interactive museum exhibit, or a procedurally generated story. The extension points below are where your game logic plugs in.

---

## Extension Points at a Glance

| Extension | What you replace or extend | Docs |
|---|---|---|
| **Dialogue actions** | Before/after hooks on individual lines -- trigger any game logic | [Custom Actions](dialogue-actions/custom-actions) |
| **Character representations** | How characters look -- sprites, prefabs, Spine, VRM, or any visual system | [Character Representations](characters/character-representations) |
| **UI layer** | The entire dialogue display -- text boxes, portraits, choice buttons, 3D panels | [Building a Custom UI](ui/building-a-ui) |
| **Save provider** | Where save data is stored -- JSON file, YAML, cloud, PlayerPrefs, encrypted | [Save Providers](save-system/save-providers) |
| **Expression actions** | Per-expression logic -- update an animator, blendshapes, or shader when an emotion changes | [Custom Actions](dialogue-actions/custom-actions#extending-baseexpressionaction) |

---

## Dialogue Actions -- Before and After Every Line

Dialogue actions are ScriptableObject assets that fire custom coroutines before a line is displayed or after the player advances past it. This is how you connect dialogue to the rest of your game.

**Before-line actions** run before the text appears. Use them to:
- Move the camera to a new shot.
- Spawn or enable a character in the scene.
- Fade in a portrait.
- Set a quest flag.

**After-line actions** run after the player advances. Use them to:
- Trigger an animation.
- Award an item.
- Send an analytics event.
- Transition to a cutscene.

A single action asset can be reused across dozens of conversations. Create an `EnableNPC` action once, configure it per-scene, and assign it wherever you need an NPC to appear mid-dialogue.

```csharp
[CreateAssetMenu(menuName = "ConvoCore/Actions/My Custom Action")]
public class MyCustomAction : BaseDialogueLineAction
{
    [SerializeField] private GameObject _target;

    protected override IEnumerator ExecuteLineAction()
    {
        _target.SetActive(true);
        yield return new WaitForSeconds(0.5f);
    }

    protected override IEnumerator ExecuteOnReversedLineAction()
    {
        _target.SetActive(false);
        yield return null;
    }
}
```

[Full guide: Custom Actions](dialogue-actions/custom-actions)

---

## Character Representations -- Any Visual System

The built-in representation types (Sprite and Prefab) cover most 2D and 3D setups. For anything else -- Spine animations, VRM avatars, VTuber rigs, dynamic texture systems, or fully procedural characters -- extend `CharacterRepresentationBase`:

```csharp
[CreateAssetMenu(menuName = "ConvoCore/Character Representation/My Representation")]
public class MyRepresentationData : CharacterRepresentationBase
{
    public override void ApplyExpression(
        string expressionId,
        ConvoCore runner,
        ConvoCoreConversationData data,
        int lineIndex,
        ConvoCoreCharacterDisplayBase display)
    {
        // Apply the correct visual state for this expression.
        // For example: trigger an animation, update a material, swap a texture.
    }
    // ...
}
```

One profile can hold multiple representation variants -- a character can have a `"Default"` sprite set, an `"Armored"` sprite set, and a `"3D Prefab"` variant all living in the same profile.

[Full guide: Character Representations](characters/character-representations)

---

## UI Layer -- Build Any Display

`ConvoCoreUIFoundation` is an abstract MonoBehaviour with six virtual methods. Override the ones you need and ConvoCore calls them at the right moment:

| Method | When ConvoCore calls it |
|---|---|
| `InitializeUI(runner)` | Once when the conversation starts |
| `UpdateDialogueUI(line, text, speaker, representation, profile)` | Every time a new line is ready to display |
| `WaitForUserInput()` | Coroutine -- must block until the player advances |
| `PresentChoices(options, labels, result)` | Coroutine -- display choice buttons, write selection to `result.SelectedIndex` |
| `HideDialogue()` | When the conversation ends |
| `UpdateForLanguageChange(text, code)` | When the player switches language mid-conversation |

You can use Unity UI (uGUI), UI Toolkit, TextMeshPro, a custom renderer, a 3D world-space panel, or any combination. ConvoCore does not know or care what system you use.

[Full guide: Building a Custom UI](ui/building-a-ui) | [Sample UI](ui/sample-ui)

---

## Save Provider -- Store Data Anywhere

The save system ships with JSON and YAML file providers. For cloud saves, PlayerPrefs, or encrypted storage, implement `IConvoSaveProvider`:

```csharp
public class MyCloudSaveProvider : IConvoSaveProvider
{
    public ConvoCoreGameSnapshot Load(string slot) { /* ... */ }
    public void Save(string slot, ConvoCoreGameSnapshot snapshot) { /* ... */ }
    public void Delete(string slot) { /* ... */ }
    public bool SlotExists(string slot) { /* ... */ }
    public List<string> GetAllSlots() { /* ... */ }
}
```

Assign your custom provider to the `ConvoCoreSaveManager` asset in the Inspector.

[Full guide: Save Providers](save-system/save-providers)

---

## Expression Actions -- React to Emotion Changes

For logic that should trigger specifically when a character's expression changes -- updating an animator, blending facial shapes, switching materials -- extend `BaseExpressionAction` instead of `BaseDialogueLineAction`. It is synchronous (no coroutine) and receives the full expression context:

```csharp
[CreateAssetMenu(menuName = "ConvoCore/Expressions/My Expression Action")]
public class MyExpressionAction : BaseExpressionAction
{
    public override void ExecuteAction(ExpressionActionContext context)
    {
        // context.Representation -- the character's representation
        // context.ExpressionId   -- the GUID of the expression being applied
        // context.Runtime        -- the ConvoCore runner
    }
}
```

[Full guide: Custom Actions](dialogue-actions/custom-actions#extending-baseexpressionaction)

---

## Summary

ConvoCore's extension model means you are never fighting the framework. Add a custom action when you need a new dialogue trigger. Swap the UI when your game's visual style changes. Plug in a cloud save provider without modifying any ConvoCore code. Every layer is a seam, not a wall.
