using CampusRPG.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.Editor
{
    // Experimental local-preview helper only. Standard Build/Repair flows must restore proxy baseline.
    public static class CombatImportedEnemyVisualUtility
    {
        public const string ImportedVisualRootName = "ImportedEnemyVisualRoot";

        private const string ProxyRootName = "CombatProxyVisualRoot";
        private const string LocalPreviewAnimationFolder = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview";
        private const string ImportedAnimatorControllerPath = LocalPreviewAnimationFolder + "/AC_Enemy_ImportedPreview.controller";

        private static readonly string[] EnemyMeleeVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyMobileVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Polearm.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Dual Wield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyRangedVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/Prefabs/Human_BasicMotionsDummy_M.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanF_Dummy_Red.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyAvatarCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx",
            "Assets/Kevin Iglesias/Human Animations/Models/HumanF_Model.fbx"
        };

        private static readonly string[] IdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] WalkClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx"
        };

        private static readonly string[] RunClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx"
        };

        private static readonly string[] HitClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx"
        };

        private static readonly string[] DeathClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx"
        };

        private static readonly string[] MeleeAttackClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx"
        };

        private static readonly string[] MobileAttackClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@AttackPolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx"
        };

        private static readonly string[] RangedAttackClipCandidatePaths =
        {
            "Assets/DoubleL/Bow/Attack B/Bow_Attack_B_1_All.fbx",
            "Assets/DoubleL/Bow/Attack A/Bow_Attack_A_1_All.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx"
        };

        public static bool HasHumanoidVisualSource(CombatProxyVisualKind kind)
        {
            return LoadFirstHumanoidPrefab(GetCandidatePaths(kind)) != null;
        }

        public static string GetSelectedHumanoidVisualPrefabPath(CombatProxyVisualKind kind)
        {
            return FindFirstCompatibleHumanoidPath(GetCandidatePaths(kind));
        }

        public static RuntimeAnimatorController EnsureImportedAvatarPreviewController()
        {
            AnimationClip idleClip = LoadFirstAvailableAnimationClip(IdleClipCandidatePaths);
            AnimationClip walkClip = LoadFirstAvailableAnimationClip(WalkClipCandidatePaths);
            AnimationClip runClip = LoadFirstAvailableAnimationClip(RunClipCandidatePaths);
            AnimationClip hitClip = LoadFirstAvailableAnimationClip(HitClipCandidatePaths);
            AnimationClip deathClip = LoadFirstAvailableAnimationClip(DeathClipCandidatePaths);
            AnimationClip meleeAttackClip = LoadFirstAvailableAnimationClip(MeleeAttackClipCandidatePaths);
            AnimationClip mobileAttackClip = LoadFirstAvailableAnimationClip(MobileAttackClipCandidatePaths);
            AnimationClip rangedAttackClip = LoadFirstAvailableAnimationClip(RangedAttackClipCandidatePaths);

            if (idleClip == null
                || walkClip == null
                || runClip == null
                || hitClip == null
                || deathClip == null
                || meleeAttackClip == null
                || mobileAttackClip == null
                || rangedAttackClip == null)
            {
                return null;
            }

            EnsureFolder(LocalPreviewAnimationFolder);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ImportedAnimatorControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ImportedAnimatorControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ImportedAnimatorControllerPath);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.GroundSpeedParameterName, AnimatorControllerParameterType.Float);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            BlendTree locomotionBlendTree = new BlendTree
            {
                name = "BT_Enemy_Imported_Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = EnemyCombatAnimationPlanUtility.GroundSpeedParameterName,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(locomotionBlendTree, controller);
            locomotionBlendTree.AddChild(idleClip, 0f);
            locomotionBlendTree.AddChild(walkClip, 0.45f);
            locomotionBlendTree.AddChild(runClip, 1f);

            AnimatorState locomotionState = stateMachine.AddState(EnemyCombatAnimationPlanUtility.LocomotionStateName);
            locomotionState.motion = locomotionBlendTree;
            stateMachine.defaultState = locomotionState;

            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.HitStateName, hitClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.DeathStateName, deathClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.MeleeAttackStateName, meleeAttackClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.MobileAttackStateName, mobileAttackClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.RangedAttackStateName, rangedAttackClip);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return controller;
        }

        public static bool TryApplyHumanoidAvatarPreview(GameObject actor, CombatProxyVisualKind kind, Animator rootAnimator)
        {
            if (actor == null || rootAnimator == null)
            {
                return false;
            }

            Transform proxyRoot = actor.transform.Find(ProxyRootName);

            if (proxyRoot == null)
            {
                return false;
            }

            GameObject visualPrefab = LoadFirstHumanoidPrefab(GetCandidatePaths(kind));

            if (visualPrefab == null)
            {
                return false;
            }

            bool changed = RemoveImportedVisual(actor, rootAnimator);
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

            Avatar avatar = FindAvatar(visualInstance);

            if (avatar == null || !avatar.isValid)
            {
                Object.DestroyImmediate(visualInstance);
                return changed;
            }

            StripImportedVisualComponents(visualInstance);
            changed |= AlignImportedVisualToGround(visualInstance, actor.transform);
            changed |= SetProxyRenderersEnabled(proxyRoot, false);

            if (rootAnimator.avatar != avatar)
            {
                rootAnimator.avatar = avatar;
                changed = true;
            }

            rootAnimator.applyRootMotion = false;
            rootAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rootAnimator.updateMode = AnimatorUpdateMode.Normal;

            EditorUtility.SetDirty(rootAnimator);
            EditorUtility.SetDirty(visualInstance.transform);
            EditorUtility.SetDirty(visualInstance);
            return true;
        }

        public static bool RemoveImportedVisual(GameObject actor, Animator rootAnimator)
        {
            if (actor == null)
            {
                return false;
            }

            bool changed = false;
            Transform importedVisualRoot = actor.transform.Find(ImportedVisualRootName);
            Transform proxyRoot = actor.transform.Find(ProxyRootName);

            if (importedVisualRoot == null && proxyRoot != null)
            {
                importedVisualRoot = proxyRoot.Find(ImportedVisualRootName);
            }

            if (importedVisualRoot != null)
            {
                Object.DestroyImmediate(importedVisualRoot.gameObject);
                changed = true;
            }

            if (proxyRoot != null)
            {
                changed |= SetProxyRenderersEnabled(proxyRoot, true);
            }

            if (rootAnimator != null)
            {
                if (rootAnimator.avatar != null)
                {
                    rootAnimator.avatar = null;
                    changed = true;
                }

                if (rootAnimator.runtimeAnimatorController != null)
                {
                    rootAnimator.runtimeAnimatorController = null;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(rootAnimator);
                }
            }

            return changed;
        }

        private static void AddClipState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
        {
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
        }

        private static string[] GetCandidatePaths(CombatProxyVisualKind kind)
        {
            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    return EnemyMeleeVisualPrefabCandidatePaths;
                case CombatProxyVisualKind.EnemyMobile:
                    return EnemyMobileVisualPrefabCandidatePaths;
                case CombatProxyVisualKind.EnemyRanged:
                    return EnemyRangedVisualPrefabCandidatePaths;
                default:
                    return System.Array.Empty<string>();
            }
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

            NavMeshAgent[] navMeshAgents = visualRoot.GetComponentsInChildren<NavMeshAgent>(true);

            for (int i = 0; i < navMeshAgents.Length; i++)
            {
                Object.DestroyImmediate(navMeshAgents[i]);
            }
        }

        private static GameObject LoadFirstHumanoidPrefab(string[] candidatePaths)
        {
            string selectedPath = FindFirstCompatibleHumanoidPath(candidatePaths);
            return string.IsNullOrWhiteSpace(selectedPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
        }

        private static string FindFirstCompatibleHumanoidPath(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePaths[i]);

                if (prefab != null && IsCompatibleWithHumanoidAvatarPreview(prefab))
                {
                    return candidatePaths[i];
                }
            }

            return null;
        }

        private static bool IsCompatibleWithHumanoidAvatarPreview(GameObject visualPrefab)
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

        private static Avatar FindAvatar(GameObject visualRoot)
        {
            Animator animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>(true) : null;

            if (animator != null && animator.avatar != null && animator.avatar.isValid)
            {
                return animator.avatar;
            }

            return LoadFirstAvailableAvatar(EnemyAvatarCandidatePaths);
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

        private static AnimationClip LoadFirstAvailableAnimationClip(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(candidatePaths[i]);

                for (int j = 0; j < assets.Length; j++)
                {
                    AnimationClip clip = assets[j] as AnimationClip;

                    if (clip == null || string.Equals(clip.name, "__preview__", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return clip;
                }
            }

            return null;
        }

        private static bool SetProxyRenderersEnabled(Transform proxyRoot, bool enabled)
        {
            if (proxyRoot == null)
            {
                return false;
            }

            bool changed = false;
            Renderer[] renderers = proxyRoot.GetComponentsInChildren<Renderer>(true);

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
