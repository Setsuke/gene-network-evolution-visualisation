using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Menu
{
    /// <summary>
    /// Shared main-menu button positions.
    /// </summary>
    public static class MainMenuLayout
    {
        public static readonly string[] ButtonNames =
        {
            "Option2Button",
            "Option4Button",
            "Option5Button",
            "Option6Button",
            "Option9Button",
            "Option11Button",
            "Option12Button",
        };

        public static readonly float[] ButtonY =
        {
            210f,
            140f,
            70f,
            0f,
            -70f,
            -140f,
            -210f,
        };

        public const float ButtonWidth = 520f;
        public const float ButtonHeight = 64f;

        public static string GetMenuLabel(int index)
        {
            return index switch
            {
                0 => "Option 1 - Evolutionary genome path (MDS)",
                1 => "Option 2 - Accepted genome path (MDS + fitness)",
                2 => "Option 3 - Boolean attractor landscape",
                3 => "MDS - shared initial genome",
                4 => "MDS - different initial genome",
                5 => "Plateau Drift - Attractor landscape",
                6 => "MDS 2D - Fitness",
                _ => $"Option {index + 1}",
            };
        }

        public static void Apply(Transform mainMenuPanel)
        {
            if (mainMenuPanel == null)
            {
                return;
            }

            for (int index = 0; index < ButtonNames.Length; index++)
            {
                Transform buttonTransform = mainMenuPanel.Find(ButtonNames[index]);
                if (buttonTransform == null)
                {
                    continue;
                }

                if (buttonTransform.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = new Vector2(0f, ButtonY[index]);
                    rectTransform.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
                }

                Transform labelTransform = buttonTransform.Find("Label");
                if (labelTransform != null && labelTransform.TryGetComponent(out Text label))
                {
                    label.text = GetMenuLabel(index);
                }
            }
        }
    }
}
