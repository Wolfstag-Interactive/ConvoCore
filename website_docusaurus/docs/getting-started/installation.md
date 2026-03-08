---
sidebar_position: 1
title: Installation
---

# Installation

This page walks you through installing ConvoCore into a Unity project, verifying the installation, and completing first-time setup. There are two installation methods: via the Unity Package Manager (recommended for most projects) and from a local disk clone.

---

## Requirements

Before installing, make sure your project meets the following requirements:

| Requirement | Minimum version |
|---|---|
| Unity | 2021.3 LTS |
| .NET Standard | 2.1 |
| Other packages | None required for the core feature set |

ConvoCore ships with its YAML parser (YamlDotNet) bundled inside the package, so you do not need to install any other Unity packages to get started. Optional integrations (such as Addressables support) are described at the bottom of this page.

---

## Install via Unity Package Manager (recommended)

The Unity Package Manager (UPM) is the cleanest way to install ConvoCore. It keeps the package files separate from your project's `Assets/` folder and makes updating straightforward.

:::note
The **Unity Package Manager** is a built-in Unity tool for installing reusable code packages — think of it like an app store for Unity project features. Packages live outside your `Assets/` folder so they don't clutter your project, and you can add, update, or remove them without manually copying files.
:::

**Steps:**

1. Open Unity and go to **Window → Package Manager**. The Package Manager window will open.

2. Click the **+** button in the top-left corner of the Package Manager window.

3. Select **Add package from git URL...** from the dropdown.

4. In the text field that appears, enter the ConvoCore git URL in this format:

   ```
   https://github.com/WolfstagInteractive/ConvoCore.git?path=WolfstagInteractive/ConvoCore
   ```

   The `?path=` suffix tells UPM to look inside a specific subfolder of the repository — this is required because the installable package lives inside a subdirectory, not at the repository root.

5. Click **Add**. Unity will download and import the package. This may take a moment.

:::warning
If you see an error like `"No 'package.json' found"`, double-check that the URL ends exactly with `?path=WolfstagInteractive/ConvoCore`. A missing or misspelled path suffix is the most common cause of this error.
:::

---

## Install from disk (local clone)

If you cloned the ConvoCore repository to your machine and want to point your project at your local copy — useful for contributing to the package or iterating on it while building a game — use the "Add package from disk" flow instead.

**Steps:**

1. Clone the ConvoCore repository to a folder on your machine (if you haven't already).

2. Open Unity and go to **Window → Package Manager**.

3. Click the **+** button in the top-left corner.

4. Select **Add package from disk...**

5. In the file browser, navigate to your local clone and select the `package.json` file located at:

   ```
   WolfstagInteractive/ConvoCore/package.json
   ```

6. Click **Open**. Unity will import the package from your local folder.

:::tip
When a package is installed from disk, changes you make to the package files are reflected immediately in the editor (after Unity recompiles). This makes local installation the best choice when you are actively modifying ConvoCore's source code.
:::

---

## Verify the installation

After the import finishes:

1. Look at the Unity menu bar along the top of the editor. A **ConvoCore** menu entry should appear.

2. Open the Unity **Console** (Window → General → Console) and confirm there are no errors related to ConvoCore.

If the ConvoCore menu does not appear, check the Console for compilation errors. The most common causes are:

- A .NET compatibility mismatch (ensure your project is set to .NET Standard 2.1 in **Edit → Project Settings → Player → Other Settings → Api Compatibility Level**).
- A corrupted package download — try removing and re-adding the package via the Package Manager.

---

## First-time setup

Once the package is installed, complete this two-minute setup before creating your first conversation.

**Step 1: Open ConvoCore Settings**

Go to **Tools → ConvoCore → Open Settings** in the menu bar.

If no settings asset exists yet, ConvoCore will automatically create one at:

```
Assets/Resources/ConvoCoreSettings.asset
```

The Inspector will open showing the settings asset.

:::note
**What is a ScriptableObject?** A ScriptableObject is a reusable data container that Unity stores as an asset file in your project — like a config file you can edit in the Inspector and share across multiple scenes. `ConvoCoreSettings` is a ScriptableObject that holds global configuration for the framework (supported languages, default fallback behavior, etc.). You edit it once and it applies everywhere.
:::

**Step 2: Add a supported language**

In the `ConvoCoreSettings` Inspector, find the **Supported Languages** list and add at least one language code. For English, type `EN`. This code must match the language keys you'll write in your YAML dialogue files.

You can add more languages at any time (e.g., `FR`, `DE`, `ES`, `JA`). ConvoCore's language manager uses these codes to look up the correct line of dialogue at runtime.

:::tip
Use short, uppercase ISO 639-1 codes (`EN`, `FR`, `DE`) to stay consistent with the YAML examples throughout this documentation.
:::

---

## Addressables support (optional)

By default, ConvoCore loads dialogue YAML files using Unity's built-in `Resources` system. If your project uses **Unity Addressables** for asset management and you want to load YAML files through that pipeline instead, you can enable Addressables support with a scripting define symbol.

**Steps:**

1. Make sure the **Addressables** package is already installed in your project (via Package Manager → search "Addressables").

2. Go to **Edit → Project Settings → Player → Other Settings → Scripting Define Symbols**.

3. Add the following symbol to the list:

   ```
   CONVOCORE_ADDRESSABLES
   ```

4. Click **Apply**. Unity will recompile.

:::warning
If you add the `CONVOCORE_ADDRESSABLES` scripting define but the Addressables package is **not** installed, ConvoCore will silently fall back to `Resources.Load` without any error or warning. Your dialogue will still load, but it will not use the Addressables pipeline. If you notice Addressables-specific behavior (like async loading) not working, verify that the Addressables package is present.
:::

---

## Next steps

With ConvoCore installed and configured, you're ready to create your first conversation.

- [Quick Start →](./quick-start) — Build a working "Hello World" conversation in under ten minutes.
- [Project Structure →](./project-structure) — Understand where to put your files and how the package is organized.
