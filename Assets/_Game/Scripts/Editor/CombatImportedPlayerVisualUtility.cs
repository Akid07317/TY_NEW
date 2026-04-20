using UnityEditor;
using UnityEngine;

namespace CampusRPG.Editor
{
    public static class CombatImportedPlayerVisualUtility
    {
        private const string ImportedVisualRootName = "ImportedVisualRoot";

        private static readonly string[] PlayerVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab",
            "Assets/JC_LP_MedievalCharacters_LITE/Prefabs/SM_MedievalMaleLite_01.prefab"
        };

        private static readonly string[] PlayerAvatarCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx",
            "Assets/JC_LP_MedievalCharacters_LITE/Models/SM_MedievalMaleLite_01.fbx"
        };

        public static bool HasPlayerVisualSource()
        {
            return LoadFirstAvailablePrefab(PlayerVisualPrefabCandidatePaths) != null;
        }

        public static bool TryApply(GameObject actor, Animator rootAnimator)
        {
            if (actor == null)
            {
                return false;
            }

            GameObject visualPrefab = LoadFirstAvailablePrefab(PlayerVisualPrefabCandidatePaths);

            if (visualPrefab == null)
            {
                return false;
            }

            bool changed = false;
            Transform importedVisualRoot = actor.transform.Find(ImportedVisualRootName);

            if (importedVisualRoot != null)
            {
                Object.DestroyImmediate(importedVisualRoot.gameObject);
                changed = true;
            }

            Transform proxyRoot = actor.transform.Find("CombatProxyVisualRoot");

            if (proxyRoot != null)
            {
                Object.DestroyImmediate(proxyRoot.gameObject);
                changed = true;
            }

            GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);

            if (visualInstance == null)
            {
                return changed;
            }

            visualInstance.name = ImportedVisualRootName;
            visualInstance.transform.SetParent(actor.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;
            changed = true;

            Avatar avatar = FindAvatar(visualInstance);
            StripImportedVisualComponents(visualInstance);

            if (rootAnimator != null && avatar != null && rootAnimator.avatar != avatar)
            {
                rootAnimator.avatar = avatar;
                EditorUtility.SetDirty(rootAnimator);
                changed = true;
            }

            EditorUtility.SetDirty(visualInstance);
            return changed;
        }

        private static void StripImportedVisualComponents(GameObject visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                Object.DestroyImmediate(animators[i]);
            }

            MonoBehaviour[] behaviours = visualRoot.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                Object.DestroyImmediate(behaviours[i]);
            }

            Animation[] legacyAnimations = visualRoot.GetComponentsInChildren<Animation>(true);

            for (int i = 0; i < legacyAnimations.Length; i++)
            {
                Object.DestroyImmediate(legacyAnimations[i]);
            }

            Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }

            Rigidbody[] rigidbodies = visualRoot.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Object.DestroyImmediate(rigidbodies[i]);
            }

            CharacterController[] characterControllers = visualRoot.GetComponentsInChildren<CharacterController>(true);

            for (int i = 0; i < characterControllers.Length; i++)
            {
                Object.DestroyImmediate(characterControllers[i]);
            }
        }

        private static Avatar FindAvatar(GameObject visualInstance)
        {
            Animator animator = visualInstance != null ? visualInstance.GetComponentInChildren<Animator>(true) : null;

            if (animator != null && animator.avatar != null)
            {
                return animator.avatar;
            }

            return LoadFirstAvailableAvatar(PlayerAvatarCandidatePaths);
        }

        private static GameObject LoadFirstAvailablePrefab(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePaths[i]);

                if (prefab != null)
                {
                    return prefab;
                }
            }

            return null;
        }

        private static Avatar LoadFirstAvailableAvatar(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(candidatePaths[i]);

                for (int j = 0; j < assets.Length; j++)
                {
                    Avatar avatar = assets[j] as Avatar;

                    if (avatar != null && avatar.isValid)
                    {
                        return avatar;
                    }
                }
            }

            return null;
        }
    }
}
