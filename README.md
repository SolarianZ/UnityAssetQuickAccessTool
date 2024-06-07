# Unity Asset Quick Access Tool.

[中文](./README_CN.md)

Pin frequently used objects to a separate editor window. An enhanced version of Unity's Favorites feature.

![Asset Quick Access Window](./Documents~/imgs/img_sample_asset_quick_access_window.png)

## Features

- Record commonly used assets.
- Quickly locate/open commonly used assets.
- Copy asset path.
- Copy asset guid.
- Copy asset type.
- Show asset in folder.

## Supported Unity Version

Unity 2021.3 and later.

For Unity 2021.2 and earlier versions of Unity, please use version [1.2.1](https://github.com/SolarianZ/UnityAssetQuickAccessTool/releases/tag/v1.2.1).

## Installation

[![openupm](https://img.shields.io/npm/v/com.greenbamboogames.assetquickaccess?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.greenbamboogames.assetquickaccess/)

Install this package via [OpenUPM](https://openupm.com/packages/com.greenbamboogames.assetquickaccess), or clone this repository directly into the Packages folder of your project.

## How to use

Open the Asset Quick Access window from the "Tools/Bamboo/Asset Quick Access" menu.

- **Drag and drop** assets into the window to record them.
- **Left-click** on a recorded asset to locate (ping) it in the Project window.
- **Double-click** on a recorded asset to open it.
- **Right-click** on a recorded asset to display the asset operation menu.
- ~~Enter the asset's guid or path in the **Find Asset** input field to find it~~ (Use Unity's builtin search(`Ctrl K`) instead).
- Use the "Clear All Items" option in the window's context menu to clear all recorded assets.
