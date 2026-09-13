using RbnVisualisation.AttractorLandscape;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Menu
{
    public enum DualPaneRightShelfTab
    {
        Legend,
        Attractors,
    }

    /// <summary>
    /// Tab bar for the right-pane legend shelf: colour legend vs. attractor readout.
    /// </summary>
    public class DualPaneRightLegendShelfTabs : MonoBehaviour
    {
        public const string RootName = "RightLegendShelfTabs";
        private const string TabBarName = "TabBar";
        private const string LegendTabName = "LegendTabButton";
        private const string AttractorsTabName = "AttractorsTabButton";
        private const float TabBarHeightPx = 30f;
        private const float TabButtonWidthPx = 132f;
        private const float TabGapPx = 8f;

        private static readonly Color ActiveTabBackground = new Color(0.16f, 0.34f, 0.62f, 1f);
        private static readonly Color InactiveTabBackground = new Color(0.11f, 0.14f, 0.20f, 1f);

        [SerializeField] private DualPaneRightShelfTab activeTab = DualPaneRightShelfTab.Legend;
        [SerializeField] private Button legendTabButton;
        [SerializeField] private Button attractorsTabButton;

        private GameObject panelRoot;
        private BooleanAttractorLandscapeVisualiser visualiser;

        public DualPaneRightShelfTab ActiveTab => activeTab;

        public static DualPaneRightLegendShelfTabs EnsureOnPanel(Transform panelRootTransform)
        {
            if (panelRootTransform == null)
            {
                return null;
            }

            Transform existing = panelRootTransform.Find(RootName);
            DualPaneRightLegendShelfTabs tabs;
            if (existing != null)
            {
                tabs = existing.GetComponent<DualPaneRightLegendShelfTabs>();
                if (tabs != null)
                {
                    tabs.EnsureTabButtonsWired();
                    return tabs;
                }
            }

            GameObject rootObject = new GameObject(
                RootName,
                typeof(RectTransform),
                typeof(DualPaneRightLegendShelfTabs));
            rootObject.transform.SetParent(panelRootTransform, false);
            rootObject.transform.SetAsLastSibling();
            tabs = rootObject.GetComponent<DualPaneRightLegendShelfTabs>();
            tabs.BuildTabBar(rootObject.transform);
            return tabs;
        }

        public void Bind(GameObject root, BooleanAttractorLandscapeVisualiser attractorVisualiser)
        {
            panelRoot = root;
            visualiser = attractorVisualiser;
            ApplyTabBarLayout(transform);
            BindTabHandlers();
            RefreshTabStyles();
        }

        public void SyncSecondaryTabLabel(BooleanAttractorLandscapeHighlightMode? highlightMode = null)
        {
            if (attractorsTabButton == null)
            {
                return;
            }

            Text label = attractorsTabButton.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            BooleanAttractorLandscapeHighlightMode mode = highlightMode
                ?? (visualiser != null
                    ? visualiser.HighlightMode
                    : BooleanAttractorLandscapeHighlightMode.GlobalAttractors);
            label.text = mode switch
            {
                BooleanAttractorLandscapeHighlightMode.ContextPath => "Paths",
                BooleanAttractorLandscapeHighlightMode.ContextAttractors => "Ctx attractors",
                _ => "Attractors",
            };
            DualPaneLegendShelfStyle.ApplyTabLabel(label);
        }

        public static void ApplyTabBarLayout(Transform tabsRoot)
        {
            if (tabsRoot == null)
            {
                return;
            }

            Rect shelf = DualPaneLegendShelfStyle.InsetShelf(DualPaneLayout.RightPaneLegendBelow);
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

        public void SelectTab(DualPaneRightShelfTab tab)
        {
            activeTab = tab;
            RefreshTabStyles();
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeHighlightMode highlightMode = visualiser != null
                ? visualiser.HighlightMode
                : BooleanAttractorLandscapeHighlightMode.GlobalAttractors;
            BooleanAttractorLandscapeLegend.LayoutDualPaneRightShelf(
                panelRoot.transform,
                activeTab,
                highlightMode);

            if (tab == DualPaneRightShelfTab.Attractors && visualiser != null && visualiser.HasActiveRun)
            {
                switch (highlightMode)
                {
                    case BooleanAttractorLandscapeHighlightMode.ContextPath:
                        BooleanAttractorLandscapeContextPathPanel.UpdateFromVisualiser(
                            panelRoot.transform,
                            visualiser);
                        break;
                    case BooleanAttractorLandscapeHighlightMode.ContextAttractors:
                        BooleanAttractorLandscapeContextAttractorReadoutPanel.UpdateFromVisualiser(
                            panelRoot.transform,
                            visualiser);
                        break;
                    default:
                        BooleanAttractorLandscapeAttractorReadoutPanel.UpdateFromVisualiser(
                            panelRoot.transform,
                            visualiser);
                        break;
                }
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

            if (legendTabButton == null)
            {
                Transform legendButton = tabBar.Find(LegendTabName);
                if (legendButton != null)
                {
                    legendTabButton = legendButton.GetComponent<Button>();
                }
            }

            if (attractorsTabButton == null)
            {
                Transform attractorsButton = tabBar.Find(AttractorsTabName);
                if (attractorsButton != null)
                {
                    attractorsTabButton = attractorsButton.GetComponent<Button>();
                }
            }

            BindTabHandlers();
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
            attractorsTabButton = CreateTabButton(tabBarObject.transform, AttractorsTabName, "Attractors");
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

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = buttonObject.AddComponent<LayoutElement>();
            }

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
                legendTabButton.onClick.AddListener(() => SelectTab(DualPaneRightShelfTab.Legend));
            }

            if (attractorsTabButton != null)
            {
                attractorsTabButton.onClick.RemoveAllListeners();
                attractorsTabButton.onClick.AddListener(() => SelectTab(DualPaneRightShelfTab.Attractors));
            }
        }

        private void RefreshTabStyles()
        {
            StyleTabButton(legendTabButton, activeTab == DualPaneRightShelfTab.Legend);
            StyleTabButton(attractorsTabButton, activeTab == DualPaneRightShelfTab.Attractors);
            ApplyTabButtonLabel(legendTabButton);
            SyncSecondaryTabLabel();
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
