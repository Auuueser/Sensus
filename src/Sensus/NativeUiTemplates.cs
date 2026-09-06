using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sensus;

// Call only with an inactive, Sensus-owned parent. The original controls and
// shared font/material assets are never edited. Runtime behaviour stays ours.
internal static class NativeUiTemplates
{
    internal static TextMeshProUGUI CloneText(TextMeshProUGUI template, Transform parent, string name)
    {
        var copy = Object.Instantiate(template, parent, false);
        Clean(copy.gameObject, copy);
        copy.name = name;
        copy.enabled = true;
        copy.raycastTarget = false;
        copy.maskable = false;
        copy.enableAutoSizing = false;
        copy.enableVertexGradient = false;
        copy.enableWordWrapping = true;
        copy.overflowMode = TextOverflowModes.Overflow;
        copy.margin = Vector4.zero;
        copy.maxVisibleCharacters = int.MaxValue;
        copy.maxVisibleWords = int.MaxValue;
        copy.maxVisibleLines = int.MaxValue;
        copy.firstVisibleCharacter = 0;
        copy.alpha = 1;
        copy.canvasRenderer.SetAlpha(1);
        copy.canvasRenderer.cull = false;
        copy.text = "";
        copy.gameObject.SetActive(true);
        ResetTransform(copy.rectTransform);
        return copy;
    }

    internal static Image CloneImage(Image template, Transform parent, string name)
    {
        var copy = Object.Instantiate(template, parent, false);
        Clean(copy.gameObject, copy);
        copy.name = name;
        copy.enabled = true;
        copy.raycastTarget = false;
        copy.maskable = false;
        // Keep the native Image component, but discard its decorative stripes
        // and custom material on this clone only. Null sprite draws a solid quad.
        copy.overrideSprite = null;
        copy.sprite = null;
        copy.material = null;
        copy.type = Image.Type.Simple;
        copy.preserveAspect = false;
        copy.canvasRenderer.SetAlpha(1);
        copy.canvasRenderer.cull = false;
        copy.gameObject.SetActive(true);
        ResetTransform(copy.rectTransform);
        return copy;
    }

    private static void Clean(GameObject copy, Component keep)
    {
        // Destroy is deferred. Disable cloned behaviours immediately, before the
        // inactive panel is enabled, so template animators/events cannot run.
        foreach (var component in copy.GetComponents<Component>())
        {
            if (component == null || component == keep || component is RectTransform || component is CanvasRenderer) continue;
            if (component is Behaviour behaviour) behaviour.enabled = false;
            if (component is CanvasGroup group) { group.alpha = 1; group.blocksRaycasts = false; }
            if (component is Button button) button.onClick = new Button.ButtonClickedEvent();
            Object.Destroy(component);
        }
        for (int i = 0; i < copy.transform.childCount; i++)
        {
            var child = copy.transform.GetChild(i).gameObject;
            child.SetActive(false);
            Object.Destroy(child);
        }
    }

    private static void ResetTransform(RectTransform rect)
    {
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }
}
