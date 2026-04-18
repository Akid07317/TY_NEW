using UnityEngine;

namespace CampusRPG.UI
{
    public static class BossTelegraphVisualUtility
    {
        public static void EnsureVisual(
            Transform parent,
            string visualName,
            GameObject desiredTemplate,
            ref GameObject visual,
            ref Renderer renderer,
            ref GameObject currentVisualTemplate,
            ref Material runtimeMaterial,
            ref Material currentMaterialTemplate)
        {
            if (visual != null && currentVisualTemplate == desiredTemplate)
            {
                return;
            }

            DestroyVisualAndMaterial(
                ref visual,
                ref renderer,
                ref currentVisualTemplate,
                ref runtimeMaterial,
                ref currentMaterialTemplate);

            visual = desiredTemplate != null
                ? Object.Instantiate(desiredTemplate, parent, false)
                : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = visualName;
            visual.transform.SetParent(parent, false);
            currentVisualTemplate = desiredTemplate;

            RemoveColliders(visual);
            renderer = visual.GetComponentInChildren<Renderer>();
            visual.SetActive(false);
        }

        public static void ApplyRuntimeMaterial(
            Renderer renderer,
            Material desiredTemplate,
            Color color,
            ref Material runtimeMaterial,
            ref Material currentMaterialTemplate)
        {
            if (renderer == null)
            {
                return;
            }

            if (runtimeMaterial == null || currentMaterialTemplate != desiredTemplate)
            {
                DestroyRuntimeMaterial(ref runtimeMaterial);
                runtimeMaterial = desiredTemplate != null ? new Material(desiredTemplate) : new Material(ResolveFallbackShader());
                renderer.sharedMaterial = runtimeMaterial;
                currentMaterialTemplate = desiredTemplate;
            }

            runtimeMaterial.color = color;
        }

        public static void DestroyVisualAndMaterial(
            ref GameObject visual,
            ref Renderer renderer,
            ref GameObject currentVisualTemplate,
            ref Material runtimeMaterial,
            ref Material currentMaterialTemplate)
        {
            DestroyRuntimeMaterial(ref runtimeMaterial);
            currentMaterialTemplate = null;

            if (visual != null)
            {
                DestroyObject(visual);
            }

            visual = null;
            renderer = null;
            currentVisualTemplate = null;
        }

        private static void DestroyRuntimeMaterial(ref Material runtimeMaterial)
        {
            if (runtimeMaterial != null)
            {
                DestroyObject(runtimeMaterial);
            }

            runtimeMaterial = null;
        }

        private static void RemoveColliders(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                DestroyObject(colliders[i]);
            }
        }

        private static void DestroyObject(Object instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(instance);
            }
            else
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Shader ResolveFallbackShader()
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
    }
}
