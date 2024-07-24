﻿using UnityEditor;
using UnityEngine;

namespace GBG.AssetQuickAccess.Editor
{
    public partial class AssetQuickAccessWindow
    {
        private static readonly string[] _toolbarCategoryNames = { "All", "Assets", "Scene Objects", "External Items" };
        private GUIContent _toolbarMenuContent;
        private GUIStyle _toolbarMenuStyle;

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(LocalCache.SelectedCategories == AssetCategory.None, _toolbarCategoryNames[0]))
                {
                    SelectCategory(AssetCategory.None);
                }

                if (GUILayout.Toggle(LocalCache.SelectedCategories == AssetCategory.ProjectAsset, _toolbarCategoryNames[1]))
                {
                    SelectCategory(AssetCategory.ProjectAsset);
                }

                if (GUILayout.Toggle(LocalCache.SelectedCategories == AssetCategory.SceneObject, _toolbarCategoryNames[2]))
                {
                    SelectCategory(AssetCategory.SceneObject);
                }

                if (GUILayout.Toggle(LocalCache.SelectedCategories == AssetCategory.ExternalFile ||
                        LocalCache.SelectedCategories == AssetCategory.Url, _toolbarCategoryNames[3]))
                {
                    SelectCategory(AssetCategory.ExternalFile | AssetCategory.Url);
                }

                if (_toolbarMenuContent == null)
                {
                    _toolbarMenuContent = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d__Menu" : "_Menu");
                }

                if (_toolbarMenuStyle == null)
                {
                    _toolbarMenuStyle = new GUIStyle(GUI.skin.label);
                    _toolbarMenuStyle.fixedWidth = 20;
                    _toolbarMenuStyle.fixedHeight = 20;
                }

                if (GUILayout.Button(_toolbarMenuContent, _toolbarMenuStyle))
                {
                    // Toolbar Menu
                    GenericMenu toolbarMenu = new GenericMenu();
                    toolbarMenu.AddItem(new GUIContent("Add External File"), false, AddExternalFile);
                    toolbarMenu.AddItem(new GUIContent("Add External Folder"), false, AddExternalFolder);
                    toolbarMenu.AddItem(new GUIContent("Add URL"), false, AddUrlEditor);
                    toolbarMenu.AddSeparator("");
                    toolbarMenu.AddItem(new GUIContent("Remove All Items"), false, RemoveAllItems);
                    toolbarMenu.ShowAsContext();
                }

                Rect toolbarMenuRect = GUILayoutUtility.GetLastRect();
                if (toolbarMenuRect.Contains(Event.current.mousePosition))
                {
                    GUI.Box(toolbarMenuRect, (string)null, new GUIStyle()
                    {
                        normal = { background = Texture2D.grayTexture }
                    });
                }
            }
        }
    }
}