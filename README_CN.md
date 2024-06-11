# Unity资产快速访问工具

[English](./README.md)

将常用的Unity对象和外部文件固定到独立的编辑器窗口中。Unity Favorites功能的增强版。

![Asset Quick Access Window](./Documents~/imgs/img_sample_asset_quick_access_window.png)

## 功能

- 记录常用的 Unity资产 / Scene中的对象和组件 / 外部文件 / 外部文件夹
- 按类别筛选已记录项目
- 快速 定位 / 打开 已记录项目
- 复制已记录项目路径
- 复制已记录项目Guid
- 复制已记录项目类型
- 在文件夹中显示已记录项目

## 支持的Unity版本

Unity 2021.3 或更新版本。

Unity 2021.2及更低版本的Unity请使用 [1.2.1](https://github.com/SolarianZ/UnityAssetQuickAccessTool/releases/tag/v1.2.1) 版本。

## 安装

[![openupm](https://img.shields.io/npm/v/com.greenbamboogames.assetquickaccess?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.cn/packages/com.greenbamboogames.assetquickaccess/)

从 [OpenUPM](https://openupm.cn/packages/com.greenbamboogames.assetquickaccess) 安装，或者直接将此仓库克隆到项目的Packages文件夹下。

## 如何使用

从菜单 “Tools/Bamboo/Asset Quick Access 打开资产快速访问工具窗口。

- 将项目 **拖放** 到窗口中来记录该项目。
- 使用 **鼠标左键单击** 已记录项目，可以在Editor中定位（Ping）此项目。
  - 若项目是Scene中的对象或组件，且Scene未打开，则改为定位其所在的SceneAsset；
  - 若项目是外部文件或文件夹，则什么都不做。
- 使用 **鼠标左键双击** 已记录项目，可以打开此项目。
  - 若项目是Scene中的对象或组件，且Scene未打开，则打开其所在的SceneAsset。
- 使用 **鼠标右键单击** 已记录项目，可以显示项目操作菜单。
- ~~在 **Find Asset** 输入框中输入资产的Guid或路径查找资产~~ （使用Unity内置的搜索功能(`Ctrl K`)替代）。
- 使用窗口工具栏的类别按钮筛选项目
- 使用窗口工具栏下拉菜单中的 **Add External File** 选项添加一个外部文件。
- 使用窗口工具栏下拉菜单中的 **Add External Folder** 选项添加一个外部文件夹。
- 使用窗口工具栏下拉菜单中的 **Remove All Items** 选项清除已记录的所有项目。
