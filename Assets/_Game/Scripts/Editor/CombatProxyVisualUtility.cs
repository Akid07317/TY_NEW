using UnityEditor;
using UnityEngine;

namespace CampusRPG.Editor
{
    public enum CombatProxyVisualKind
    {
        Player,
        EnemyMelee,
        EnemyMobile,
        EnemyRanged
    }

    public static class CombatProxyVisualUtility
    {
        private const string MaterialsFolder = "Assets/_Game/Materials";
        private const string ProxyRootName = "CombatProxyVisualRoot";
        private const string WeaponGripName = "WeaponGrip";

        public static bool Apply(GameObject actor, CombatProxyVisualKind kind)
        {
            if (actor == null)
            {
                return false;
            }

            bool changed = RemoveRootPrimitiveRenderers(actor);
            Transform proxyRoot = actor.transform.Find(ProxyRootName);
            bool hasExternalVisuals = HasExternalVisuals(actor.transform, proxyRoot);

            if (hasExternalVisuals)
            {
                if (kind != CombatProxyVisualKind.Player)
                {
                    if (proxyRoot != null)
                    {
                        Object.DestroyImmediate(proxyRoot.gameObject);
                        changed = true;
                    }

                    return changed;
                }

                if (proxyRoot != null)
                {
                    Object.DestroyImmediate(proxyRoot.gameObject);
                    changed = true;
                }

                EnsureFolder(MaterialsFolder);
                Material overlayPrimaryMaterial = CreateOrLoadMaterial(GetPrimaryMaterialPath(kind), GetPrimaryColor(kind));
                Material overlayAccentMaterial = CreateOrLoadMaterial(GetAccentMaterialPath(kind), GetAccentColor(kind));
                Transform overlayRoot = CreateTransformChild(actor.transform, ProxyRootName, Vector3.zero, Quaternion.identity, Vector3.one);
                BuildImportedPlayerOverlay(overlayRoot, overlayPrimaryMaterial, overlayAccentMaterial);
                return true;
            }

            if (proxyRoot != null)
            {
                Object.DestroyImmediate(proxyRoot.gameObject);
                changed = true;
            }

            EnsureFolder(MaterialsFolder);
            Material primaryMaterial = CreateOrLoadMaterial(GetPrimaryMaterialPath(kind), GetPrimaryColor(kind));
            Material accentMaterial = CreateOrLoadMaterial(GetAccentMaterialPath(kind), GetAccentColor(kind));
            Transform newProxyRoot = CreateTransformChild(actor.transform, ProxyRootName, Vector3.zero, Quaternion.identity, Vector3.one);
            BuildBaseHumanoid(newProxyRoot, primaryMaterial, accentMaterial);
            AddVariantDetails(newProxyRoot, kind, primaryMaterial, accentMaterial);
            changed = true;

            return changed;
        }

        private static void BuildImportedPlayerOverlay(Transform root, Material primaryMaterial, Material accentMaterial)
        {
            CreatePrimitive(root, "ForwardMarker", PrimitiveType.Cube, new Vector3(0f, 0.94f, 0.62f), Quaternion.identity, new Vector3(0.14f, 0.18f, 0.56f), accentMaterial);
            Transform weaponGrip = CreateTransformChild(root, WeaponGripName, new Vector3(0.3f, 1f, 0.38f), Quaternion.Euler(2f, 16f, 72f), Vector3.one);
            CreatePrimitive(weaponGrip, "Handle", PrimitiveType.Cube, new Vector3(-0.18f, 0f, 0f), Quaternion.identity, new Vector3(0.32f, 0.06f, 0.06f), accentMaterial);
            CreatePrimitive(weaponGrip, "Pommel", PrimitiveType.Sphere, new Vector3(-0.34f, 0f, 0f), Quaternion.identity, new Vector3(0.1f, 0.1f, 0.1f), accentMaterial);
            CreatePrimitive(weaponGrip, "Guard", PrimitiveType.Cube, new Vector3(0.03f, 0f, 0f), Quaternion.identity, new Vector3(0.1f, 0.18f, 0.22f), accentMaterial);
            CreatePrimitive(weaponGrip, "Blade", PrimitiveType.Cube, new Vector3(0.64f, 0f, 0f), Quaternion.identity, new Vector3(1.28f, 0.1f, 0.1f), primaryMaterial);
        }

        private static bool RemoveRootPrimitiveRenderers(GameObject actor)
        {
            bool changed = false;
            MeshRenderer meshRenderer = actor.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                Object.DestroyImmediate(meshRenderer);
                changed = true;
            }

            MeshFilter meshFilter = actor.GetComponent<MeshFilter>();

            if (meshFilter != null)
            {
                Object.DestroyImmediate(meshFilter);
                changed = true;
            }

            return changed;
        }

        private static bool HasExternalVisuals(Transform actorRoot, Transform proxyRoot)
        {
            Renderer[] renderers = actorRoot.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Transform rendererTransform = renderers[i].transform;

                if (rendererTransform == actorRoot)
                {
                    continue;
                }

                if (proxyRoot != null && rendererTransform.IsChildOf(proxyRoot))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void BuildBaseHumanoid(Transform root, Material primaryMaterial, Material accentMaterial)
        {
            CreatePrimitive(root, "Feet", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.06f), Quaternion.identity, new Vector3(0.42f, 0.12f, 0.32f), primaryMaterial);
            CreatePrimitive(root, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.88f, 0.04f), Quaternion.identity, new Vector3(0.42f, 0.52f, 0.34f), primaryMaterial);
            CreatePrimitive(root, "Chest", PrimitiveType.Cube, new Vector3(0f, 1.12f, 0.24f), Quaternion.identity, new Vector3(0.38f, 0.22f, 0.18f), accentMaterial);
            CreatePrimitive(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0.08f), Quaternion.identity, new Vector3(0.26f, 0.26f, 0.24f), primaryMaterial);
            CreatePrimitive(root, "LeftShoulder", PrimitiveType.Cube, new Vector3(-0.24f, 1.12f, 0.08f), Quaternion.identity, new Vector3(0.16f, 0.18f, 0.18f), accentMaterial);
            CreatePrimitive(root, "RightShoulder", PrimitiveType.Cube, new Vector3(0.24f, 1.12f, 0.08f), Quaternion.identity, new Vector3(0.16f, 0.18f, 0.18f), accentMaterial);
            CreatePrimitive(root, "ForwardMarker", PrimitiveType.Cube, new Vector3(0f, 0.94f, 0.62f), Quaternion.identity, new Vector3(0.14f, 0.18f, 0.56f), accentMaterial);
        }

        private static void AddVariantDetails(Transform root, CombatProxyVisualKind kind, Material primaryMaterial, Material accentMaterial)
        {
            switch (kind)
            {
                case CombatProxyVisualKind.Player:
                    CreatePrimitive(root, "Guard", PrimitiveType.Cube, new Vector3(0.26f, 0.95f, 0.34f), Quaternion.Euler(0f, 0f, 20f), new Vector3(0.14f, 0.08f, 0.12f), accentMaterial);
                    CreatePrimitive(root, "Blade", PrimitiveType.Cube, new Vector3(0.34f, 1.04f, 0.54f), Quaternion.Euler(0f, 12f, 62f), new Vector3(0.07f, 0.07f, 0.82f), accentMaterial);
                    CreatePrimitive(root, "BackCore", PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.12f), Quaternion.identity, new Vector3(0.24f, 0.16f, 0.16f), primaryMaterial);
                    break;
                case CombatProxyVisualKind.EnemyMelee:
                    CreatePrimitive(root, "MeleeShoulderLeft", PrimitiveType.Cube, new Vector3(-0.3f, 1.18f, 0.04f), Quaternion.identity, new Vector3(0.2f, 0.16f, 0.22f), primaryMaterial);
                    CreatePrimitive(root, "MeleeShoulderRight", PrimitiveType.Cube, new Vector3(0.3f, 1.18f, 0.04f), Quaternion.identity, new Vector3(0.2f, 0.16f, 0.22f), primaryMaterial);
                    CreatePrimitive(root, "MeleeBlade", PrimitiveType.Cube, new Vector3(0.34f, 1f, 0.44f), Quaternion.Euler(0f, -8f, 76f), new Vector3(0.08f, 0.08f, 0.76f), accentMaterial);
                    break;
                case CombatProxyVisualKind.EnemyMobile:
                    CreatePrimitive(root, "MobileFinLeft", PrimitiveType.Cube, new Vector3(-0.28f, 0.9f, 0.08f), Quaternion.Euler(0f, 0f, -30f), new Vector3(0.12f, 0.34f, 0.08f), accentMaterial);
                    CreatePrimitive(root, "MobileFinRight", PrimitiveType.Cube, new Vector3(0.28f, 0.9f, 0.08f), Quaternion.Euler(0f, 0f, 30f), new Vector3(0.12f, 0.34f, 0.08f), accentMaterial);
                    CreatePrimitive(root, "MobileTail", PrimitiveType.Cube, new Vector3(0f, 0.74f, -0.18f), Quaternion.identity, new Vector3(0.12f, 0.18f, 0.4f), primaryMaterial);
                    break;
                case CombatProxyVisualKind.EnemyRanged:
                    CreatePrimitive(root, "FocusOrb", PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0.42f), Quaternion.identity, new Vector3(0.18f, 0.18f, 0.18f), accentMaterial);
                    CreatePrimitive(root, "Staff", PrimitiveType.Cylinder, new Vector3(0.28f, 0.95f, 0.18f), Quaternion.Euler(0f, 0f, 10f), new Vector3(0.05f, 0.55f, 0.05f), primaryMaterial);
                    CreatePrimitive(root, "CasterPack", PrimitiveType.Cube, new Vector3(0f, 1.04f, -0.14f), Quaternion.identity, new Vector3(0.28f, 0.26f, 0.18f), accentMaterial);
                    break;
            }
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;

            Collider collider = primitive.GetComponent<Collider>();

            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = primitive.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }

            return primitive;
        }

        private static Transform CreateTransformChild(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = localRotation;
            child.transform.localScale = localScale;
            return child.transform;
        }

        private static string GetPrimaryMaterialPath(CombatProxyVisualKind kind)
        {
            switch (kind)
            {
                case CombatProxyVisualKind.Player:
                    return MaterialsFolder + "/M_CombatProxy_PlayerPrimary.mat";
                case CombatProxyVisualKind.EnemyMelee:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMeleePrimary.mat";
                case CombatProxyVisualKind.EnemyMobile:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMobilePrimary.mat";
                default:
                    return MaterialsFolder + "/M_CombatProxy_EnemyRangedPrimary.mat";
            }
        }

        private static string GetAccentMaterialPath(CombatProxyVisualKind kind)
        {
            switch (kind)
            {
                case CombatProxyVisualKind.Player:
                    return MaterialsFolder + "/M_CombatProxy_PlayerAccent.mat";
                case CombatProxyVisualKind.EnemyMelee:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMeleeAccent.mat";
                case CombatProxyVisualKind.EnemyMobile:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMobileAccent.mat";
                default:
                    return MaterialsFolder + "/M_CombatProxy_EnemyRangedAccent.mat";
            }
        }

        private static Color GetPrimaryColor(CombatProxyVisualKind kind)
        {
            switch (kind)
            {
                case CombatProxyVisualKind.Player:
                    return new Color(0.21f, 0.53f, 0.78f);
                case CombatProxyVisualKind.EnemyMelee:
                    return new Color(0.59f, 0.23f, 0.19f);
                case CombatProxyVisualKind.EnemyMobile:
                    return new Color(0.72f, 0.47f, 0.16f);
                default:
                    return new Color(0.26f, 0.42f, 0.62f);
            }
        }

        private static Color GetAccentColor(CombatProxyVisualKind kind)
        {
            switch (kind)
            {
                case CombatProxyVisualKind.Player:
                    return new Color(0.92f, 0.9f, 0.74f);
                case CombatProxyVisualKind.EnemyMelee:
                    return new Color(0.94f, 0.73f, 0.52f);
                case CombatProxyVisualKind.EnemyMobile:
                    return new Color(0.95f, 0.9f, 0.46f);
                default:
                    return new Color(0.58f, 0.86f, 0.95f);
            }
        }

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = ResolvePlaceholderShader();
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader ResolvePlaceholderShader()
        {
            Shader shader = Shader.Find("Unlit/Color");

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return shader;
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
    }
}
