using UnityEngine;

namespace RbnVisualisation.Menu
{
    internal static class MainMenuVisibility
    {
        public static GameObject ResolveMainMenuPanel(GameObject cachedPanel)
        {
            if (cachedPanel != null)
            {
                return cachedPanel;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            Transform panelTransform = canvas.transform.Find("MainMenuPanel");
            return panelTransform != null ? panelTransform.gameObject : null;
        }

        public static GameObject HideMainMenu(GameObject cachedPanel)
        {
            GameObject panel = ResolveMainMenuPanel(cachedPanel);
            HideAllMenuPanels();
            DeactivateLegacyLayeredMenuBlocker();

            if (panel != null)
            {
                SetCanvasGroupVisible(panel, visible: false);
                panel.SetActive(false);
            }

            return panel;
        }

        public static void ShowMainMenu(GameObject panel)
        {
            panel = ResolveMainMenuPanel(panel);
            if (panel == null)
            {
                return;
            }

            panel.SetActive(true);
            SetCanvasGroupVisible(panel, visible: true);
            panel.transform.SetAsLastSibling();
        }

        public static void HideAllMenuPanels()
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate.name != "MainMenuPanel" || !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                SetCanvasGroupVisible(candidate.gameObject, visible: false);
                candidate.gameObject.SetActive(false);
            }
        }

        public static void DeactivateLegacyLayeredMenuBlocker()
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate.name == "LayeredMenuBlocker" && candidate.gameObject.scene.IsValid())
                {
                    candidate.gameObject.SetActive(false);
                }
            }
        }

        private static void SetCanvasGroupVisible(GameObject panel, bool visible)
        {
            if (!panel.TryGetComponent(out CanvasGroup group))
            {
                if (!visible)
                {
                    group = panel.AddComponent<CanvasGroup>();
                }
                else
                {
                    return;
                }
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
