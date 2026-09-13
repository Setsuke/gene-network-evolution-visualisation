using RbnVisualisation.GenomePath;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Menu
{
    public enum Option10LeftShelfTab
    {
        Legend,
        Details,
    }

    /// <summary>
    /// Option 10 left-pane shelf tabs: path colour legend vs. plateau drift details.
    /// </summary>
    public class Option10LeftLegendShelfTabs : MonoBehaviour
    {
        public const string RootName = "Option10LeftLegendShelfTabs";
        private const string TabBarName = "TabBar";
        private const string LegendTabName = "LegendTabButton";
        private const string DetailsTabName = "DetailsTabButton";
        private const float TabBarHeightPx = 30f;
        private const float TabButtonWidthPx = 132f;
        private const float TabGapPx = 8f;

        private static readonly Color ActiveTabBackground = new Color(0.16f, 0.34f, 0.62f, 1f);
        private static readonly Color InactiveTabBackground = new Color(0.11f, 0.14f, 0.20f, 1f);

        [SerializeField] private Option10LeftShelfTab activeTab = Option10LeftShelfTab.Details;
        [SerializeField] private Button legendTabButton;
        [SerializeField] private Button detailsTabButton;

        private GameObject panelRoot;

        public Option10LeftShelfTab ActiveTab => activeTab;

        public static Option10LeftLegendShelfTabs EnsureOnPanel(Transform panelRootTransform)
        {
            if (panelRootTransform == null)
            {
                return null;
            }

            Transform existing = panelRootTransform.Find(RootName);
            Option10LeftLegendShelfTabs tabs;
            if (existing != null)
            {
                tabs = existing.GetComponent<Option10LeftLegendShelfTabs>();
                if (tabs != null)
                {
                    tabs.RemoveLegacyTabBarChildren();
                    tabs.EnsureTabButtonsWired();
                    ApplyTabBarLayout(tabs.transform);
                    return tabs;
                }
            }

            GameObject rootObject = new GameObject(
                RootName,
                typeof(RectTransform),
                typeof(Option10LeftLegendShelfTabs));
            rootObject.transform.SetParent(panelRootTransform, false);
            rootObject.transform.SetAsLastSibling();
            tabs = rootObject.GetComponent<Option10LeftLegendShelfTabs>();
            tabs.BuildTabBar(rootObject.transform);
            return tabs;
        }

        public void Bind(GameObject root)
        {
            panelRoot = root;
            RemoveLegacyTabBarChildren();
            ApplyTabBarLayout(transform);
            BindTabHandlers();
            RefreshTabStyles();
            RefreshShelfLayout();
        }

        public static void ApplyTabBarLayout(Transform tabsRoot)
        {
            if (tabsRoot == null)
            {
                return;
            }

            Rect shelf = DualPaneLegendShelfStyle.InsetShelf(DualPaneLayout.LeftPaneLegendBelow);
            Rect tabBarRect = BuildTabBarRect(shelf);
            DualPaneLayout.ApplyNormalizedRect(tabsRoot.GetComponent<RectTransform>(), tabBarRect);

            Transform tabBar = tabsRoot.Find(TabBarName);
            if (tabBar != null && tabBar.TryGetComponent(out RectTransform tabBarRt))
            {
                tabBarRt.anchorMin = Vector2.zero;
                tabBarRt.anchorMax = Vector2.one;
                tabBarRt.offsetMin = Vector2.zero;
                tabBarRt.offsetMax = Vector2.zero;
            }
        }

        public static Rect GetTabContentRect(Rect legendBelow)
        {
            Rect shelf = DualPaneLegendShelfStyle.InsetShelf(legendBelow);
            float tabBarNorm = TabBarHeightPx / Screen.height;
            return new Rect(shelf.x, shelf.y, shelf.width, Mathf.Max(0f, shelf.height - tabBarNorm));
        }

        public static void SetVisible(Transform panelRootTransform, bool visible)
        {
            if (panelRootTransform == null)
            {
                return;
            }

            Transform tabsRoot = panelRootTransform.Find(RootName);
            if (tabsRoot != null)
            {
                tabsRoot.gameObject.SetActive(visible);
            }
        }

        public static void RefreshShelfLayout(Transform panelRootTransform)
        {
            if (panelRootTransform == null)
            {
                return;
            }

            Transform tabsRoot = panelRootTransform.Find(RootName);
            if (tabsRoot != null && tabsRoot.TryGetComponent(out Option10LeftLegendShelfTabs tabs))
            {
                tabs.RefreshShelfLayout();
            }
        }

        public void SelectTab(Option10LeftShelfTab tab)
        {
            activeTab = tab;
            RefreshTabStyles();
            RefreshShelfLayout();
            if (panelRoot != null)
            {
                DualPaneViewUiFactory.BringShelfTabBarsToFront(panelRoot.transform);
            }
        }

        private void RefreshShelfLayout()
        {
            if (panelRoot == null)
            {
                return;
            }

            Rect contentShelf = GetTabContentRect(DualPaneLayout.LeftPaneLegendBelow);
            if (activeTab == Option10LeftShelfTab.Legend)
            {
                PlateauDriftPathLegend.LayoutInShelf(panelRoot.transform, contentShelf);
                PlateauDriftLeftShelfLayout.SetDetailsVisible(panelRoot.transform, false);
            }
            else
            {
                PlateauDriftPathLegend.SetVisible(panelRoot.transform, false);
                PlateauDriftLeftShelfLayout.ApplyDetailsTab(panelRoot.transform, contentShelf);
            }

            DualPaneViewUiFactory.BringShelfTabBarsToFront(panelRoot.transform);
        }

        private void EnsureTabButtonsWired()
        {
            Transform tabBar = transform.Find(TabBarName);
            if (tabBar == null)
            {
                return;
            }

            RemoveLegacyTabBarChildren(tabBar);

            if (legendTabButton == null)
            {
                Transform legendButton = tabBar.Find(LegendTabName);
                if (legendButton != null)
                {
                    legendTabButton = legendButton.GetComponent<Button>();
                }
            }

            if (detailsTabButton == null)
            {
                Transform detailsButton = tabBar.Find(DetailsTabName);
                if (detailsButton != null)
                {
                    detailsTabButton = detailsButton.GetComponent<Button>();
                }
            }

            BindTabHandlers();
        }

        private void RemoveLegacyTabBarChildren(Transform tabBar = null)
        {
            tabBar ??= transform.Find(TabBarName);
            if (tabBar == null)
            {
                return;
            }

            for (int index = tabBar.childCount - 1; index >= 0; index--)
            {
                Transform child = tabBar.GetChild(index);
                if (child.name == LegendTabName || child.name == DetailsTabName)
                {
                    continue;
                }

                DestroyUiObject(child.gameObject);
            }

            Transform legacyTitle = panelRoot != null
                ? panelRoot.transform.Find($"{PlateauDriftPathLegend.PanelName}/LegendTitle")
                : null;
            if (legacyTitle != null)
            {
                DestroyUiObject(legacyTitle.gameObject);
            }
        }

        private static void DestroyUiObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }

        private void BuildTabBar(Transform tabsRoot)
        {
            GameObject tabBarObject = new GameObject(
                TabBarName,
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            tabBarObject.transform.SetParent(tabsRoot, false);

            HorizontalLayoutGroup layout = tabBarObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = TabGapPx;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            legendTabButton = CreateTabButton(tabBarObject.transform, LegendTabName, "Legend");
            detailsTabButton = CreateTabButton(tabBarObject.transform, DetailsTabName, "Details");
        }

        private static Rect BuildTabBarRect(Rect insetShelf)
        {
            float tabBarNorm = TabBarHeightPx / Screen.height;
            return new Rect(
                insetShelf.x,
                insetShelf.y + insetShelf.height - tabBarNorm,
                insetShelf.width,
                tabBarNorm);
        }

        private static Button CreateTabButton(Transform parent, string name, string label)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(TabButtonWidthPx, TabBarHeightPx - 4f);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>()
                ?? buttonObject.AddComponent<LayoutElement>();
            layout.minWidth = TabButtonWidthPx;
            layout.preferredWidth = TabButtonWidthPx;
            layout.preferredHeight = TabBarHeightPx - 4f;

            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.text = label;
            DualPaneLegendShelfStyle.ApplyTabLabel(labelText);

            Image image = buttonObject.GetComponent<Image>();
            image.color = InactiveTabBackground;
            return buttonObject.GetComponent<Button>();
        }

        private void BindTabHandlers()
        {
            if (legendTabButton != null)
            {
                legendTabButton.onClick.RemoveAllListeners();
                legendTabButton.onClick.AddListener(() => SelectTab(Option10LeftShelfTab.Legend));
            }

            if (detailsTabButton != null)
            {
                detailsTabButton.onClick.RemoveAllListeners();
                detailsTabButton.onClick.AddListener(() => SelectTab(Option10LeftShelfTab.Details));
            }
        }

        private void RefreshTabStyles()
        {
            StyleTabButton(legendTabButton, activeTab == Option10LeftShelfTab.Legend);
            StyleTabButton(detailsTabButton, activeTab == Option10LeftShelfTab.Details);
            ApplyTabButtonLabel(legendTabButton);
            ApplyTabButtonLabel(detailsTabButton);
        }

        private static void ApplyTabButtonLabel(Button button)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                DualPaneLegendShelfStyle.ApplyTabLabel(label);
            }
        }

        private static void StyleTabButton(Button button, bool isActive)
        {
            if (button == null || !button.TryGetComponent(out Image image))
            {
                return;
            }

            image.color = isActive ? ActiveTabBackground : InactiveTabBackground;
        }
    }
}
