using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HearTheTide.UI
{
    /// <summary>
    /// 代码生成 uGUI 的小工具。美术未到位前，所有界面都用纯色块 + 文字占位，
    /// 后续由 UI 组替换成预制体，逻辑层不受影响。
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Bg = Hex("0E1A24");
        public static readonly Color Panel = Hex("16283A");
        public static readonly Color PanelLight = Hex("21384F");
        public static readonly Color Accent = Hex("F2C46D");   // 灯塔暖黄
        public static readonly Color TextColor = Hex("E8EEF2");
        public static readonly Color TextDim = Hex("8FA3B3");
        public static readonly Color Danger = Hex("D9534F");
        public static readonly Color Good = Hex("5CB85C");

        private static Font _font;

        /// <summary>
        /// 字体：优先用 Resources/Fonts/UIFont（美术后续放入可商用中文字体），
        /// 否则用系统中文字体，最后退回 Unity 内置字体。
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("Fonts/UIFont");
                if (_font == null)
                {
                    try
                    {
                        _font = Font.CreateDynamicFontFromOSFont(new[]
                        {
                            "PingFang SC", "Noto Sans CJK SC", "Noto Sans SC", "Source Han Sans SC",
                            "Microsoft YaHei", "Droid Sans Fallback", "Heiti SC", "Arial",
                        }, 32);
                    }
                    catch (Exception) { _font = null; }
                }
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Color Hex(string hex)
        {
            Color c;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>按锚点比例放置（0–1）。</summary>
        public static RectTransform Anchor(this RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }

        public static Image Block(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 32, TextAnchor align = TextAnchor.MiddleLeft, Color? color = null)
        {
            var rt = Rect(parent, "Label");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, Color? color = null, int fontSize = 30)
        {
            var img = Block(parent, "Button_" + label, color ?? PanelLight);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var t = Label(img.transform, label, fontSize, TextAnchor.MiddleCenter);
            t.rectTransform.Stretch(6f);
            return btn;
        }

        /// <summary>带自适应高度的元素放进布局组时使用。</summary>
        public static LayoutElement Size(this Component c, float preferredHeight, float preferredWidth = -1f)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            if (preferredWidth > 0) { le.preferredWidth = preferredWidth; le.minWidth = preferredWidth; }
            return le;
        }

        public static RectTransform VList(Transform parent, float spacing = 12f, int padding = 16)
        {
            var rt = Rect(parent, "VList");
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.childControlHeight = true;
            v.childControlWidth = true;
            return rt;
        }

        public static RectTransform HList(Transform parent, float spacing = 12f, int padding = 0)
        {
            var rt = Rect(parent, "HList");
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(padding, padding, padding, padding);
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = true;
            h.childControlHeight = true;
            h.childControlWidth = true;
            return rt;
        }

        /// <summary>纵向滚动列表，返回内容节点（往里加子物体即可）。</summary>
        public static RectTransform ScrollList(Transform parent, float spacing = 12f)
        {
            var view = Block(parent, "Scroll", new Color(0, 0, 0, 0.15f));
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            view.gameObject.AddComponent<RectMask2D>();
            var content = VList(view.transform, spacing);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            scroll.viewport = view.rectTransform;
            return content;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>可按住的按钮（格挡需要"按住"）。</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action Pressed;
        public event Action Released;
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            if (Pressed != null) Pressed();
        }

        public void OnPointerUp(PointerEventData eventData) { Release(); }
        public void OnPointerExit(PointerEventData eventData) { Release(); }

        private void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            if (Released != null) Released();
        }
    }

    /// <summary>虚拟摇杆（屏幕左下）。</summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public float Radius = 110f;
        public Vector2 Value { get; private set; }

        private RectTransform _rt;

        private void Awake() { _rt = (RectTransform)transform; }

        public void OnPointerDown(PointerEventData e) { OnDrag(e); }

        public void OnDrag(PointerEventData e)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out local)) return;
            var clamped = Vector2.ClampMagnitude(local, Radius);
            if (Knob != null) Knob.anchoredPosition = clamped;
            Value = clamped / Radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }
    }
}
