---
sidebar_position: 4
title: Variable Store
---

# Variable Store

`ConvoVariableStore` is a ScriptableObject that stores typed, scoped key-value pairs - a lightweight runtime database for gameplay state that dialogue can read and write. It is the bridge between what happens in a conversation and the rest of your game.

**Create via**: Right-click in the Project window → **Create → ConvoCore → Runtime → Variable Store**

One variable store asset can serve your entire project. Create additional stores only if you need strict isolation between unrelated systems (for example, a separate store for a mini-game).

---

## Variable scopes

Every variable has a scope that determines how it is persisted:

| Scope | Persisted? | Where saved | Description |
|---|---|---|---|
| **Global** | Yes | `ConvoCoreGameSnapshot.GlobalVariables` | Shared across all conversations. Use for player-wide state: quest progress, relationship values, story flags that span scenes. |
| **Conversation** | Yes | Inside each `ConversationSnapshot.Variables` | Belongs to one conversation. Use for per-NPC state: whether the player was rude, which branch they took, how many times they have spoken to this character. |
| **Session** | No | Memory only | Reset when the application closes or Play Mode exits. Never written to disk. Use for temporary flags that exist only within a single play session. |

:::note
Think of **Global** as your save file's top-level entries - "has the player freed the village?". **Conversation** scope is per-NPC state - "did the player choose the aggressive option with this merchant?". **Session** scope is for scratch variables - counters, temporary flags, or UI state that is meaningless after a restart.
:::

---

## Variable types

Variables are strongly typed. Supported types:

| Enum value | C# type | Inspector label |
|---|---|---|
| `ConvoCoreVariableType.Bool` | `bool` | Bool |
| `ConvoCoreVariableType.Int` | `int` | Int |
| `ConvoCoreVariableType.Float` | `float` | Float |
| `ConvoCoreVariableType.String` | `string` | String |

Attempting to read a variable as the wrong type returns the default value for that type (e.g. `0` for Int, `false` for Bool) rather than throwing. Use `TryGet` methods to distinguish between "variable not found" and "variable has the zero value".

---

## Writing variables

```csharp
using WolfstagInteractive.ConvoCore.SaveSystem;
using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    [SerializeField] private ConvoVariableStore _store;

    public void StartQuest()
    {
        _store.SetBool("quest_started", true, ConvoVariableScope.Global);
        _store.SetInt("quest_step", 1, ConvoVariableScope.Global);
        _store.SetString("quest_giver", "Elder Morin", ConvoVariableScope.Global);
    }

    public void RecordDialogueChoice(string key, string choiceValue)
    {
        _store.SetString(key, choiceValue, ConvoVariableScope.Conversation);
    }

    public void SetSessionFlag(string key)
    {
        _store.SetBool(key, true, ConvoVariableScope.Session);
    }

    public void AddGold(int amount)
    {
        _store.TryGetInt("player_gold", out int current);
        _store.SetInt("player_gold", current + amount, ConvoVariableScope.Global);
    }
}
```

All `Set` methods overwrite any existing value for that key. If the key does not exist, a new entry is created at runtime in `_sessionEntries`. To create entries that are persisted and visible in the inspector, declare them in `_persistentEntries` (see [Inspector declaration](#declaring-variables-in-the-inspector)).

---

## Reading variables

```csharp
// TryGet - returns false if the variable does not exist.
// Use this when the variable might not have been set yet.
if (_store.TryGetBool("quest_started", out bool questStarted))
{
    Debug.Log($"Quest started: {questStarted}");
}
else
{
    Debug.Log("quest_started has not been set.");
}

if (_store.TryGetInt("player_gold", out int gold))
{
    Debug.Log($"Player gold: {gold}");
}

if (_store.TryGetFloat("elapsed_time", out float elapsed))
{
    Debug.Log($"Elapsed time: {elapsed:F1}s");
}

if (_store.TryGetString("last_choice", out string choice))
{
    Debug.Log($"Last choice: {choice}");
}

// Direct access - retrieves the raw ConvoCoreVariable entry.
// Prefer TryGet for gameplay code; use this when you need the full entry metadata.
ConvoCoreVariable variable = _store.GetVariable("player_gold");
int directGold = variable.GetInt();
```

:::warning
`GetVariable()` throws a `KeyNotFoundException` if the variable does not exist. Always use the `TryGet` variants in gameplay code unless you have pre-declared the variable and are certain it will be present.
:::

---

## Checking existence

```csharp
bool exists = _store.HasVariable("quest_started");

if (exists)
{
    // Safe to call GetVariable directly
}
```

---

## Querying by scope or tag

```csharp
// Get all variables in a specific scope
IEnumerable<ConvoCoreVariable> globals = _store.GetByScope(ConvoVariableScope.Global);
IEnumerable<ConvoCoreVariable> convVars = _store.GetByScope(ConvoVariableScope.Conversation);

// Get all variables that have a specific tag
IEnumerable<ConvoCoreVariable> questVars = _store.GetByTag("quest");
IEnumerable<ConvoCoreVariable> npcVars = _store.GetByTag("npc_state");

// Combine: all global quest variables
var globalQuestVars = _store.GetByScope(ConvoVariableScope.Global)
    .Where(v => v.Tags.Contains("quest"));
```

Tags are defined per-variable in the inspector (see [Inspector declaration](#declaring-variables-in-the-inspector)).

---

## Listening for changes

Subscribe to be notified when a specific variable changes, or when any variable changes:

```csharp
private void OnEnable()
{
    // Listen to a specific key
    _store.Listen("player_gold", OnGoldChanged);

    // Listen to all changes
    _store.OnVariableChanged += OnAnyVariableChanged;
}

private void OnDisable()
{
    _store.Unlisten("player_gold", OnGoldChanged);
    _store.OnVariableChanged -= OnAnyVariableChanged;
}

private void OnGoldChanged(string key, object oldValue, object newValue)
{
    int oldGold = (int)oldValue;
    int newGold = (int)newValue;
    _goldDisplay.text = newGold.ToString();
}

private void OnAnyVariableChanged(string key, object oldValue, object newValue)
{
    Debug.Log($"[VariableStore] {key}: {oldValue} → {newValue}");
}
```

:::tip
Use `Listen` / `Unlisten` for targeted bindings (e.g. a UI element that displays one variable). Use `OnVariableChanged` for broad listeners like debug overlays or analytics. Always unsubscribe in `OnDisable` to avoid memory leaks when objects are destroyed.
:::

---

## Declaring variables in the inspector

Variables can be pre-declared in the **Variable Store** inspector under `_persistentEntries`. Each entry has:

| Field | Description |
|---|---|
| **Key** | The variable name. Must be unique within the store. |
| **Type** | `Bool`, `Int`, `Float`, or `String`. |
| **Default Value** | The authored starting value for a new game. |
| **Scope** | `Global` or `Conversation`. Session-scoped variables cannot be pre-declared. |
| **Description** | Optional notes for your team. Not used at runtime. |
| **Tags** | String tags used with `GetByTag()`. |
| **IsReadOnly** | Prevents runtime writes. Read attempts work normally; write attempts log a warning and do nothing. |

Pre-declared variables appear in the inspector during Play Mode with their current runtime value shown next to the authored default.

:::warning
The authored defaults in `_persistentEntries` represent the **starting state for a new game**. They are not updated by the save system - they are the baseline. At runtime, writes go to `_sessionEntries` (the in-memory layer). When the save system loads a slot, it restores the saved values on top of the authored defaults. If you exit Play Mode and re-enter without loading a save, values reset to their authored defaults.
:::

---

## Internal storage model

The variable store uses two internal dictionaries:

| Dictionary | Access | When cleared |
|---|---|---|
| `_persistentEntries` | Serialized field; authored in the inspector | Only when the asset is reimported or manually edited. |
| `_sessionEntries` | `[NonSerialized]`; created lazily | Every time Play Mode exits or the application closes. |

When reading a variable, the store checks `_sessionEntries` first, then falls back to `_persistentEntries`. When writing, the value always goes into `_sessionEntries`. This ensures that authored defaults in `_persistentEntries` are never modified at runtime, even in the editor.

:::info[For Advanced Users]
The variable store editor tracks a **snapshot of authored defaults** captured when Unity exits Edit Mode. During Play Mode, any variable whose current runtime value differs from its authored default is highlighted in orange in the inspector. This **live diff** makes it easy to see at a glance which variables have been touched during a test playthrough - without running a separate debug overlay.

You can also use the editor's **scope filter** and **text filter toolbar** to quickly find variables in large stores. The editor repaints at 0.1-second intervals during Play Mode so the live diff stays current without requiring manual inspector focus.
:::

---

## Clearing variables

```csharp
// Clear all session-layer entries (does not affect authored persistent entries)
_store.ClearSessionVariables();

// Clear all variables of a specific scope from the session layer
_store.ClearByScope(ConvoVariableScope.Conversation);

// Reset a single variable to its authored default (or remove it if not pre-declared)
_store.ResetVariable("quest_step");
```

These are useful during scene transitions or when starting a new game - clear Conversation-scoped variables between conversations, or clear all session variables on "New Game".
