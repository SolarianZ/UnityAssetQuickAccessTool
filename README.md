# Unity Asset Quick Access Tool.

[中文](./README_CN.md)

Pin frequently used Unity objects and external files/folders to a separate editor window. An enhanced version of the Unity's Favorites feature.

![Asset Quick Access Window](./Documents~/imgs/img_sample_asset_quick_access_window.png)

## Features

- Record frequently used resources, including:
  - Unity assets
  - Scene objects and components
  - External files and folders
- Filter recorded items by category.
- Quickly locate / open recorded items.
- Copy the path of recorded items.
- Copy the guid of recorded items.
- Copy the type of recorded items.
- Show recorded items in the folder.

## Supported Unity Version

Unity 2021.3 and later.

For Unity 2021.2 and earlier versions of Unity, please use version [1.2.1](https://github.com/SolarianZ/UnityAssetQuickAccessTool/releases/tag/v1.2.1).

## Installation

[![openupm](https://img.shields.io/npm/v/com.greenbamboogames.assetquickaccess?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.greenbamboogames.assetquickaccess/)

Install this package via [OpenUPM](https://openupm.com/packages/com.greenbamboogames.assetquickaccess), or clone this repository directly into the Packages folder of your project.

## How to use

Open the Asset Quick Access window from the menu "Tools/Bamboo/Asset Quick Access".

- **Drag and drop** items into the window to record them, or right-click on a Unity object and select the **Bamboo/Add to Asset Quick Access** option to record the object.
- **Left-click** on a recorded item to locate (ping) it in the Editor.
  - If the item is an object or component in a Scene and the Scene is not open, it will locate the containing SceneAsset instead.
  - If the item is an external file or folder, no action will be taken.
- **Double-click** on a recorded item to open it.
  - If the item is an object or component in a Scene and the Scene is not open, it will open the containing SceneAsset.
- **Right-click** on a recorded item to display the operation menu.
- ~~Enter the asset's guid or path in the **Find Asset** input field to find it~~ (Use Unity's builtin search(`Ctrl K`) instead).
- Use the category buttons on the window's toolbar to filter items.
- Use the **Add External File** option in the window toolbar's dropdown menu to add an external file.
- Use the **Add External Folder** option in the window toolbar's dropdown menu to add an external folder.
- Use the **Remove All Items** option in the window toolbar's dropdown menu to clear all recorded items.

## Known Issues

1. Upgrading from versions prior to 3.0.0 will result in the clearing of old version records.
2. Files and folders from the project folder (`Application.dataPath`) cannot be dragged into the quick access window because Unity does not provide external drag-and-drop callbacks for such items.
   - **Solution**: Use the add external item options from the toolbar dropdown menu instead of dragging.