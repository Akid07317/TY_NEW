using System.Reflection;
using CampusRPG.Combat;
using CampusRPG.Composition;
using CampusRPG.Core;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ProceduralAudioUtilityTests
    {
        [Test]
        public void ResolveSfxVolume_UsesActiveSceneAudioSettings()
        {
            GameObject contextObject = null;
            AudioSettingsSO audioSettings = null;

            try
            {
                contextObject = new GameObject("SceneRuntimeContext");
                SceneRuntimeContext sceneContext = contextObject.AddComponent<SceneRuntimeContext>();
                audioSettings = ScriptableObject.CreateInstance<AudioSettingsSO>();
                SetPrivateField(audioSettings, "masterVolume", 0.5f);
                SetPrivateField(audioSettings, "sfxVolume", 0.4f);
                SetPrivateField(sceneContext, "audioSettings", audioSettings);
                SetActiveContext(sceneContext);

                Assert.AreEqual(0.1f, ProceduralAudioUtility.ResolveSfxVolume(0.5f), 0.001f);
            }
            finally
            {
                SetActiveContext(null);

                if (audioSettings != null)
                {
                    Object.DestroyImmediate(audioSettings);
                }

                if (contextObject != null)
                {
                    Object.DestroyImmediate(contextObject);
                }
            }
        }

        [Test]
        public void ResolveSfxVolume_FallsBackToRequestedVolume_WithoutActiveContext()
        {
            SetActiveContext(null);
            Assert.AreEqual(0.35f, ProceduralAudioUtility.ResolveSfxVolume(0.35f), 0.001f);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void SetActiveContext(SceneRuntimeContext context)
        {
            PropertyInfo property = typeof(SceneRuntimeContext).GetProperty(
                "Active",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property);
            property.SetValue(null, context);
        }
    }
}
