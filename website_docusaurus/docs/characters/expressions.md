---
sidebar_position: 3
title: Expressions
---

# Expressions

Expressions are named emotional states — `Happy`, `Angry`, `Surprised`, `Neutral` — that control what a character looks like during a specific dialogue line. Each expression maps to a visual change: a different sprite, an animation trigger, a shader parameter update, or any custom logic you provide.

---

## ConvoCoreCharacterExpression

A `ConvoCoreCharacterExpression` is a ScriptableObject that names an emotion and maps it to visual data.

**Create one**: Right-click in the **Project** panel → **Create → ConvoCore → Character Expression**

### Fields

| Field | Description |
|---|---|
| **Expression Name** | A human-readable label shown in the inspector and expression picker (e.g., `"Surprised"`, `"Happy"`, `"Neutral"`). |
| **Expression GUID** | An auto-generated, stable unique identifier. The GUID is what ConvoCore uses internally to reference this expression — not the name. |
| **Default Sprite** | The sprite used for this expression when no per-representation override is set. |
| **Representation Overrides** | A list of `RepresentationExpressionOverride` entries. Each entry pairs a specific `CharacterRepresentationBase` asset with an alternate sprite, allowing the same emotion to look different across visual variants of the same character. |

### Expression GUIDs

The GUID is the most important property of a `ConvoCoreCharacterExpression` asset.

:::tip
Prefer expression GUIDs over expression names when referencing expressions in code. GUIDs are stable across renames — even if you rename `"Surprised"` to `"Shocked"`, the GUID stays the same and all dialogue lines that reference it continue to work correctly. Display names are cosmetic; GUIDs are the actual identity.
:::

When you create a new `ConvoCoreCharacterExpression`, ConvoCore generates a GUID for it automatically. You should never need to set or change this value manually.

:::warning
Deleting a `ConvoCoreCharacterExpression` asset and creating a new one with the same name will produce a different GUID. Any dialogue lines that referenced the old expression's GUID will lose their expression assignment. Rename expressions freely; avoid deleting and recreating them.
:::

---

## Representation Overrides

The **Representation Overrides** list handles the case where a character has multiple visual variants (representations) and the same emotion should look different across them.

For example, a character with a `"Default"` and an `"Armored"` representation might use:
- A smiling sprite for `Happy` in the `"Default"` variant
- A slightly different smiling sprite (with a helmet visible) for `Happy` in the `"Armored"` variant

Rather than creating two separate expression assets, you create one `Happy` expression and add a Representation Override entry that points to the `Armored` representation and provides the alternate sprite.

When the runner applies an expression, it checks the Representation Overrides list first. If a matching override is found for the active representation, that override's sprite is used. Otherwise, the **Default Sprite** is used.

---

## Setting Expressions on Dialogue Lines

Expressions are assigned per dialogue line in the `ConvoCoreConversationData` inspector.

1. Select your `ConvoCoreConversationData` asset.
2. In the Inspector, expand a dialogue line's entry.
3. Find the **Display Settings** sub-section for that line.
4. The **Selected Expression Id** field shows the currently assigned expression GUID (or blank for none).
5. Click the field to open the expression GUID selector, which lists all expression assets linked to the speaking character's representations.
6. Select the expression you want.

The selector only shows expressions that are actually attached to the speaking character's representation(s), so you will not accidentally assign an expression from a different character.

:::note
If the expression selector is empty, it means the speaking character's profile has no representations, or the representations have no expression mappings configured. Add expressions to the character's representation asset(s) first, then return to assign them on lines.
:::

---

## BaseExpressionAction

For expression changes that require more than a static sprite swap — triggering an animation, playing a particle effect, blending a shader, or any coroutine-based visual — create a ScriptableObject that extends `BaseExpressionAction`.

```csharp
using System.Collections;
using UnityEngine;
using WolfstagInteractive.ConvoCore;
using WolfstagInteractive.ConvoCore.UI;

[CreateAssetMenu(menuName = "ConvoCore/Expression Actions/My Expression Action")]
public class MyExpressionAction : BaseExpressionAction
{
    [SerializeField] private float _transitionDuration = 0.3f;

    protected override IEnumerator ExecuteExpression(
        string expressionId,
        ConvoCore runner,
        ConvoCoreCharacterDisplayBase display)
    {
        // Trigger the animation on the display's animator
        if (display.Animator != null)
        {
            display.Animator.SetTrigger(expressionId);
        }

        // Wait for the transition to finish before the line continues
        yield return new WaitForSeconds(_transitionDuration);
    }
}
```

Attach the `BaseExpressionAction` asset to the expression via the representation's expression mapping entry. When the runner applies the expression, it runs `ExecuteExpression()` as a coroutine before advancing to the dialogue text display.

:::tip
`ExecuteExpression()` runs as a coroutine and the line will not display until it completes. Keep transitions short — 0.2–0.5 seconds is usually enough. If you need an animation to play in parallel with the text, start a separate coroutine and yield nothing, returning immediately from `ExecuteExpression()`.
:::

:::info[For Advanced Users]
`BaseExpressionAction` is a ScriptableObject. You can have multiple expression action types (one for animation, one for VFX, one for audio) and mix them per expression mapping entry. The runner processes them in order. You can also access the full `ConvoCore` runner instance and `ConvoCoreConversationData` from within `ExecuteExpression()` if you need conversation-level context — for example, to check a variable store value and conditionally apply a different visual.
:::

---

## Expression Resolution Order

When the runner applies an expression for a dialogue line, it follows this order:

1. Read the line's **Selected Expression Id** (a GUID).
2. Call `GetExpressionMappingByGuid(guid)` on the active `CharacterRepresentationBase` asset.
3. Check the mapping for a **Representation Override** matching the active representation. If found, use the override sprite.
4. Otherwise, use the expression's **Default Sprite**.
5. If a `BaseExpressionAction` is attached, run it as a coroutine.
6. Pass the resolved sprite (or other visual data) to the `ConvoCoreCharacterDisplayBase` subclass via `ApplyExpression()`.

If the Selected Expression Id is blank, step 2 returns null and the character display retains whatever expression it was showing from the previous line.

---

## Next Steps

| I want to… | Go here |
|---|---|
| Understand the representation that holds expression mappings | [Character Representations →](character-representations) |
| Build the character display component that renders expressions | [UI Foundation →](../ui/ui-foundation) |
| Assign expressions in YAML instead of the inspector | [YAML Format →](../yaml-reference/yaml-format) |
