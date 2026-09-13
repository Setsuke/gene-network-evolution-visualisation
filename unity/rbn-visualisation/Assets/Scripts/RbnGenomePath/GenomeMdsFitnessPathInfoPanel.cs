using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public class GenomeMdsFitnessPathInfoPanel : MonoBehaviour
    {
        public const float TextSectionHeight = 72f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text fitnessText;
        [SerializeField] private Text acceptedGenomeText;
        [SerializeField] private GenomeMdsFitnessPathFactorCircles factorCircles;

        public void Initialize(
            GameObject root,
            Text fitnessLabel,
            Text acceptedLabel,
            GenomeMdsFitnessPathFactorCircles circles)
        {
            panelRoot = root;
            fitnessText = fitnessLabel;
            acceptedGenomeText = acceptedLabel;
            factorCircles = circles;
        }

        public void ConfigureFactorLayout(int geneCount, int contextCount, string[] contextTargets)
        {
            EnsureWired();

            if (geneCount <= 0 || contextCount <= 0)
            {
                if (factorCircles != null)
                {
                    factorCircles.gameObject.SetActive(false);
                }

                return;
            }

            if (factorCircles == null)
            {
                GameObject circlesRoot = new GameObject(
                    "FactorCircles",
                    typeof(RectTransform),
                    typeof(GenomeMdsFitnessPathFactorCircles));
                circlesRoot.transform.SetParent(transform, false);
                factorCircles = circlesRoot.GetComponent<GenomeMdsFitnessPathFactorCircles>();
            }

            factorCircles.gameObject.SetActive(true);
            LayoutFactorSection(factorCircles.transform as RectTransform);
            factorCircles.Build(geneCount, contextCount, contextTargets);
        }

        public static void LayoutFactorSection(RectTransform factorRect)
        {
            if (factorRect == null)
            {
                return;
            }

            factorRect.anchorMin = Vector2.zero;
            factorRect.anchorMax = Vector2.one;
            factorRect.pivot = new Vector2(0.5f, 0.5f);
            factorRect.offsetMin = Vector2.zero;
            factorRect.offsetMax = new Vector2(0f, -TextSectionHeight);
        }

        private void Awake()
        {
            EnsureWired();
        }

        public void EnsureWired()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (fitnessText == null)
            {
                Transform fitnessTransform = transform.Find("FitnessText");
                if (fitnessTransform != null)
                {
                    fitnessText = fitnessTransform.GetComponent<Text>();
                }
            }

            if (acceptedGenomeText == null)
            {
                Transform acceptedTransform = transform.Find("AcceptedGenomeText");
                if (acceptedTransform != null)
                {
                    acceptedGenomeText = acceptedTransform.GetComponent<Text>();
                }
            }

            if (factorCircles == null)
            {
                factorCircles = GetComponentInChildren<GenomeMdsFitnessPathFactorCircles>(true);
            }
        }

        public void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        public void ShowStep(int stepIndex, int totalAccepted, float fitness, float[] factorScores)
        {
            EnsureWired();

            if (acceptedGenomeText != null)
            {
                int displayStep = Mathf.Clamp(stepIndex + 1, 1, Mathf.Max(totalAccepted, 1));
                acceptedGenomeText.text = $"Accepted genome: {displayStep} / {totalAccepted}";
            }

            if (fitnessText != null)
            {
                fitnessText.text = $"Fitness: {fitness:G6}";
            }

            factorCircles?.UpdateScores(factorScores);
            SetVisible(true);
        }

        public void Clear()
        {
            if (acceptedGenomeText != null)
            {
                acceptedGenomeText.text = "Accepted genome: — / —";
            }

            if (fitnessText != null)
            {
                fitnessText.text = "Fitness: —";
            }

            factorCircles?.UpdateScores(null);
        }
    }
}
