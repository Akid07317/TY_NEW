using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CampusRPG.Editor
{
    public static class CombatImportedPlayerVisualUtility
    {
        private const string ImportedVisualRootName = "ImportedVisualRoot";
        private const string ImportedWeaponVisualRootName = "ImportedWeaponVisualRoot";
        private const string ProxyRootName = "CombatProxyVisualRoot";
        private const string LocalPreviewMaterialFolder = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview/Materials/Player";
        private const string LocalImportedSourcePreferenceKey = "CampusRPG.CombatTest.UseImportedPlayerSources";
        private const bool DefaultUseImportedPlayerSourcesForLocalPreview = false;
        private const string ToggleImportedSourceMenu = "CampusRPG/Setup/CombatTest/Prefer Imported Player Sources When Available";

        private static readonly string[] PlayerVisualPrefabCandidatePaths =
        {
            "Assets/JC_LP_MedievalCharacters_LITE/Prefabs/SM_MedievalMaleLite_01.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanF_Dummy_Red.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab"
        };

        private static readonly string[] PlayerAvatarCandidatePaths =
        {
            "Assets/JC_LP_MedievalCharacters_LITE/Models/SM_MedievalMaleLite_01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Models/HumanF_Model.fbx",
            "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx"
        };

        private static readonly string[] PlayerWeaponPrefabCandidatePaths =
        {
            "Assets/Free medieval weapons/Prefabs/Sword_DH.prefab",
            "Assets/MYFG-Weapon Pack Lite/Perfabs/Two-Handed Sword/TH_Sword08.prefab",
            "Assets/MYFG-Weapon Pack Lite/Perfabs/Two-Handed Sword/TH_Sword05.prefab",
            "Assets/MYFG-Weapon Pack Lite/Perfabs/Two-Handed Sword/TH_Sword13.prefab"
        };

        private static readonly string[] ImportedWeaponAnchorCandidateNames =
        {
            "RightHand",
            "Hand_R",
            "R_Hand",
            "mixamorig:RightHand",
            "Bip001 R Hand"
        };

        private static readonly Vector3 ImportedWeaponRootLocalPosition = new Vector3(0.02f, -0.02f, 0.02f);
        private static readonly Quaternion ImportedWeaponRootLocalRotation = Quaternion.Euler(8f, 18f, 92f);
        private static readonly Vector3 ImportedWeaponRootLocalScale = Vector3.one;
        private static readonly Vector3 ImportedWeaponVisualLocalPosition = Vector3.zero;
        private static readonly Quaternion ImportedWeaponVisualLocalRotation = Quaternion.Euler(0f, 0f, -90f);
        private static readonly Vector3 ImportedWeaponVisualLocalScale = new Vector3(1.05f, 1.05f, 1.05f);

        public static bool UseImportedPlayerSourcesForLocalPreview
        {
            get => EditorPrefs.GetBool(LocalImportedSourcePreferenceKey, DefaultUseImportedPlayerSourcesForLocalPreview);
            set => EditorPrefs.SetBool(LocalImportedSourcePreferenceKey, value);
        }

        public static bool ShouldUseImportedPlayerSources =>
            UseImportedPlayerSourcesForLocalPreview && HasPlayerVisualSource();

        [MenuItem(ToggleImportedSourceMenu)]
        private static void ToggleImportedPlayerSourcesForLocalPreview()
        {
            UseImportedPlayerSourcesForLocalPreview = !UseImportedPlayerSourcesForLocalPreview;
            Debug.Log(
                UseImportedPlayerSourcesForLocalPreview
                    ? "CombatTest local preview now allows imported player sources when they are available. Standard Build/Repair paths still restore the proxy baseline."
                    : "CombatTest local preview reverted to proxy player sources. Standard Build/Repair paths remain on the proxy baseline.");
        }

        [MenuItem(ToggleImportedSourceMenu, true)]
        private static bool ToggleImportedPlayerSourcesForLocalPreviewValidation()
        {
            Menu.SetChecked(ToggleImportedSourceMenu, UseImportedPlayerSourcesForLocalPreview);
            return true;
        }

        public static bool HasPlayerVisualSource()
        {
            return LoadFirstAvailablePrefab(PlayerVisualPrefabCandidatePaths) != null;
        }

        public static string GetSelectedPlayerVisualPrefabPath()
        {
            return FindFirstCompatiblePath(PlayerVisualPrefabCandidatePaths);
        }

        public static string GetSelectedPlayerWeaponPrefabPath()
        {
            return FindFirstAvailablePath(PlayerWeaponPrefabCandidatePaths);
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

            Transform proxyRoot = actor.transform.Find(ProxyRootName);

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
            changed |= AlignImportedVisualToGround(visualInstance, actor.transform);
            changed |= NormalizePreviewMaterialsForBuiltinPipeline(visualInstance);

            if (rootAnimator != null && avatar != null && rootAnimator.avatar != avatar)
            {
                rootAnimator.avatar = avatar;
                EditorUtility.SetDirty(rootAnimator);
                changed = true;
            }

            changed |= SyncImportedWeaponPreview(actor, rootAnimator);

            EditorUtility.SetDirty(visualInstance.transform);
            EditorUtility.SetDirty(visualInstance);
            return changed;
        }

        public static bool RemoveImportedVisual(GameObject actor, Animator rootAnimator)
        {
            if (actor == null)
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

            changed |= RemoveImportedWeaponPreview(actor);
            changed |= SetProxyWeaponRenderersEnabled(actor.transform, true);

            if (rootAnimator != null && rootAnimator.avatar != null)
            {
                rootAnimator.avatar = null;
                EditorUtility.SetDirty(rootAnimator);
                changed = true;
            }

            return changed;
        }

        public static bool SyncImportedWeaponPreview(GameObject actor, Animator rootAnimator)
        {
            if (actor == null)
            {
                return false;
            }

            bool changed = RemoveImportedWeaponPreview(actor);
            Transform importedVisualRoot = actor.transform.Find(ImportedVisualRootName);

            if (importedVisualRoot == null)
            {
                changed |= SetProxyWeaponRenderersEnabled(actor.transform, true);
                return changed;
            }

            GameObject weaponPrefab = LoadFirstAvailableGenericPrefab(PlayerWeaponPrefabCandidatePaths);
            Transform weaponAnchor = ResolveImportedWeaponAnchor(actor.transform, rootAnimator);

            if (weaponPrefab == null || weaponAnchor == null)
            {
                changed |= SetProxyWeaponRenderersEnabled(actor.transform, true);
                return changed;
            }

            GameObject weaponRoot = new GameObject(ImportedWeaponVisualRootName);
            weaponRoot.transform.SetParent(weaponAnchor, false);
            weaponRoot.transform.localPosition = ImportedWeaponRootLocalPosition;
            weaponRoot.transform.localRotation = ImportedWeaponRootLocalRotation;
            weaponRoot.transform.localScale = ImportedWeaponRootLocalScale;

            GameObject weaponInstance = (GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab);

            if (weaponInstance == null)
            {
                Object.DestroyImmediate(weaponRoot);
                changed |= SetProxyWeaponRenderersEnabled(actor.transform, true);
                return changed;
            }

            weaponInstance.name = weaponPrefab.name;
            weaponInstance.transform.SetParent(weaponRoot.transform, false);
            weaponInstance.transform.localPosition = ImportedWeaponVisualLocalPosition;
            weaponInstance.transform.localRotation = ImportedWeaponVisualLocalRotation;
            weaponInstance.transform.localScale = ImportedWeaponVisualLocalScale;

            StripImportedVisualComponents(weaponInstance);
            changed |= NormalizePreviewMaterialsForBuiltinPipeline(weaponInstance);
            changed |= SetProxyWeaponRenderersEnabled(actor.transform, false);

            EditorUtility.SetDirty(weaponRoot.transform);
            EditorUtility.SetDirty(weaponRoot);
            EditorUtility.SetDirty(weaponInstance.transform);
            EditorUtility.SetDirty(weaponInstance);
            return true;
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

        private static Transform ResolveImportedWeaponAnchor(Transform actorRoot, Animator rootAnimator)
        {
            if (rootAnimator != null && rootAnimator.avatar != null && rootAnimator.isHuman)
            {
                Transform handBone = rootAnimator.GetBoneTransform(HumanBodyBones.RightHand);

                if (handBone != null)
                {
                    return handBone;
                }
            }

            Transform importedRoot = actorRoot != null ? actorRoot.Find(ImportedVisualRootName) : null;

            if (importedRoot == null)
            {
                return null;
            }

            for (int i = 0; i < ImportedWeaponAnchorCandidateNames.Length; i++)
            {
                Transform candidate = FindDeepChild(importedRoot, ImportedWeaponAnchorCandidateNames[i]);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool RemoveImportedWeaponPreview(GameObject actor)
        {
            if (actor == null)
            {
                return false;
            }

            bool changed = false;
            Transform[] transforms = actor.GetComponentsInChildren<Transform>(true);

            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                Transform transform = transforms[i];

                if (transform == null || transform == actor.transform || transform.name != ImportedWeaponVisualRootName)
                {
                    continue;
                }

                Object.DestroyImmediate(transform.gameObject);
                changed = true;
            }

            return changed;
        }

        private static bool SetProxyWeaponRenderersEnabled(Transform actorRoot, bool enabled)
        {
            Transform proxyWeaponGrip = actorRoot != null ? actorRoot.Find($"{ProxyRootName}/WeaponGrip") : null;

            if (proxyWeaponGrip == null)
            {
                return false;
            }

            bool changed = false;
            Renderer[] renderers = proxyWeaponGrip.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled == enabled)
                {
                    continue;
                }

                renderers[i].enabled = enabled;
                EditorUtility.SetDirty(renderers[i]);
                changed = true;
            }

            return changed;
        }

        private static GameObject LoadFirstAvailablePrefab(string[] candidatePaths)
        {
            string selectedPath = FindFirstCompatiblePath(candidatePaths);
            return string.IsNullOrWhiteSpace(selectedPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
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

        private static GameObject LoadFirstAvailableGenericPrefab(string[] candidatePaths)
        {
            string selectedPath = FindFirstAvailablePath(candidatePaths);
            return string.IsNullOrWhiteSpace(selectedPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
        }

        private static string FindFirstCompatiblePath(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePaths[i]);

                if (prefab != null && IsCompatibleWithHumanoidPreview(prefab))
                {
                    return candidatePaths[i];
                }
            }

            return null;
        }

        private static string FindFirstAvailablePath(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(candidatePaths[i]) != null)
                {
                    return candidatePaths[i];
                }
            }

            return null;
        }

        private static bool IsCompatibleWithHumanoidPreview(GameObject visualPrefab)
        {
            if (visualPrefab == null)
            {
                return false;
            }

            if (visualPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
            {
                return false;
            }

            return FindAvatar(visualPrefab) != null;
        }

        private static bool AlignImportedVisualToGround(GameObject visualRoot, Transform actorRoot)
        {
            if (visualRoot == null || actorRoot == null)
            {
                return false;
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
                renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            }

            if (renderers.Length == 0)
            {
                return false;
            }

            float minY = float.PositiveInfinity;

            for (int i = 0; i < renderers.Length; i++)
            {
                minY = Mathf.Min(minY, renderers[i].bounds.min.y);
            }

            if (!float.IsFinite(minY))
            {
                return false;
            }

            float desiredLift = actorRoot.position.y - minY;

            if (Mathf.Abs(desiredLift) <= 0.0001f)
            {
                return false;
            }

            Vector3 worldPosition = visualRoot.transform.position;
            worldPosition.y += desiredLift;
            visualRoot.transform.position = worldPosition;
            return true;
        }

        private static bool NormalizePreviewMaterialsForBuiltinPipeline(GameObject visualRoot)
        {
            if (visualRoot == null || GraphicsSettings.defaultRenderPipeline != null)
            {
                return false;
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            bool changed = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] sharedMaterials = renderers[i].sharedMaterials;
                bool rendererChanged = false;

                for (int j = 0; j < sharedMaterials.Length; j++)
                {
                    Material sourceMaterial = sharedMaterials[j];

                    if (!ShouldCreateBuiltinPreviewMaterial(sourceMaterial))
                    {
                        continue;
                    }

                    Material previewMaterial = GetOrCreateBuiltinPreviewMaterial(sourceMaterial);

                    if (previewMaterial == null || ReferenceEquals(sourceMaterial, previewMaterial))
                    {
                        continue;
                    }

                    sharedMaterials[j] = previewMaterial;
                    rendererChanged = true;
                }

                if (!rendererChanged)
                {
                    continue;
                }

                renderers[i].sharedMaterials = sharedMaterials;
                EditorUtility.SetDirty(renderers[i]);
                changed = true;
            }

            return changed;
        }

        private static bool ShouldCreateBuiltinPreviewMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return false;
            }

            Shader shader = sourceMaterial.shader;
            string shaderName = shader != null ? shader.name : string.Empty;
            return string.IsNullOrWhiteSpace(shaderName)
                || (shader != null && !shader.isSupported)
                || shaderName.Contains("InternalErrorShader", System.StringComparison.OrdinalIgnoreCase)
                || shaderName.Contains("HDRP", System.StringComparison.OrdinalIgnoreCase)
                || shaderName.Contains("High Definition", System.StringComparison.OrdinalIgnoreCase)
                || shaderName.Contains("Universal Render Pipeline", System.StringComparison.OrdinalIgnoreCase)
                || shaderName.StartsWith("Hidden/HDRP", System.StringComparison.OrdinalIgnoreCase);
        }

        private static Material GetOrCreateBuiltinPreviewMaterial(Material sourceMaterial)
        {
            Shader standardShader = Shader.Find("Standard");

            if (sourceMaterial == null || standardShader == null)
            {
                return sourceMaterial;
            }

            EnsureFolder(LocalPreviewMaterialFolder);

            string sourceAssetPath = AssetDatabase.GetAssetPath(sourceMaterial);
            string sourceGuid = string.IsNullOrWhiteSpace(sourceAssetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(sourceAssetPath);
            string safeName = SanitizeFileName(sourceMaterial.name);
            string suffix = string.IsNullOrWhiteSpace(sourceGuid)
                ? "preview"
                : sourceGuid.Substring(0, Mathf.Min(8, sourceGuid.Length));
            string previewMaterialPath = $"{LocalPreviewMaterialFolder}/{safeName}_{suffix}.mat";

            Material previewMaterial = AssetDatabase.LoadAssetAtPath<Material>(previewMaterialPath);

            if (previewMaterial == null)
            {
                previewMaterial = new Material(standardShader)
                {
                    name = $"{sourceMaterial.name}_BuiltinPreview"
                };

                AssetDatabase.CreateAsset(previewMaterial, previewMaterialPath);
            }

            CopyColorAndTextureProperties(sourceMaterial, previewMaterial);
            EditorUtility.SetDirty(previewMaterial);
            AssetDatabase.SaveAssets();
            return previewMaterial;
        }

        private static void CopyColorAndTextureProperties(Material sourceMaterial, Material previewMaterial)
        {
            if (sourceMaterial == null || previewMaterial == null)
            {
                return;
            }

            Texture mainTexture = GetFirstTexture(
                sourceMaterial,
                "_BaseColorMap",
                "_BaseMap",
                "_MainTex");
            Color color = GetFirstColor(
                sourceMaterial,
                "_BaseColor",
                "_Color");

            previewMaterial.shader = Shader.Find("Standard");
            previewMaterial.color = color;
            previewMaterial.mainTexture = mainTexture;
            previewMaterial.SetFloat("_Glossiness", 0f);
            previewMaterial.SetFloat("_Metallic", 0f);

            if (previewMaterial.HasProperty("_MainTex"))
            {
                previewMaterial.SetTexture("_MainTex", mainTexture);
            }
        }

        private static Texture GetFirstTexture(Material material, params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (material.HasProperty(propertyNames[i]))
                {
                    Texture texture = material.GetTexture(propertyNames[i]);

                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static Color GetFirstColor(Material material, params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (material.HasProperty(propertyNames[i]))
                {
                    return material.GetColor(propertyNames[i]);
                }
            }

            return Color.white;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "PreviewMaterial";
            }

            char[] invalidCharacters = System.IO.Path.GetInvalidFileNameChars();
            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                builder.Append(System.Array.IndexOf(invalidCharacters, character) >= 0 ? '_' : character);
            }

            return builder.ToString().Replace(' ', '_');
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeepChild(root.GetChild(i), targetName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
