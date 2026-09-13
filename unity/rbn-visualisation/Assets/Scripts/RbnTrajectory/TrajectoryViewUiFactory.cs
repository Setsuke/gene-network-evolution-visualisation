using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Trajectory
{
    public static class TrajectoryViewUiFactory
    {
        public const string ViewModeScreenBackdropName = "ViewModeScreenBackdrop";
        private static readonly Color ViewModeScreenBackdropColor = new Color(0.07f, 0.08f, 0.11f, 1f);

        /// <summary>
        /// Opaque margin bars above the main menu and below view chrome.
        /// The viewport rect stays transparent so the 3D camera can show through.
        /// </summary>
        public static void SetViewModeScreenBackdrop(Transform panelRoot, bool visible)
        {
            SetViewModeScreenBackdrop(panelRoot, visible, new Rect(0f, 0f, 1f, 1f));
        }

        public static void SetViewModeScreenBackdrop(
            Transform panelRoot,
            bool visible,
            Rect transparentViewport)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform backdropRoot = panelRoot.Find(ViewModeScreenBackdropName);
            if (backdropRoot == null)
            {
                GameObject backdropObject = new GameObject(ViewModeScreenBackdropName, typeof(RectTransform));
                backdropObject.transform.SetParent(panelRoot, false);
                backdropRoot = backdropObject.transform;
            }

            RectTransform backdropRect = backdropRoot.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            if (backdropRoot.TryGetComponent(out Image legacyRootImage))
            {
                Object.Destroy(legacyRootImage);
            }

            if (backdropRoot.TryGetComponent(out CanvasRenderer legacyRenderer)
                && backdropRoot.childCount == 0)
            {
                Object.Destroy(legacyRenderer);
            }

            backdropRoot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            EnsureViewModeBackdropBar(
                backdropRoot,
                "TopBar",
                new Rect(
                    0f,
                    transparentViewport.y + transparentViewport.height,
                    1f,
                    1f - transparentViewport.y - transparentViewport.height));
            EnsureViewModeBackdropBar(
                backdropRoot,
                "BottomBar",
                new Rect(0f, 0f, 1f, transparentViewport.y));
            EnsureViewModeBackdropBar(
                backdropRoot,
                "LeftBar",
                new Rect(0f, transparentViewport.y, transparentViewport.x, transparentViewport.height));
            EnsureViewModeBackdropBar(
                backdropRoot,
                "RightBar",
                new Rect(
                    transparentViewport.x + transparentViewport.width,
                    transparentViewport.y,
                    1f - transparentViewport.x - transparentViewport.width,
                    transparentViewport.height));

            RemoveUnknownBackdropChildren(backdropRoot);
            backdropRoot.SetAsFirstSibling();
        }

        private static void RemoveUnknownBackdropChildren(Transform backdropRoot)
        {
            if (backdropRoot == null)
            {
                return;
            }

            for (int index = backdropRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = backdropRoot.GetChild(index);
                switch (child.name)
                {
                    case "TopBar":
                    case "BottomBar":
                    case "LeftBar":
                    case "RightBar":
                        continue;
                    default:
                        Object.Destroy(child.gameObject);
                        break;
                }
            }
        }

        private static void EnsureViewModeBackdropBar(
            Transform backdropRoot,
            string barName,
            Rect normalizedRect)
        {
            if (normalizedRect.width <= 0f || normalizedRect.height <= 0f)
            {
                Transform existing = backdropRoot.Find(barName);
                if (existing != null)
                {
                    existing.gameObject.SetActive(false);
                }

                return;
            }

            Transform barTransform = backdropRoot.Find(barName);
            GameObject barObject;
            if (barTransform != null)
            {
                barObject = barTransform.gameObject;
            }
            else
            {
                barObject = new GameObject(barName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                barObject.transform.SetParent(backdropRoot, false);
            }

            barObject.SetActive(true);
            Image image = barObject.GetComponent<Image>();
            image.color = ViewModeScreenBackdropColor;
            image.raycastTarget = false;

            RectTransform rectTransform = barObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(normalizedRect.x, normalizedRect.y);
            rectTransform.anchorMax = new Vector2(
                normalizedRect.x + normalizedRect.width,
                normalizedRect.y + normalizedRect.height);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        public static void PlaceBehindViewChrome(Transform element, Transform panelRoot)
        {
            if (element == null || panelRoot == null)
            {
                return;
            }

            Transform screenBackdrop = panelRoot.Find(ViewModeScreenBackdropName);
            int targetIndex = screenBackdrop != null
                ? screenBackdrop.GetSiblingIndex() + 1
                : 0;
            element.SetSiblingIndex(targetIndex);
        }

        public static void DisableRootRaycasts(GameObject uiPanelRoot)
        {
            if (uiPanelRoot == null)
            {
                return;
            }

            Image rootImage = uiPanelRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.raycastTarget = false;
            }

            foreach (Image image in uiPanelRoot.GetComponentsInChildren<Image>(true))
            {
                if (image == rootImage || image.gameObject.name is "FitnessInfoPanel")
                {
                    continue;
                }

                if (IsUnderInteractiveControl(image.transform))
                {
                    image.raycastTarget = true;
                    continue;
                }

                image.raycastTarget = false;
            }

            foreach (Text text in uiPanelRoot.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = IsUnderInteractiveControl(text.transform);
            }
        }

        /// <summary>
        /// Re-enable raycasts on a control subtree and draw it above other panel chrome.
        /// </summary>
        public static void BringInteractiveControlsToFront(GameObject controlsRoot)
        {
            if (controlsRoot == null)
            {
                return;
            }

            controlsRoot.transform.SetAsLastSibling();
            EnableInteractiveRaycasts(controlsRoot.transform);
        }

        public static void EnableInteractiveRaycasts(Transform interactiveRoot)
        {
            if (interactiveRoot == null)
            {
                return;
            }

            foreach (Image image in interactiveRoot.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = true;
            }

            foreach (Text text in interactiveRoot.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = true;
            }
        }

        private static bool IsUnderInteractiveControl(Transform element)
        {
            return element.GetComponentInParent<Button>() != null
                || element.GetComponentInParent<Slider>() != null
                || element.GetComponentInParent<InputField>() != null
                || IsUnderNamedRoot(element, "LeftLegendShelfTabs")
                || IsUnderNamedRoot(element, "Option10LeftLegendShelfTabs")
                || IsUnderNamedRoot(element, "RightLegendShelfTabs")
                || IsUnderNamedRoot(element, "DualPaneControls");
        }

        private static bool IsUnderNamedRoot(Transform element, string rootName)
        {
            Transform current = element;
            while (current != null)
            {
                if (current.name == rootName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
