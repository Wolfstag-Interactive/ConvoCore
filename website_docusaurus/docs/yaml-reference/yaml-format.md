---
sidebar_position: 2
title: YAML Format
---

# YAML Format

This page is the complete field reference for ConvoCore's YAML dialogue format. Every supported field is documented here with an explanation of its purpose, whether it is required, and examples showing correct usage.

---

## Root structure

A ConvoCore YAML file contains one or more conversations. Each conversation is a **top-level key** — the Conversation Key — mapped to a list of dialogue lines. The Conversation Key is the identifier that links this YAML block to a `ConvoCoreConversationData` asset in Unity.

```yaml
MyConversation:
  - CharacterID: "Narrator"
    LocalizedDialogue:
      EN: "Hello world."
```

In this example, `MyConversation` is the Conversation Key. The `-` that opens the next line starts a list entry (a single dialogue line). Each dialogue line is an indented block of fields.

You can define multiple conversations in one file by adding additional top-level keys:

```yaml
MorningGreeting:
  - CharacterID: "NPC"
    LocalizedDialogue:
      EN: "Good morning!"

EveningGreeting:
  - CharacterID: "NPC"
    LocalizedDialogue:
      EN: "Good evening!"
```

Each conversation key must have a corresponding `ConvoCoreConversationData` asset in your Unity project with its **Conversation Key** field set to that exact string.

---

## Fields per dialogue line

### CharacterID

**Required.**

The identifier of the character speaking this line. This value must exactly match the `CharacterID` field on the corresponding `ConvoCoreCharacterProfileBaseData` asset.

```yaml
- CharacterID: "Guard"
  LocalizedDialogue:
    EN: "Halt! Who goes there?"
```

:::warning
`CharacterID` is case-sensitive. `"guard"`, `"Guard"`, and `"GUARD"` are three different identifiers. If ConvoCore cannot find a character profile matching the ID on a line, it will log a warning and attempt to continue with no character display. Always copy the CharacterID exactly from the character profile asset.
:::

---

### LineID

**Automatically generated. Do not write these manually.**

A `LineID` is a stable, unique identifier for each dialogue line within its conversation. ConvoCore uses LineIDs to track which lines the player has visited and where they left off — essential for save/restore to work correctly even after you add, remove, or reorder lines in the YAML.

**LineIDs are generated for you automatically.** When you link a YAML file to a `ConvoCoreConversationData` asset (by assigning it in the inspector and running validation), ConvoCore reads your YAML, assigns a unique ID to every line that doesn't already have one, and writes them back into the YAML file. You will see them appear in your source file after the first import:

```yaml
- CharacterID: "Guard"
  LineID: "a1b2c3d4"    # ← written by ConvoCore automatically
  LocalizedDialogue:
    EN: "Halt! Who goes there?"
```

Once a LineID has been generated for a line, it is stable for the lifetime of that line. You can freely add new lines, remove other lines, or reorder the conversation — the existing IDs do not change, so saved progress remains valid.

:::warning
Do not edit or delete a LineID that ConvoCore has written. Changing an ID is equivalent to removing the old line and adding a new one — any save data referencing the old ID will no longer match and the player's progress for that line will be lost. Treat auto-generated LineIDs as read-only.
:::

:::note
If you delete a line from the YAML, its LineID disappears with it. Save data that referenced that line will gracefully fall back to the nearest valid line. This is expected behaviour when you intentionally remove dialogue.
:::

---

### LocalizedDialogue

**Required.**

A map of language codes to display strings. The language code keys must match the codes registered in your `ConvoCoreSettings` asset, but matching is case-insensitive at runtime — `EN`, `en`, and `En` all resolve correctly.

At least one language key must be present. If the player's currently active language has no entry for a line, ConvoCore falls back to the first available language and logs a warning.

**Single-language example:**
```yaml
- CharacterID: "Narrator"
  LocalizedDialogue:
    EN: "The kingdom fell silent."
```

**Multi-language example:**
```yaml
- CharacterID: "Guard"
  LocalizedDialogue:
    EN: "Halt! Who goes there?"
    FR: "Halte ! Qui va là ?"
    ES: "¡Alto! ¿Quién va?"
    DE: "Halt! Wer geht da?"
```

---

## Complete minimal example

This is what you write:

```yaml
TownSquare:
  - CharacterID: "Guard"
    LocalizedDialogue:
      EN: "Halt! Who goes there?"
  - CharacterID: "Player"
    LocalizedDialogue:
      EN: "It's just me, passing through."
  - CharacterID: "Guard"
    LocalizedDialogue:
      EN: "Move along, then."
```

After linking this file to a `ConvoCoreConversationData` asset and running validation, ConvoCore writes the LineIDs back into the file automatically:

```yaml
TownSquare:
  - CharacterID: "Guard"
    LineID: "a1b2c3d4"    # written by ConvoCore — do not edit
    LocalizedDialogue:
      EN: "Halt! Who goes there?"
  - CharacterID: "Player"
    LineID: "e5f6a7b8"    # written by ConvoCore — do not edit
    LocalizedDialogue:
      EN: "It's just me, passing through."
  - CharacterID: "Guard"
    LineID: "c9d0e1f2"    # written by ConvoCore — do not edit
    LocalizedDialogue:
      EN: "Move along, then."
```

---

## Special characters

### Apostrophes

Apostrophes inside single-quoted YAML strings will break parsing because YAML uses the single quote as the string delimiter. There are two reliable ways to handle dialogue text that contains apostrophes.

**Use double quotes (recommended):**
```yaml
# Safe — double quotes let apostrophes appear freely
LocalizedDialogue:
  EN: "It's a trap!"
```

**Escape the apostrophe inside single-quoted strings:**
```yaml
# Also valid — double the apostrophe to escape it in single-quoted context
LocalizedDialogue:
  EN: 'It''s a trap!'
```

**This will cause a parse error:**
```yaml
# Do not do this
LocalizedDialogue:
  EN: 'It's a trap!'
```

ConvoCore includes a pre-processor that auto-wraps many common apostrophe cases before handing the YAML to the parser, but relying on that behavior is not recommended. Writing double-quoted values consistently is the safest and most explicit approach.

---

### Multi-line dialogue text

For long lines of dialogue that wrap across multiple source lines, use YAML's literal block scalar (`|`) or folded block scalar (`>`).

**Literal block scalar** (`|`) — preserves newlines exactly:
```yaml
- CharacterID: "Narrator"
  LineID: "narrator_prologue"
  LocalizedDialogue:
    EN: |
      Long ago, in a kingdom forgotten by time,
      a hero rose from the ashes of a fallen empire.
```

**Folded block scalar** (`>`) — joins lines with spaces, keeps paragraph breaks:
```yaml
- CharacterID: "Elder"
  LineID: "elder_warning"
  LocalizedDialogue:
    EN: >
      This path is treacherous. Many have tried
      and none have returned. I urge you to reconsider.
```

:::tip
Prefer the folded scalar (`>`) for dialogue that your UI will word-wrap automatically. Use the literal scalar (`|`) when you need explicit line breaks — for example, poetry, in-game letters, or UI tooltips where you control the layout.
:::

---

### The `{PlayerName}` placeholder

Write `{PlayerName}` anywhere inside a dialogue string. At runtime, ConvoCore replaces it with the `CharacterName` from the character profile asset that has the `IsPlayerCharacter` flag checked.

```yaml
- CharacterID: "Innkeeper"
  LineID: "innkeeper_welcome"
  LocalizedDialogue:
    EN: "Welcome back, {PlayerName}! Your usual room is ready."
    FR: "Bienvenue, {PlayerName} ! Votre chambre habituelle est prête."
```

The substitution happens at display time, after localization lookup, so the placeholder works identically in every language.

:::warning
There must be exactly one character profile in your project with `IsPlayerCharacter` checked. If no profile has it checked, `{PlayerName}` is substituted with an empty string and no error is logged. If multiple profiles have it checked, the first one found is used. Set `IsPlayerCharacter` on exactly one profile and leave it unchecked on all others.
:::

---

## Multiple conversations in one file

A single YAML file can contain as many conversations as you like. Each top-level key is an independent conversation. You must create a separate `ConvoCoreConversationData` asset for each key and set its **Conversation Key** field accordingly.

```yaml
ShopGreeting:
  - CharacterID: "Merchant"
    LineID: "merchant_hello"
    LocalizedDialogue:
      EN: "Welcome to my shop!"

ShopFarewell:
  - CharacterID: "Merchant"
    LineID: "merchant_goodbye"
    LocalizedDialogue:
      EN: "Come back anytime!"

ShopOutOfStock:
  - CharacterID: "Merchant"
    LineID: "merchant_out_of_stock"
    LocalizedDialogue:
      EN: "Sorry, I'm all out of that item."
```

Grouping related short conversations in one file keeps the source directory tidy. A good rule of thumb is to group conversations by scene or NPC.

---

:::info[For Advanced Users]
The `ConvoCoreYamlParser` uses YamlDotNet with `IgnoreUnmatchedProperties` enabled. Any key present in your YAML that does not correspond to a known field (for example, a comment-as-field or a future field you are testing) is silently ignored — it will not cause a parse error.

Language code normalization happens at parse time: all language codes are lowercased before being stored in the `DialogueLineInfo.LocalizedDialogue` dictionary. The `ConvoCoreDialogueLocalizationHandler.GetLocalizedDialogue()` method also lowercases the requested language code before performing the dictionary lookup. This means a mismatch between the casing in your YAML (`EN`) and the casing in `ConvoCoreSettings` (`en`) is handled transparently — they will always match.
:::
