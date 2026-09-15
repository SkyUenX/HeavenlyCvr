using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace HeavenlyCVR.Heavenly.UI;

#pragma warning disable CS0618 // Legacy panel references its own obsolete members.
public static class HeavenlyPanel
{
    public static GameObject? Panel { get; private set; }

    private static Canvas? _canvas;

    [System.Obsolete("Deprecated: fragile Cohtml/QuickMenu parenting. Use HeavenlyUILibBridge.")]
    public static void Initialize()
    {
        try
        {
            MelonLogger.Msg(
                "[HeavenlyCVR] Creating Heavenly panel..."
            );

            GameObject? quickMenu =
                GameObject.Find("Cohtml/QuickMenu");

            if (quickMenu == null)
            {
                MelonLogger.Error(
                    "[HeavenlyCVR] Could not find Cohtml/QuickMenu."
                );

                return;
            }

            Panel = new GameObject("HeavenlyCVR");

            Panel.layer = LayerMask.NameToLayer("UI");

            Panel.transform.SetParent(
                quickMenu.transform,
                false
            );

            _canvas = Panel.AddComponent<Canvas>();

            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main;

            _canvas.sortingLayerName = "UI";
            _canvas.sortingOrder = 20;
            _canvas.overrideSorting = true;

            Panel.AddComponent<GraphicRaycaster>();

            Panel.transform.localPosition =
                new Vector3(0.686f, 0f, 0f);

            Panel.transform.localScale =
                new Vector3(0.006f, 0.007f, 0.001f);

            CreateBackground();

            Panel.SetActive(true);

            MelonLogger.Msg(
                "[HeavenlyCVR] Heavenly panel created!"
            );
        }
        catch (Exception ex)
        {
            MelonLogger.Error(
                $"[HeavenlyCVR] Failed to create panel: {ex}"
            );
        }
    }

    private static void CreateBackground()
    {
        if (Panel == null)
            return;

        GameObject background =
            new GameObject("Background");

        background.layer =
            LayerMask.NameToLayer("UI");

        background.transform.SetParent(
            Panel.transform,
            false
        );

        RectTransform rect =
            background.AddComponent<RectTransform>();

        rect.localPosition =
            Vector3.zero;

        rect.sizeDelta =
            new Vector2(90f, 85f);

        Image image =
            background.AddComponent<Image>();

        image.color =
            new Color(0.03f, 0.03f, 0.04f, 0.92f);
    }
}
