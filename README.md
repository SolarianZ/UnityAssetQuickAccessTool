# Unity Asset Quick Access Tool.

[中文](./README_CN.md)

Pin frequently used Unity objects and external files/folders/urls to a separate editor window. An enhanced version of the Unity's Favorites feature.

This version is specifically developed for Unity **2019.4**. If you are using Unity 2021.3 and later versions, please use the [latest](https://github.com/SolarianZ/UnityAssetQuickAccessTool/releases/latest) version .

![Asset Quick Access Window](./Documents~/imgs/img_sample_asset_quick_access_window.png)

## Features

- Record frequently used objects, including:
  - Project assets
  - Scene objects and components
  - External files and folders
  - External URLs(text content)
  - Menu Items(path)
- Filter recorded items by category.
- Quickly locate / open recorded items.
- Copy the path of recorded items.
- Copy the guid of recorded items.
- Copy the type of recorded items.
- Show recorded items in the folder.

## Supported Unity Version

Unity 2019.4 and later.

For Unity 2017.4 - Unity 2019.3, please use version [v1.2.1](https://github.com/SolarianZ/UnityAssetQuickAccessTool/releases/tag/v1.2.1).

## Installation

[![openupm](https://img.shields.io/npm/v/com.greenbamboogames.assetquickaccess?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.greenbamboogames.assetquickaccess/)

Install this package via [OpenUPM](https://openupm.com/packages/com.greenbamboogames.assetquickaccess), or clone this repository directly into the Packages folder of your project.

## How to use

Open the Asset Quick Access window from the menu **Tools/Bamboo/Asset Quick Access** or the `Ctrl+Q` shortcut.

- **Drag and drop** items into the window to record them.
-  In the Unity object context menu, selecting **Bamboo/Add to Asset Quick Access** can record that object.
- **Left-click** on a recorded item to locate (ping) it in the Editor.
  - If the item is an object or component in a Scene and the Scene is not open, it will locate the containing SceneAsset instead.
  - If the item is an external file or folder, no action will be taken.
- **Double-click** on a recorded item to open/execute it.
  - If the item is an object or component in a Scene and the Scene is not open, it will open the containing SceneAsset.
- **Right-click** on a recorded item to display the operation menu.
- ~~Enter the asset's guid or path in the **Find Asset** input field to find it~~ (Use Unity's builtin search(`Ctrl K`) instead).
- Use the category buttons on the window's toolbar to filter items.
- Use the **Add External File** option in the window toolbar's dropdown menu to add an external file.
- Use the **Add External Folder** option in the window toolbar's dropdown menu to add an external folder.
- Use the **Add URL** option in the window toolbar's dropdown menu to add an external url.
- Use the **Remove All Items** option in the window toolbar's dropdown menu to clear all recorded items.

To disable the shortcut, add the [scripting symbol](https://docs.unity3d.com/Manual/CustomScriptingSymbols.html) `GBG_AQA_HOTKEY_OFF` in **Edit > Project Settings > Player** . You can also adjust the shortcut through the [Shortcuts Manager](https://docs.unity3d.com/Manual/ShortcutsManager.html).

To disable the Unity object context menu item, add the [scripting symbol](https://docs.unity3d.com/Manual/CustomScriptingSymbols.html) `GBG_AQA_CONTEXT_MENU_OFF` in **Edit > Project Settings > Player**.

## Known Issues

1. Files and folders from the project folder (`Application.dataPath`) cannot be dragged into the quick access window because Unity does not provide external drag-and-drop callbacks for such items.
   - **Solution**: Use the add external item options from the toolbar dropdown menu instead of dragging.
2. When objects dynamically generated in Play Mode are deleted and new equivalent objects (with the same type, path, etc.) are regenerated, the quick access tool is unable to associate the new equivalent objects with the previous ones, and will consider the previous objects to be in a "Missing" state.
