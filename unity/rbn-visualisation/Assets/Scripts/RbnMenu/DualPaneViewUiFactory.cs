using RbnVisualisation.AttractorLandscape;
using RbnVisualisation.GenomePath;
using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Menu
{
    public sealed class DualPaneViewUiHandles
    {
        public GameObject Root;
        public TrajectoryZoomSlider LeftZoomSlider;
        public TrajectoryZoomSlider RightZoomSlider;
    }

    public static class DualPaneViewUiFactory
    {
        private static readonly Color ButtonBackground = new Color(0.16f, 0.34f, 0.62f, 1f);
        private static readonly Color ControlsPanelBackground = new Color(0.07f, 0.09f, 0.13f, 0.95f);
        private static readonly Color InputBackground = new Color(0.12f, 0.20f, 0.34f, 1f);
        private static readonly Color InputTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        private const float ControlsBottomOffset = DualPaneLayout.ControlsBottomPaddingPx;
        private const float NavBarWidth = 280f;
        private const float ControlsPanelHeight = DualPaneLayout.ControlsStackHeightPx;
        private const float StepNavBarHeightPx = 126f;
        private const float StepNavBarBottomInsetPx = 6f;
        private const float StepNavVerticalSpacingPx = 6f;
        private const float StepInputWidth = 96f;
        private const float NavButtonRowHeight = 36f;
        private const float ContextToggleHeight = 36f;
        private const float ContextToggleWidth = 220f;
        private const float PaneBorderThickness = 3f;
        private const float PaneBorderHighlightThickness = 1f;
        private static readonly Color PaneBorderColor = new Color(0.36f, 0.44f, 0.58f, 0.98f);
        private static readonly Color PaneBorderHighlightColor = new Color(0.58f, 0.66f, 0.78f, 0.55f);
        private static readonly Color PaneBorderShadowColor = new Color(0.08f, 0.09f, 0.12f, 0.75f);

        public static DualPaneViewUiHandles EnsureOnPanel(
            Transform panelRoot,
            DualPaneMdsAttractorViewController viewController,
            AppMenuController menuController)
        {
            if (panelRoot == null || viewController == null)
            {
                return null;
            }

            EnsurePaneFrames(panelRoot);
            EnsureLegendShelves(panelRoot);
            EnsurePaneTitles(panelRoot);
            EnsureBackButton(panelRoot, menuController);
            bool isOption10View = viewController is Option10DualPaneMdsAttractorViewController;

            EnsureDualPaneLegends(panelRoot);
            BooleanAttractorLandscapeContextPathPanel.EnsureOnPanel(panelRoot);
            BooleanAttractorLandscapeContextAttractorReadoutPanel.EnsureOnPanel(panelRoot);
            BooleanAttractorLandscapeAttractorReadoutPanel.EnsureOnPanel(panelRoot);
            GenomeMdsPathFactorScorePanel.EnsureOnPanel(panelRoot);
            if (!isOption10View)
            {
                DualPaneLeftLegendShelfTabs.EnsureOnPanel(panelRoot);
            }

            DualPaneRightLegendShelfTabs.EnsureOnPanel(panelRoot);

            GameObject controlsRoot = EnsureControlsRoot(panelRoot);
            Transform stepNavBar = EnsureStepNavBar(controlsRoot.transform);
            EnsureStepButtonsRow(stepNavBar);
            EnsureInfoLabel(controlsRoot.transform);
            EnsureStepInput(stepNavBar);
            EnsureContextPathToggle(stepNavBar);
            TrajectoryZoomSlider leftZoom = EnsureZoomSlider(
                panelRoot,
                "LeftZoomSlider",
                DualPaneLayout.LeftPaneZoomRow,
                "Zoom");
            TrajectoryZoomSlider rightZoom = EnsureZoomSlider(
                panelRoot,
                "RightZoomSlider",
                DualPaneLayout.RightPaneZoomRow,
                "Zoom");

            leftZoom.Configure(leftZoom.GetComponent<Slider>(), viewController.LeftSceneCamera);
            rightZoom.Configure(rightZoom.GetComponent<Slider>(), viewController.RightSceneCamera);

            if (!isOption10View)
            {
                SyncLeftPaneShelfLayout(panelRoot, viewController.MdsPathVisualiser);
            }

            SyncRightPaneShelfLayout(panelRoot, viewController.AttractorVisualiser);
            GenomeMdsPathFactorScorePanel.UpdateFromVisualiser(
                panelRoot,
                viewController.MdsPathVisualiser,
                0);

            return new DualPaneViewUiHandles
            {
                Root = panelRoot.gameObject,
                LeftZoomSlider = leftZoom,
                RightZoomSlider = rightZoom,
            };
        }

        public static void SetVisible(DualPaneViewUiHandles handles, bool visible)
        {
            if (handles?.Root != null)
            {
                handles.Root.SetActive(visible);
                SetDualPaneLegendsVisible(handles.Root.transform, visible);
                if (visible)
                {
                    BringShelfTabBarsToFront(handles.Root.transform);
                }
            }
        }

        public static void BringShelfTabBarsToFront(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform leftTabs = panelRoot.Find(DualPaneLeftLegendShelfTabs.RootName);
            if (leftTabs != null)
            {
                leftTabs.SetAsLastSibling();
                TrajectoryViewUiFactory.EnableInteractiveRaycasts(leftTabs);
            }

            Transform option10LeftTabs = panelRoot.Find(Option10LeftLegendShelfTabs.RootName);
            if (option10LeftTabs != null)
            {
                option10LeftTabs.SetAsLastSibling();
                TrajectoryViewUiFactory.EnableInteractiveRaycasts(option10LeftTabs);
            }

            Transform rightTabs = panelRoot.Find(DualPaneRightLegendShelfTabs.RootName);
            if (rightTabs != null)
            {
                rightTabs.SetAsLastSibling();
                TrajectoryViewUiFactory.EnableInteractiveRaycasts(rightTabs);
            }
        }

        public static void SyncRightPaneShelfLayout(
            Transform panelRoot,
            BooleanAttractorLandscapeVisualiser attractorVisualiser = null)
        {
            if (panelRoot == null)
            {
                return;
            }

            DualPaneRightLegendShelfTabs tabs = DualPaneRightLegendShelfTabs.EnsureOnPanel(panelRoot);
            if (tabs != null && attractorVisualiser != null)
            {
                tabs.Bind(panelRoot.gameObject, attractorVisualiser);
            }

            BooleanAttractorLandscapeHighlightMode highlightMode = attractorVisualiser != null
                ? attractorVisualiser.HighlightMode
                : BooleanAttractorLandscapeHighlightMode.GlobalAttractors;
            DualPaneRightShelfTab activeTab = tabs != null
                ? tabs.ActiveTab
                : DualPaneRightShelfTab.Legend;
            BooleanAttractorLandscapeLegend.LayoutDualPaneRightShelf(
                panelRoot,
                activeTab,
                highlightMode);
            BringShelfTabBarsToFront(panelRoot);
        }

        public static void SyncLeftPaneShelfLayout(
            Transform panelRoot,
            GenomeMdsPathVisualiser mdsPathVisualiser)
        {
            if (panelRoot == null)
            {
                return;
            }

            GenomeMdsPathLegend.ApplyLegendBelowFigure(
                panelRoot.gameObject,
                DualPaneLayout.LeftPaneLegendBelow);
            DualPaneLeftLegendShelfTabs tabs = DualPaneLeftLegendShelfTabs.EnsureOnPanel(panelRoot);
            if (tabs != null)
            {
                tabs.Bind(panelRoot.gameObject, mdsPathVisualiser);
            }

            DualPaneLeftShelfTab activeTab = tabs != null
                ? tabs.ActiveTab
                : DualPaneLeftShelfTab.Legend;
            GenomeMdsPathLegend.LayoutDualPaneLeftShelf(
                panelRoot.gameObject,
                mdsPathVisualiser,
                activeTab);
            BringShelfTabBarsToFront(panelRoot);
        }

        private static void EnsureDualPaneLegends(Transform panelRoot)
        {
            BooleanAttractorLandscapeLegend.EnsureOnPanel(panelRoot);
            SharedGenomeMdsPathLegend.SetVisible(panelRoot, false);
            SharedGenomeMdsPathLegend.SetBatchVisible(panelRoot, false);
            GenomeMdsPathLegend.ApplyLegendBelowFigure(
                panelRoot.gameObject,
                DualPaneLayout.LeftPaneLegendBelow);
            DualPaneLeftLegendShelfTabs.SetVisible(panelRoot, true);
            DualPaneRightLegendShelfTabs.SetVisible(panelRoot, true);
        }

        private static void SetDualPaneLegendsVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            GenomeMdsPathLegend.SetVisible(panelRoot, visible);
            DualPaneLeftLegendShelfTabs.SetVisible(panelRoot, visible);
            DualPaneRightLegendShelfTabs.SetVisible(panelRoot, visible);
            if (!visible)
            {
                BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(panelRoot, false);
                BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(panelRoot, false);
                BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, false);
                BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, false);
            }
        }

        private static void EnsureLegendShelves(Transform panelRoot)
        {
            EnsureLegendShelf(panelRoot, "LeftLegendShelf", DualPaneLayout.LeftPaneLegendBelow);
            EnsureLegendShelf(panelRoot, "RightLegendShelf", DualPaneLayout.RightPaneLegendBelow);
        }

        private static void EnsureLegendShelf(Transform panelRoot, string shelfName, Rect legendBelow)
        {
            Transform shelfRoot = panelRoot.Find(shelfName);
            if (shelfRoot == null)
            {
                GameObject shelfObject = new GameObject(
                    shelfName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                shelfObject.transform.SetParent(panelRoot, false);
                shelfRoot = shelfObject.transform;
                shelfRoot.SetAsFirstSibling();
            }

            Image background = shelfRoot.GetComponent<Image>();
            background.color = DualPaneLegendShelfStyle.ShelfBackground;
            background.raycastTarget = false;
            DualPaneLayout.ApplyNormalizedRect(shelfRoot.GetComponent<RectTransform>(), legendBelow);
        }

        private static void EnsurePaneFrames(Transform panelRoot)
        {
            EnsurePaneFrame(panelRoot, "LeftPaneFrame", DualPaneLayout.LeftPaneViewport);
            EnsurePaneFrame(panelRoot, "RightPaneFrame", DualPaneLayout.RightPaneViewport);
        }

        public static void EnsureFigureViewportFrame(
            Transform panelRoot,
            Rect viewport,
            string frameName = LayeredAttractorLandscapeLayout.FigureFrameName)
        {
            RemoveLayeredFigureBackdrop(panelRoot);
            EnsurePaneFrame(panelRoot, frameName, viewport);
        }

        public static void SetFigureViewportFrameVisible(
            Transform panelRoot,
            bool visible,
            string frameName = LayeredAttractorLandscapeLayout.FigureFrameName)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform frameRoot = panelRoot.Find(frameName);
            if (frameRoot != null)
            {
                frameRoot.gameObject.SetActive(visible);
            }
        }

        public static void SetEmbeddedDualPaneChromeVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            SetUiChildActive(panelRoot, "LeftPaneFrame", visible);
            SetUiChildActive(panelRoot, "RightPaneFrame", visible);
            SetUiChildActive(panelRoot, "LeftLegendShelf", visible);
            SetUiChildActive(panelRoot, "RightLegendShelf", visible);
            SetUiChildActive(panelRoot, "LeftPaneTitle", visible);
            SetUiChildActive(panelRoot, "RightPaneTitle", visible);
            SetUiChildActive(panelRoot, "DualPaneControls", visible);
            SetUiChildActive(panelRoot, "LeftZoomSlider", visible);
            SetUiChildActive(panelRoot, "RightZoomSlider", visible);
            SetUiChildActive(panelRoot, "LeftZoomSliderLabel", visible);
            SetUiChildActive(panelRoot, "RightZoomSliderLabel", visible);
            SetUiChildActive(panelRoot, "BatchGenomeMdsFitnessPathControls", visible);
            SetUiChildActive(panelRoot, "FitnessInfoPanel", visible);
            DualPaneLeftLegendShelfTabs.SetVisible(panelRoot, visible);
            DualPaneRightLegendShelfTabs.SetVisible(panelRoot, visible);
            Option10LeftLegendShelfTabs.SetVisible(panelRoot, visible);
            GenomeMdsPathLegend.SetVisible(panelRoot, visible);
            GenomeMdsPathFactorScorePanel.SetVisible(panelRoot, visible);
        }

        public static void ClearOpaqueFigureFrameFills(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            string[] frameNames =
            {
                LayeredAttractorLandscapeLayout.FigureFrameName,
                "LeftPaneFrame",
                "RightPaneFrame",
            };

            for (int index = 0; index < frameNames.Length; index++)
            {
                Transform frameRoot = panelRoot.Find(frameNames[index]);
                if (frameRoot == null)
                {
                    continue;
                }

                DestroyLegacyFrameBackground(frameRoot);

                if (frameRoot.TryGetComponent(out Image frameImage))
                {
                    frameImage.color = Color.clear;
                    frameImage.raycastTarget = false;
                }
            }
        }

        private static void SetUiChildActive(Transform parent, string childName, bool active)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        public static void RemoveLayeredFigureBackdrop(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform backdropRoot = panelRoot.Find("LayeredFigureBackdrop");
            if (backdropRoot != null)
            {
                Object.Destroy(backdropRoot.gameObject);
            }
        }

        private static void DestroyLegacyFrameBackground(Transform frameRoot)
        {
            if (frameRoot == null)
            {
                return;
            }

            Transform background = frameRoot.Find("Background");
            if (background != null)
            {
                Object.Destroy(background.gameObject);
            }
        }

        private static void EnsurePaneFrame(Transform panelRoot, string frameName, Rect viewport)
        {
            Transform frameRoot = panelRoot.Find(frameName);
            if (frameRoot == null)
            {
                GameObject frameObject = new GameObject(frameName, typeof(RectTransform));
                frameObject.transform.SetParent(panelRoot, false);
                frameRoot = frameObject.transform;
            }

            frameRoot.SetAsFirstSibling();
            ApplyPaneFrameRootLayout(frameRoot, viewport);
            EnsurePaneBorderEdge(frameRoot, "OuterTop", Edge.Top, PaneBorderThickness, PaneBorderColor);
            EnsurePaneBorderEdge(frameRoot, "OuterBottom", Edge.Bottom, PaneBorderThickness, PaneBorderColor);
            EnsurePaneBorderEdge(frameRoot, "OuterLeft", Edge.Left, PaneBorderThickness, PaneBorderColor);
            EnsurePaneBorderEdge(frameRoot, "OuterRight", Edge.Right, PaneBorderThickness, PaneBorderColor);
            EnsurePaneBorderEdge(frameRoot, "InnerTop", Edge.Top, PaneBorderHighlightThickness, PaneBorderHighlightColor, PaneBorderThickness);
            EnsurePaneBorderEdge(frameRoot, "InnerLeft", Edge.Left, PaneBorderHighlightThickness, PaneBorderHighlightColor, PaneBorderThickness);
            EnsurePaneBorderEdge(frameRoot, "ShadowBottom", Edge.Bottom, PaneBorderHighlightThickness, PaneBorderShadowColor, PaneBorderThickness);
            EnsurePaneBorderEdge(frameRoot, "ShadowRight", Edge.Right, PaneBorderHighlightThickness, PaneBorderShadowColor, PaneBorderThickness);
        }

        private static void ApplyPaneFrameRootLayout(Transform frameRoot, Rect viewport)
        {
            RectTransform rectTransform = frameRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(viewport.x, viewport.y);
            rectTransform.anchorMax = new Vector2(viewport.x + viewport.width, viewport.y + viewport.height);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private enum Edge
        {
            Top,
            Bottom,
            Left,
            Right,
        }

        private static void EnsurePaneBorderEdge(
            Transform frameRoot,
            string edgeName,
            Edge edge,
            float thickness,
            Color color,
            float margin = 0f)
        {
            Transform edgeTransform = frameRoot.Find(edgeName);
            GameObject edgeObject;
            if (edgeTransform != null)
            {
                edgeObject = edgeTransform.gameObject;
            }
            else
            {
                edgeObject = new GameObject(edgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                edgeObject.transform.SetParent(frameRoot, false);
            }

            Image image = edgeObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rectTransform = edgeObject.GetComponent<RectTransform>();
            switch (edge)
            {
                case Edge.Top:
                    rectTransform.anchorMin = new Vector2(0f, 1f);
                    rectTransform.anchorMax = new Vector2(1f, 1f);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.offsetMin = new Vector2(margin, -(margin + thickness));
                    rectTransform.offsetMax = new Vector2(-margin, -margin);
                    break;
                case Edge.Bottom:
                    rectTransform.anchorMin = new Vector2(0f, 0f);
                    rectTransform.anchorMax = new Vector2(1f, 0f);
                    rectTransform.pivot = new Vector2(0.5f, 0f);
                    rectTransform.offsetMin = new Vector2(margin, margin);
                    rectTransform.offsetMax = new Vector2(-margin, margin + thickness);
                    break;
                case Edge.Left:
                    rectTransform.anchorMin = new Vector2(0f, 0f);
                    rectTransform.anchorMax = new Vector2(0f, 1f);
                    rectTransform.pivot = new Vector2(0f, 0.5f);
                    rectTransform.offsetMin = new Vector2(margin, margin);
                    rectTransform.offsetMax = new Vector2(margin + thickness, -margin);
                    break;
                case Edge.Right:
                    rectTransform.anchorMin = new Vector2(1f, 0f);
                    rectTransform.anchorMax = new Vector2(1f, 1f);
                    rectTransform.pivot = new Vector2(1f, 0.5f);
                    rectTransform.offsetMin = new Vector2(-(margin + thickness), margin);
                    rectTransform.offsetMax = new Vector2(-margin, -margin);
                    break;
            }
        }

        private static void EnsurePaneTitles(Transform panelRoot)
        {
            ApplyPaneTitle(
                panelRoot,
                "LeftPaneTitle",
                "Accepted Genomes Path",
                DualPaneLayout.LeftPaneTitleAnchor);
            ApplyPaneTitle(
                panelRoot,
                "RightPaneTitle",
                "Attractor Landscape",
                DualPaneLayout.RightPaneTitleAnchor);
            ApplyPaneTitle(
                panelRoot,
                "ViewTitle",
                "Linked evolution step: MDS path + attractor landscape",
                new Vector2(0.5f, 1f),
                new Vector2(0f, -8f),
                22);
        }

        private static void ApplyPaneTitle(Transform panelRoot, string name, string content, Vector2 anchor)
        {
            CreateTitle(
                panelRoot,
                name,
                content,
                anchor,
                new Vector2(0f, -8f),
                18,
                new Vector2(420f, 32f),
                TextAnchor.UpperCenter);
        }

        private static void ApplyPaneTitle(
            Transform panelRoot,
            string name,
            string content,
            Vector2 anchor,
            Vector2 anchoredPosition,
            int fontSize)
        {
            Text title = CreateTitle(
                panelRoot,
                name,
                content,
                anchor,
                anchoredPosition,
                fontSize,
                new Vector2(760f, 40f),
                TextAnchor.UpperCenter);
            RectTransform rectTransform = title.rectTransform;
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void EnsureBackButton(Transform panelRoot, AppMenuController menuController)
        {
            Transform existing = panelRoot.Find("BackButton");
            if (existing != null)
            {
                StyleBackButton(existing.gameObject);
                return;
            }

            if (menuController == null)
            {
                return;
            }

            Button backButton = CreateButton(
                "BackButton",
                panelRoot,
                "Back to Menu",
                new Vector2(0f, 1f),
                new Vector2(120f, -36f),
                new Vector2(200f, 52f));
            StyleBackButton(backButton.gameObject);
            backButton.onClick.AddListener(menuController.ShowMainMenu);
        }

        private static void StyleBackButton(GameObject buttonObject)
        {
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.color = Color.white;
            }
        }

        private static GameObject EnsureControlsRoot(Transform panelRoot)
        {
            Transform existing = panelRoot.Find("DualPaneControls");
            GameObject controlsRoot = existing != null
                ? existing.gameObject
                : CreatePanel(
                    "DualPaneControls",
                    panelRoot,
                    ControlsPanelBackground,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, ControlsBottomOffset),
                    new Vector2(NavBarWidth + 40f, ControlsPanelHeight));

            ApplyControlsRootLayout(controlsRoot.GetComponent<RectTransform>());
            Image panelImage = controlsRoot.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = ControlsPanelBackground;
                panelImage.raycastTarget = false;
            }

            return controlsRoot;
        }

        private static void ApplyControlsRootLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, ControlsBottomOffset);
            rectTransform.sizeDelta = new Vector2(NavBarWidth + 40f, ControlsPanelHeight);
        }

        private static Transform EnsureStepNavBar(Transform controlsRoot)
        {
            Transform navBar = controlsRoot.Find("StepNavBar");
            if (navBar == null)
            {
                GameObject navBarObject = new GameObject(
                    "StepNavBar",
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup));
                navBarObject.transform.SetParent(controlsRoot, false);
                navBar = navBarObject.transform;
            }

            ApplyStepNavBarLayout(navBar);
            ReparentLegacyNavControls(controlsRoot, navBar);
            return navBar;
        }

        private static Transform EnsureStepButtonsRow(Transform stepNavBar)
        {
            Transform buttonsRow = stepNavBar.Find("StepButtonsRow");
            if (buttonsRow == null)
            {
                GameObject rowObject = new GameObject(
                    "StepButtonsRow",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup));
                rowObject.transform.SetParent(stepNavBar, false);
                buttonsRow = rowObject.transform;
            }

            ApplyStepButtonsRowLayout(buttonsRow);
            ReparentLegacyButtonsIntoRow(stepNavBar, buttonsRow);
            buttonsRow.SetSiblingIndex(1);
            return buttonsRow;
        }

        private static void ApplyStepNavBarLayout(Transform navBar)
        {
            HorizontalLayoutGroup legacyHorizontal = navBar.GetComponent<HorizontalLayoutGroup>();
            if (legacyHorizontal != null)
            {
                Object.Destroy(legacyHorizontal);
            }

            RectTransform rectTransform = navBar.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, StepNavBarBottomInsetPx);
            rectTransform.sizeDelta = new Vector2(NavBarWidth, StepNavBarHeightPx);

            VerticalLayoutGroup layout = navBar.GetComponent<VerticalLayoutGroup>()
                ?? navBar.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.spacing = StepNavVerticalSpacingPx;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void ApplyStepButtonsRowLayout(Transform buttonsRow)
        {
            RectTransform rectTransform = buttonsRow.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(NavBarWidth, NavButtonRowHeight);

            LayoutElement rowLayout = buttonsRow.GetComponent<LayoutElement>();
            if (rowLayout == null)
            {
                rowLayout = buttonsRow.gameObject.AddComponent<LayoutElement>();
            }

            rowLayout.preferredWidth = NavBarWidth;
            rowLayout.preferredHeight = NavButtonRowHeight;

            HorizontalLayoutGroup layout = buttonsRow.GetComponent<HorizontalLayoutGroup>()
                ?? buttonsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void ReparentLegacyNavControls(Transform controlsRoot, Transform navBar)
        {
            ReparentIfFound(controlsRoot, navBar, "StepInputField");
            Transform buttonsRow = EnsureStepButtonsRow(navBar);
            ReparentIfFound(controlsRoot, buttonsRow, "FirstStepButton");
            ReparentIfFound(controlsRoot, buttonsRow, "PreviousStepButton");
            ReparentIfFound(controlsRoot, buttonsRow, "NextStepButton");
            ReparentIfFound(controlsRoot, buttonsRow, "LastStepButton");
            ReparentLegacyButtonsIntoRow(navBar, buttonsRow);
            ReparentContextToggleIntoNavBar(navBar);
        }

        private static void ReparentContextToggleIntoNavBar(Transform navBar)
        {
            Transform controlsRoot = navBar.parent;
            Transform panelRoot = controlsRoot != null ? controlsRoot.parent : null;
            if (controlsRoot != null)
            {
                ReparentIfFound(controlsRoot, navBar, "ContextPathToggleButton");
            }

            if (panelRoot != null)
            {
                ReparentIfFound(panelRoot, navBar, "ContextPathToggleButton");
            }
        }

        private static void ReparentLegacyButtonsIntoRow(Transform navBar, Transform buttonsRow)
        {
            ReparentIfFound(navBar, buttonsRow, "FirstStepButton");
            ReparentIfFound(navBar, buttonsRow, "PreviousStepButton");
            ReparentIfFound(navBar, buttonsRow, "NextStepButton");
            ReparentIfFound(navBar, buttonsRow, "LastStepButton");
        }

        private static void ReparentIfFound(Transform controlsRoot, Transform navBar, string childName)
        {
            Transform child = controlsRoot.Find(childName);
            if (child != null && child.parent != navBar)
            {
                child.SetParent(navBar, false);
            }
        }

        private static Text EnsureInfoLabel(Transform controlsRoot)
        {
            Transform existing = controlsRoot.Find("InfoLabel");
            Text label;
            if (existing != null && existing.TryGetComponent(out label))
            {
                ApplyInfoLabelLayout(existing.GetComponent<RectTransform>());
                return label;
            }

            label = CreateTitle(
                controlsRoot,
                "InfoLabel",
                string.Empty,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -6f),
                15,
                new Vector2(NavBarWidth + 20f, 22f),
                TextAnchor.MiddleCenter);
            ApplyInfoLabelLayout(label.GetComponent<RectTransform>());
            return label;
        }

        private static void ApplyInfoLabelLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -4f);
            rectTransform.sizeDelta = new Vector2(NavBarWidth + 20f, 22f);
        }

        private static InputField EnsureStepInput(Transform navBar)
        {
            Transform existing = navBar.Find("StepInputField");
            InputField inputField;
            if (existing != null && existing.TryGetComponent(out inputField))
            {
                StyleStepInput(existing.gameObject, inputField);
                ApplyStepInputLayout(existing.gameObject);
                existing.SetAsFirstSibling();
                return inputField;
            }

            GameObject inputObject = DefaultControls.CreateInputField(new DefaultControls.Resources());
            inputObject.name = "StepInputField";
            inputObject.transform.SetParent(navBar, false);
            inputField = inputObject.GetComponent<InputField>();
            StyleStepInput(inputObject, inputField);
            ApplyStepInputLayout(inputObject);
            inputObject.transform.SetAsFirstSibling();
            return inputField;
        }

        private static void StyleStepInput(GameObject inputObject, InputField inputField)
        {
            inputField.contentType = InputField.ContentType.IntegerNumber;
            inputField.lineType = InputField.LineType.SingleLine;

            if (inputField.textComponent != null)
            {
                inputField.textComponent.fontSize = 20;
                inputField.textComponent.fontStyle = FontStyle.Bold;
                inputField.textComponent.color = InputTextColor;
                inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            }

            if (inputField.placeholder is Text placeholder)
            {
                placeholder.text = "1";
                placeholder.fontSize = 20;
                placeholder.fontStyle = FontStyle.Bold;
                placeholder.color = new Color(0.58f, 0.66f, 0.78f, 0.9f);
                placeholder.alignment = TextAnchor.MiddleCenter;
            }

            if (inputObject.TryGetComponent(out Image background))
            {
                background.color = InputBackground;
            }
        }

        private static void ApplyStepInputLayout(GameObject inputObject)
        {
            RectTransform rectTransform = inputObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(StepInputWidth, 36f);

            LayoutElement layout = inputObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = inputObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = StepInputWidth;
            layout.preferredWidth = StepInputWidth;
            layout.flexibleWidth = 0f;
            layout.preferredHeight = 36f;
        }

        private static Button EnsureContextPathToggle(Transform stepNavBar)
        {
            ReparentContextToggleIntoNavBar(stepNavBar);

            Transform existing = stepNavBar.Find("ContextPathToggleButton");
            Button toggleButton;
            if (existing != null && existing.TryGetComponent(out toggleButton))
            {
                ApplyContextPathToggleLayout(existing.gameObject);
                existing.SetAsLastSibling();
                return toggleButton;
            }

            toggleButton = CreateContextPathToggle(stepNavBar);
            return toggleButton;
        }

        private static Button CreateContextPathToggle(Transform stepNavBar)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = "ContextPathToggleButton";
            buttonObject.transform.SetParent(stepNavBar, false);
            StyleContextPathToggle(buttonObject);
            ApplyContextPathToggleLayout(buttonObject);
            buttonObject.transform.SetAsLastSibling();
            return buttonObject.GetComponent<Button>();
        }

        private static void StyleContextPathToggle(GameObject buttonObject)
        {
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.text = "Show context path";
            labelText.fontSize = 16;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            Image image = buttonObject.GetComponent<Image>();
            image.color = ButtonBackground;
        }

        private static void ApplyContextPathToggleLayout(GameObject buttonObject)
        {
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(ContextToggleWidth, ContextToggleHeight);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = buttonObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = ContextToggleWidth;
            layout.preferredWidth = ContextToggleWidth;
            layout.preferredHeight = ContextToggleHeight;
        }

        private static TrajectoryZoomSlider EnsureZoomSlider(
            Transform panelRoot,
            string name,
            Rect zoomRow,
            string labelText)
        {
            Rect labelRect = new Rect(
                zoomRow.x,
                zoomRow.y,
                zoomRow.width * 0.1f,
                zoomRow.height);
            Rect sliderRect = new Rect(
                zoomRow.x + zoomRow.width * 0.12f,
                zoomRow.y,
                zoomRow.width * 0.86f,
                zoomRow.height);

            Transform labelTransform = panelRoot.Find($"{name}Label");
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject(
                    $"{name}Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                labelObject.transform.SetParent(panelRoot, false);
                labelTransform = labelObject.transform;
                Text label = labelObject.GetComponent<Text>();
                label.text = labelText;
                label.fontSize = 16;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleLeft;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.raycastTarget = false;
            }
            else if (labelTransform.TryGetComponent(out Text existingLabel))
            {
                existingLabel.text = labelText;
            }

            DualPaneLayout.ApplyNormalizedRect(labelTransform.GetComponent<RectTransform>(), labelRect);

            Transform existing = panelRoot.Find(name);
            if (existing != null)
            {
                DualPaneLayout.ApplyNormalizedRect(existing.GetComponent<RectTransform>(), sliderRect);
                return existing.GetComponent<TrajectoryZoomSlider>()
                    ?? existing.gameObject.AddComponent<TrajectoryZoomSlider>();
            }

            GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObject.name = name;
            sliderObject.transform.SetParent(panelRoot, false);
            DualPaneLayout.ApplyNormalizedRect(sliderObject.GetComponent<RectTransform>(), sliderRect);
            return sliderObject.GetComponent<TrajectoryZoomSlider>()
                ?? sliderObject.AddComponent<TrajectoryZoomSlider>();
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static Text CreateTitle(
            Transform parent,
            string name,
            string content,
            Vector2 anchor,
            Vector2 anchoredPosition,
            int fontSize = 18,
            Vector2? sizeDelta = null,
            TextAnchor alignment = TextAnchor.UpperCenter)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Text existingLabel))
            {
                existingLabel.text = content;
                return existingLabel;
            }

            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta ?? new Vector2(560f, 40f);
            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontSize >= 20 ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = alignment;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor.x <= 0f ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.text = label;
            labelText.fontSize = 18;
            Image image = buttonObject.GetComponent<Image>();
            image.color = ButtonBackground;
            return buttonObject.GetComponent<Button>();
        }
    }
}
