#if UNITY_EDITOR

using System.Collections.Generic;
using RbnVisualisation.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RbnVisualisation.Menu.Editor
{
    /// <summary>
    /// Removes duplicate scene hierarchies and legacy roots without touching AppMenuController wiring.
    /// </summary>
    public static class AppSceneCleanupMenu
    {
        private static readonly string[] RemoveAllRootNames =
        {
            "DualPaneMdsAttractorViewRoot",
            "TrajectoryRoot",
            "TrajectoryUiPanel",
        };

        private static readonly string[] DeduplicateRootNames =
        {
            "Option10DualPaneMdsAttractorViewRoot",
            "GenomeMdsPathRoot",
            "GenomeMdsFitnessPathRoot",
            "BooleanAttractorLandscapeRoot",
            "SharedGenomeMdsPathRoot",
            "BatchGenomeMdsPathRoot",
            "BatchGenomeMdsFitnessPathRoot",
            "AppMenuController",
        };

        [MenuItem("RBN/Clean Scene Duplicates")]
        public static void CleanSceneDuplicates()
        {
            int removedObjects = 0;
            int removedMissingScripts = 0;

            removedObjects += RemoveAllNamedRoots(RemoveAllRootNames);
            removedObjects += RemoveLegacyUmapAndSolutionRoots();
            removedObjects += DeduplicateViewRoots(CollectWiredObjects());
            removedMissingScripts += RemoveMissingScriptsInScene();

            if (removedObjects > 0 || removedMissingScripts > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            Debug.Log(
                $"RBN scene cleanup complete. Removed {removedObjects} GameObject(s) and " +
                $"{removedMissingScripts} missing script component(s).");
        }

        /// <summary>
        /// Batch-mode entry point: Unity -batchmode -quit -projectPath ... -executeMethod ...
        /// </summary>
        public static void CleanActiveSceneBatch()
        {
            CleanSceneDuplicates();
            EditorSceneManager.SaveOpenScenes();
            EditorApplication.Exit(0);
        }

        private static HashSet<GameObject> CollectWiredObjects()
        {
            HashSet<GameObject> keep = new HashSet<GameObject>();
            AppMenuController controller = Object.FindFirstObjectByType<AppMenuController>();
            if (controller == null)
            {
                return keep;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty property = serializedController.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    Object reference = property.objectReferenceValue;
                    switch (reference)
                    {
                        case GameObject gameObject:
                            keep.Add(gameObject);
                            break;
                        case Component component:
                            keep.Add(component.gameObject);
                            break;
                    }
                }
                while (property.NextVisible(false));
            }

            return keep;
        }

        private static int RemoveAllNamedRoots(IEnumerable<string> names)
        {
            HashSet<string> nameSet = new HashSet<string>(names);
            int removed = 0;

            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = allObjects.Length - 1; index >= 0; index--)
            {
                GameObject candidate = allObjects[index];
                if (candidate != null && nameSet.Contains(candidate.name))
                {
                    Object.DestroyImmediate(candidate);
                    removed++;
                }
            }

            return removed;
        }

        private static int RemoveLegacyUmapAndSolutionRoots()
        {
            int removed = 0;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = allObjects.Length - 1; index >= 0; index--)
            {
                GameObject candidate = allObjects[index];
                if (candidate == null)
                {
                    continue;
                }

                switch (candidate.name)
                {
                    case "SharedGenomeSolutionMdsRoot":
                    case "GenomeUmapPathRoot":
                    case "SharedGenomeUmapPathRoot":
                        Object.DestroyImmediate(candidate);
                        removed++;
                        break;
                }
            }

            return removed;
        }

        private static int DeduplicateViewRoots(HashSet<GameObject> wiredObjects)
        {
            int removed = 0;

            foreach (string rootName in DeduplicateRootNames)
            {
                List<GameObject> matches = FindAllByName(rootName);
                if (matches.Count <= 1)
                {
                    continue;
                }

                GameObject keeper = null;
                foreach (GameObject wired in wiredObjects)
                {
                    if (wired != null && wired.name == rootName)
                    {
                        keeper = wired;
                        break;
                    }
                }

                if (keeper == null)
                {
                    keeper = matches[0];
                }

                for (int index = 0; index < matches.Count; index++)
                {
                    GameObject candidate = matches[index];
                    if (candidate != null && candidate != keeper)
                    {
                        Object.DestroyImmediate(candidate);
                        removed++;
                    }
                }
            }

            return removed;
        }

        private static List<GameObject> FindAllByName(string objectName)
        {
            List<GameObject> matches = new List<GameObject>();
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < allObjects.Length; index++)
            {
                GameObject candidate = allObjects[index];
                if (candidate != null && candidate.name == objectName)
                {
                    matches.Add(candidate);
                }
            }

            return matches;
        }

        private static int RemoveMissingScriptsInScene()
        {
            int removed = 0;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < allObjects.Length; index++)
            {
                GameObject candidate = allObjects[index];
                if (candidate == null)
                {
                    continue;
                }

                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(candidate);
                if (missingCount > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(candidate);
                    removed += missingCount;
                }
            }

            return removed;
        }
    }
}

#endif
