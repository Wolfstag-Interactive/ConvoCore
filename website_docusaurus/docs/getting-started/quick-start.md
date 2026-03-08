---
sidebar_position: 2
title: Quick Start
---

# Quick Start

This guide walks you through creating a simple "Hello World" conversation from scratch. By the end you will have a fully wired ConvoCore setup running in a Unity scene — with console log output confirming that lines are advancing correctly. Adding a visible UI is covered in the [UI Foundation](../ui/ui-foundation) page; this guide focuses on getting the core logic working first.

**Time to complete: approximately 10 minutes.**

---

## Step 1: Create a YAML file

Right-click anywhere in the **Project** panel → **Create → Text Asset**. Name the new file `MyFirstConversation.yml`.

Open the file (double-click to open in your script editor) and replace its contents with the following:

```yaml
MyFirstConversation:
  - CharacterID: "Narrator"
    LocalizedDialogue:
      EN: "Hello! This is your first ConvoCore conversation."
  - CharacterID: "Narrator"
    LocalizedDialogue:
      EN: "Press any key to advance to the next line."
  - CharacterID: "Narrator"
    LocalizedDialogue:
      EN: "That's it! You've finished the conversation."
```

Save the file. Unity will detect it and add it to the project as a `TextAsset`.

:::note
**What is YAML?** YAML ("YAML Ain't Markup Language") is a human-readable plain-text format commonly used for configuration and data files. ConvoCore uses YAML as its **single source of truth** for dialogue — you write your lines in YAML, and ConvoCore compiles them into Unity assets. The indentation in YAML is significant: use two spaces per indent level (not tabs).
:::

---

## Step 2: Create a Character Profile

Right-click in the **Project** panel → **Create → ConvoCore → Character Profile**. Name the new asset `Narrator`.

Select the asset and look at its **Inspector**:

- Set **Character ID** to `Narrator`.
- Set **Character Name** to `Narrator` (this is the display name shown in UI; it can differ from the ID, but for this tutorial keep them the same).

Leave all other fields at their defaults for now.

:::tip
**Character ID is case-sensitive** and must exactly match the `CharacterID` value in your YAML. If your YAML says `"Narrator"` but the profile says `"narrator"`, ConvoCore will not be able to link them and will log a warning at parse time. Always copy-paste the ID rather than typing it twice.
:::

---

## Step 3: Create a Conversation Data asset

Right-click in the **Project** panel → **Create → ConvoCore → Conversation Dialogue Object**. Name the new asset `MyFirstConversation`.

Select the asset and configure it in the **Inspector**:

1. **Conversation Key** — Set this to `MyFirstConversation`. This must exactly match the root key in your YAML file (the first line, `MyFirstConversation:`).

2. **Conversation Yaml** — Drag your `MyFirstConversation.yml` TextAsset from the Project panel into this field.

3. **Participant Profiles** — Click the **+** button on the Participant Profiles list and drag the `Narrator` Character Profile asset into the new slot.

4. **Parse the YAML** — Right-click the `MyFirstConversation` asset in the Project panel and select **Force Validate Dialogue Lines** from the context menu. ConvoCore will parse the YAML and populate the compiled dialogue data inside the asset. Check the Console for any parse warnings.

:::note
**What is a ScriptableObject?** In Unity, a ScriptableObject is a data asset stored as a file in your project — similar to a spreadsheet or config file that you can edit visually in the Inspector. The Conversation Data asset is a ScriptableObject that holds the compiled version of your YAML: the list of participants, the ordered dialogue lines, localized text, and metadata. You never need to edit the compiled data by hand; always edit the YAML and re-validate.
:::

:::warning
If you skip the **Force Validate Dialogue Lines** step, the Conversation Data asset will be empty at runtime and no lines will play. If your conversation silently does nothing when you press Play, this is the first thing to check.
:::

---

## Step 4: Add ConvoCore to the scene

Open (or create) the scene you want to test in.

1. In the **Hierarchy** panel, right-click → **Create Empty**. Rename the new GameObject to `DialogueRunner`.

2. With `DialogueRunner` selected, click **Add Component** in the Inspector. Search for **ConvoCore** and add the `ConvoCore` component.

3. In the ConvoCore component Inspector:
   - Find the **Input Mode** (or **Conversation Input**) field and set it to **Single Conversation**.
   - A **Conversation** field will appear. Drag your `MyFirstConversation` Conversation Data asset into it.

:::note
**What is a MonoBehaviour?** A MonoBehaviour is a C# script you attach to a GameObject to give it behaviour — like adding an engine to a car. The `ConvoCore` component is a MonoBehaviour that manages conversation state: which line is current, when to advance, which character is speaking, and when the conversation ends. It runs the logic but does not display anything itself.
:::

---

## Step 5: Call StartConversation

For this tutorial, you will start the conversation automatically when the scene loads. In a real project you would trigger it from a collider, button, or cutscene event.

1. In the **Hierarchy**, right-click → **Create Empty**. Rename the new GameObject to `ConvoStarter`.

2. In the Inspector for `ConvoStarter`, click **Add Component → New Script**. Name the script `ConvoStarter` and click **Create and Add**.

3. Open `ConvoStarter.cs` in your editor and replace its contents with:

```csharp
using UnityEngine;
using WolfstagInteractive.ConvoCore;

public class ConvoStarter : MonoBehaviour
{
    [SerializeField] private ConvoCore _runner;

    private void Start()
    {
        _runner.StartConversation();
    }
}
```

4. Save the script. Back in Unity, select the `ConvoStarter` GameObject. In the Inspector, drag the `DialogueRunner` GameObject into the **Runner** field that has appeared on the `ConvoStarter` component.

:::tip
For production use, wire `StartConversation()` to a **UnityEvent** instead — for example, from a trigger collider's `OnTriggerEnter`, a UI button's `onClick` event, or a timeline signal. Calling it from `Start()` works for testing, but it fires before your scene has fully settled (e.g., before any fade-in or camera transition completes).
:::

---

## Step 6: Press Play and check the Console

Press **Play**. The conversation will start, but nothing will appear on screen yet — ConvoCore is headless by design and does not include a built-in text display.

Open the **Console** (Window → General → Console). You should see log messages from ConvoCore confirming that lines are being processed and advanced. A successful run looks something like:

```
[ConvoCore] Conversation started: MyFirstConversation
[ConvoCore] Line 0 — Narrator: "Hello! This is your first ConvoCore conversation."
[ConvoCore] Line 1 — Narrator: "Press any key to advance to the next line."
[ConvoCore] Line 2 — Narrator: "That's it! You've finished the conversation."
[ConvoCore] Conversation completed: MyFirstConversation
```

:::note
**ConvoCore is headless by design.** It runs the conversation logic and fires C# events at each stage (line started, line completed, conversation ended), but displaying text, portraits, or choice buttons is entirely up to your custom UI layer. This means you can use any UI system — Unity UI (uGUI), UI Toolkit, TextMeshPro, or a fully custom renderer — without ConvoCore knowing or caring. The [UI Foundation](../ui/ui-foundation) page explains how to subscribe to ConvoCore's events and build a display layer.
:::

If you see errors instead of log output, the most common causes are:

- **"Conversation data is null"** — The Conversation asset was not assigned to the ConvoCore component. Double-check Step 4.
- **"No lines found"** — The YAML was not validated after changes. Re-run **Force Validate Dialogue Lines** (Step 3, item 4).
- **"Character ID not found"** — The CharacterID in the YAML does not match any Character Profile. Check case and spelling (Step 2).

---

## Step 7: Next steps

You now have a working ConvoCore setup. Here is where to go next:

| I want to… | Go here |
|---|---|
| Understand all the YAML options (branches, choices, expressions) | [YAML Format →](../yaml-reference/yaml-format) |
| Display dialogue text and portraits on screen | [UI Foundation →](../ui/ui-foundation) |
| Learn about the ConvoCore component in depth | [ConvoCore Component →](../core-systems/convocore-component) |
| Add branching choices | [Player Choices →](../yaml-reference/player-choices) |
| Save conversation progress between sessions | [Save System →](../save-system/save-system-overview) |
