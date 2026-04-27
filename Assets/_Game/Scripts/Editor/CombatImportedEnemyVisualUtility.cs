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
        public const string ImportedRoleMarkerRootName = "ImportedEnemyRoleMarkerRoot";

        private const string ProxyRootName = "CombatProxyVisualRoot";
        private const string MaterialsFolder = "Assets/_Game/Materials";
        private const string LocalPreviewAnimationFolder = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview";
        private const string ImportedAnimatorControllerPathPrefix = LocalPreviewAnimationFolder + "/AC_Enemy_ImportedPreview_";
        private const string ImportedUpperBodyMaskPath = LocalPreviewAnimationFolder + "/AM_Enemy_ImportedUpperBody.mask";
        private const float ImportedWalkThreshold = 0.18f;
        private const float ImportedRunThreshold = 0.7f;
        private const float AntiAirResponseStateSpeed = 1.12f;
        private const float ChaseRollResponseStateSpeed = 0.88f;
        private const float GuardBreakResponseStateSpeed = 0.78f;
        private const float ImportedGroundingInset = 0.035f;
        private const string CombatPoseLayerName = "CombatPose";
        private const string CombatPoseStateName = "Hold";

        private static readonly string[] EnemyMeleeVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyMobileVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Polearm.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Dual Wield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyRangedVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/Prefabs/Human_BasicMotionsDummy_M.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanF_Dummy_Red.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyAvatarCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx",
            "Assets/Kevin Iglesias/Human Animations/Models/HumanF_Model.fbx"
        };

        private static readonly string[] DefaultIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] OneHandedIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@CombatIdle1H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] PolearmIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] TwoHandedIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] RangedIdleClipCandidatePaths =
        {
            "Assets/DoubleL/Bow/Movement/Idle/Idle/Bow_Idle_B.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] WalkClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_B_InPlace.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx"
        };

        private static readonly string[] OneHandedWalkClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_B_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/1Hand_Up_Walk_A_F.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/1Hand_Up_Walk_A_B.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx"
        };

        private static readonly string[] RunClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_B_InPlace.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx"
        };

        private static readonly string[] OneHandedRunClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_B_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/1Hand_Up_Run_A_F.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/1Hand_Up_Run_A_B.fbx",
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

        private static readonly string[] OneHandedHoldClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/Human@ObjectGripHands01.fbx"
        };

        private static readonly string[] PolearmHoldClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanM@WeaponHoldPolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanF@WeaponHoldPolearm01.fbx"
        };

        private static readonly string[] TwoHandedHoldClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanM@WeaponHold2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanF@WeaponHold2H01.fbx"
        };

        private enum ImportedEnemyAnimationProfile
        {
            Default,
            OneHanded,
            Polearm,
            TwoHanded,
            Ranged
        }

        public static bool HasHumanoidVisualSource(CombatProxyVisualKind kind)
        {
            return LoadFirstHumanoidPrefab(GetCandidatePaths(kind)) != null;
        }

        public static string GetSelectedHumanoidVisualPrefabPath(CombatProxyVisualKind kind)
        {
            return FindFirstCompatibleHumanoidPath(GetCandidatePaths(kind));
        }

        public static RuntimeAnimatorController EnsureImportedAvatarPreviewController(CombatProxyVisualKind kind)
        {
            ImportedEnemyAnimationProfile profile = ResolveAnimationProfile(kind);
            AnimationClip idleClip = LoadFirstAvailableAnimationClip(GetIdleClipCandidatePaths(profile));
            AnimationClip walkClip = LoadFirstAvailableAnimationClip(GetWalkClipCandidatePaths(profile));
            AnimationClip runClip = LoadFirstAvailableAnimationClip(GetRunClipCandidatePaths(profile));
            AnimationClip hitClip = LoadFirstAvailableAnimationClip(HitClipCandidatePaths);
            AnimationClip deathClip = LoadFirstAvailableAnimationClip(DeathClipCandidatePaths);
            AnimationClip meleeAttackClip = LoadFirstAvailableAnimationClip(MeleeAttackClipCandidatePaths);
            AnimationClip mobileAttackClip = LoadFirstAvailableAnimationClip(MobileAttackClipCandidatePaths);
            AnimationClip rangedAttackClip = LoadFirstAvailableAnimationClip(RangedAttackClipCandidatePaths);
            AnimationClip holdClip = LoadFirstAvailableAnimationClip(GetHoldClipCandidatePaths(profile));

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
            string controllerPath = GetImportedAnimatorControllerPath(kind);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.GroundSpeedParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.ResponseReadParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.AntiAirReadParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName, AnimatorControllerParameterType.Float);

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
            locomotionBlendTree.AddChild(walkClip, ImportedWalkThreshold);
            locomotionBlendTree.AddChild(runClip, ImportedRunThreshold);

            AnimatorState locomotionState = stateMachine.AddState(EnemyCombatAnimationPlanUtility.LocomotionStateName);
            locomotionState.motion = locomotionBlendTree;
            stateMachine.defaultState = locomotionState;

            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.HitStateName, hitClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.DeathStateName, deathClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.MeleeAttackStateName, meleeAttackClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.MobileAttackStateName, mobileAttackClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.RangedAttackStateName, rangedAttackClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.AntiAirAttackStateName, rangedAttackClip, AntiAirResponseStateSpeed);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName, mobileAttackClip, ChaseRollResponseStateSpeed);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName, meleeAttackClip, GuardBreakResponseStateSpeed);

            if (holdClip != null)
            {
                AddCombatPoseLayer(controller, holdClip);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return controller;
        }

        public static bool TryApplyHumanoidAvatarPreview(GameObject actor, CombatProxyVisualKind kind, Animator rootAnimator)
        {
            if (actor == null)
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

            Animator visualAnimator = FindAvatarAnimator(visualInstance);
            Avatar avatar = visualAnimator != null ? visualAnimator.avatar : LoadFirstAvailableAvatar(EnemyAvatarCandidatePaths);

            if (avatar == null || !avatar.isValid)
            {
                Object.DestroyImmediate(visualInstance);
                return changed;
            }

            if (visualAnimator == null)
            {
                visualAnimator = visualInstance.AddComponent<Animator>();
            }

            StripImportedVisualComponents(visualInstance, visualAnimator);
            changed |= AlignImportedVisualToGround(visualInstance, actor.transform);
            changed |= SetProxyRenderersEnabled(proxyRoot, false);

            visualAnimator.enabled = true;
            visualAnimator.avatar = avatar;
            visualAnimator.applyRootMotion = false;
            visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visualAnimator.updateMode = AnimatorUpdateMode.Normal;
            changed |= AddImportedRoleMarkers(visualInstance.transform, kind);
            EditorUtility.SetDirty(visualAnimator);

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

                rootAnimator.enabled = false;

                EditorUtility.SetDirty(rootAnimator);
            }

            EditorUtility.SetDirty(visualInstance.transform);
            EditorUtility.SetDirty(visualInstance);
            return true;
        }

        public static Animator FindImportedPreviewAnimator(GameObject actor)
        {
            if (actor == null)
            {
                return null;
            }

            Transform importedVisualRoot = actor.transform.Find(ImportedVisualRootName);
            return importedVisualRoot != null
                ? importedVisualRoot.GetComponentInChildren<Animator>(true)
                : null;
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
                if (!rootAnimator.enabled)
                {
                    rootAnimator.enabled = true;
                    changed = true;
                }

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

        private static bool AddImportedRoleMarkers(Transform visualRoot, CombatProxyVisualKind kind)
        {
            if (visualRoot == null)
            {
                return false;
            }

            bool changed = false;
            Transform previousMarkerRoot = visualRoot.Find(ImportedRoleMarkerRootName);

            if (previousMarkerRoot != null)
            {
                Object.DestroyImmediate(previousMarkerRoot.gameObject);
                changed = true;
            }

            Material primaryMaterial = LoadRoleMarkerMaterial(kind, false);
            Material accentMaterial = LoadRoleMarkerMaterial(kind, true);
            Transform markerRoot = CreateTransformChild(
                visualRoot,
                ImportedRoleMarkerRootName,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one);

            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    CreatePrimitive(markerRoot, "MeleeShoulderLeft", PrimitiveType.Cube, new Vector3(-0.34f, 1.32f, 0.02f), Quaternion.identity, new Vector3(0.22f, 0.16f, 0.24f), primaryMaterial);
                    CreatePrimitive(markerRoot, "MeleeShoulderRight", PrimitiveType.Cube, new Vector3(0.34f, 1.32f, 0.02f), Quaternion.identity, new Vector3(0.22f, 0.16f, 0.24f), primaryMaterial);
                    CreatePrimitive(markerRoot, "MeleeBlade", PrimitiveType.Cube, new Vector3(0.38f, 1.1f, 0.46f), Quaternion.Euler(0f, -8f, 76f), new Vector3(0.08f, 0.08f, 0.86f), accentMaterial);
                    break;
                case CombatProxyVisualKind.EnemyMobile:
                    CreatePrimitive(markerRoot, "MobileFinLeft", PrimitiveType.Cube, new Vector3(-0.4f, 1f, 0.04f), Quaternion.Euler(0f, 0f, -32f), new Vector3(0.12f, 0.42f, 0.08f), accentMaterial);
                    CreatePrimitive(markerRoot, "MobileFinRight", PrimitiveType.Cube, new Vector3(0.4f, 1f, 0.04f), Quaternion.Euler(0f, 0f, 32f), new Vector3(0.12f, 0.42f, 0.08f), accentMaterial);
                    CreatePrimitive(markerRoot, "MobileTail", PrimitiveType.Cube, new Vector3(0f, 0.78f, -0.24f), Quaternion.identity, new Vector3(0.12f, 0.2f, 0.54f), primaryMaterial);
                    break;
                case CombatProxyVisualKind.EnemyRanged:
                    CreatePrimitive(markerRoot, "FocusOrb", PrimitiveType.Sphere, new Vector3(0f, 1.12f, 0.5f), Quaternion.identity, new Vector3(0.2f, 0.2f, 0.2f), accentMaterial);
                    CreatePrimitive(markerRoot, "Staff", PrimitiveType.Cylinder, new Vector3(0.36f, 1.02f, 0.16f), Quaternion.Euler(0f, 0f, 10f), new Vector3(0.05f, 0.62f, 0.05f), primaryMaterial);
                    CreatePrimitive(markerRoot, "CasterPack", PrimitiveType.Cube, new Vector3(0f, 1.12f, -0.18f), Quaternion.identity, new Vector3(0.3f, 0.28f, 0.2f), accentMaterial);
                    break;
                default:
                    Object.DestroyImmediate(markerRoot.gameObject);
                    return changed;
            }

            EditorUtility.SetDirty(markerRoot.gameObject);
            return true;
        }

        private static Material LoadRoleMarkerMaterial(CombatProxyVisualKind kind, bool accent)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(GetRoleMarkerMaterialPath(kind, accent));
        }

        private static string GetRoleMarkerMaterialPath(CombatProxyVisualKind kind, bool accent)
        {
            string suffix = accent ? "Accent" : "Primary";

            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMelee" + suffix + ".mat";
                case CombatProxyVisualKind.EnemyMobile:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMobile" + suffix + ".mat";
                case CombatProxyVisualKind.EnemyRanged:
                    return MaterialsFolder + "/M_CombatProxy_EnemyRanged" + suffix + ".mat";
                default:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMelee" + suffix + ".mat";
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

        private static Transform CreateTransformChild(
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = localRotation;
            child.transform.localScale = localScale;
            return child.transform;
        }

        private static void AddClipState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip, float speed = 1f)
        {
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = Mathf.Max(0.01f, speed);
        }

        private static void AddCombatPoseLayer(AnimatorController controller, AnimationClip holdClip)
        {
            if (controller == null || holdClip == null)
            {
                return;
            }

            AvatarMask avatarMask = EnsureUpperBodyAvatarMask();

            if (avatarMask == null)
            {
                return;
            }

            AnimatorStateMachine stateMachine = new AnimatorStateMachine
            {
                name = CombatPoseLayerName
            };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);

            AnimatorState holdState = stateMachine.AddState(CombatPoseStateName);
            holdState.motion = holdClip;
            stateMachine.defaultState = holdState;

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = CombatPoseLayerName,
                avatarMask = avatarMask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 0f,
                iKPass = false,
                stateMachine = stateMachine,
                syncedLayerAffectsTiming = false
            };

            controller.AddLayer(layer);
        }

        private static AvatarMask EnsureUpperBodyAvatarMask()
        {
            AvatarMask avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(ImportedUpperBodyMaskPath);

            if (avatarMask != null)
            {
                return avatarMask;
            }

            avatarMask = new AvatarMask();

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                avatarMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            }

            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

            AssetDatabase.CreateAsset(avatarMask, ImportedUpperBodyMaskPath);
            return avatarMask;
        }

        private static string GetImportedAnimatorControllerPath(CombatProxyVisualKind kind)
        {
            return ImportedAnimatorControllerPathPrefix + kind + ".controller";
        }

        private static ImportedEnemyAnimationProfile ResolveAnimationProfile(CombatProxyVisualKind kind)
        {
            string selectedVisualPrefabPath = GetSelectedHumanoidVisualPrefabPath(kind);

            if (kind == CombatProxyVisualKind.EnemyMelee)
            {
                return ImportedEnemyAnimationProfile.OneHanded;
            }

            if (kind == CombatProxyVisualKind.EnemyMobile)
            {
                if (!string.IsNullOrEmpty(selectedVisualPrefabPath)
                    && selectedVisualPrefabPath.IndexOf("Polearm", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ImportedEnemyAnimationProfile.Polearm;
                }

                return ImportedEnemyAnimationProfile.TwoHanded;
            }

            if (kind == CombatProxyVisualKind.EnemyRanged)
            {
                return ImportedEnemyAnimationProfile.Ranged;
            }

            return ImportedEnemyAnimationProfile.Default;
        }

        private static string[] GetIdleClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                    return OneHandedIdleClipCandidatePaths;
                case ImportedEnemyAnimationProfile.Polearm:
                    return PolearmIdleClipCandidatePaths;
                case ImportedEnemyAnimationProfile.TwoHanded:
                    return TwoHandedIdleClipCandidatePaths;
                case ImportedEnemyAnimationProfile.Ranged:
                    return RangedIdleClipCandidatePaths;
                default:
                    return DefaultIdleClipCandidatePaths;
            }
        }

        private static string[] GetHoldClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                    return OneHandedHoldClipCandidatePaths;
                case ImportedEnemyAnimationProfile.Polearm:
                    return PolearmHoldClipCandidatePaths;
                case ImportedEnemyAnimationProfile.TwoHanded:
                    return TwoHandedHoldClipCandidatePaths;
                default:
                    return System.Array.Empty<string>();
            }
        }

        private static string[] GetWalkClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                    return OneHandedWalkClipCandidatePaths;
                default:
                    return WalkClipCandidatePaths;
            }
        }

        private static string[] GetRunClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                    return OneHandedRunClipCandidatePaths;
                default:
                    return RunClipCandidatePaths;
            }
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

        private static void StripImportedVisualComponents(GameObject visualRoot, Animator preservedAnimator)
        {
            if (visualRoot == null)
            {
                return;
            }

            Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] == preservedAnimator)
                {
                    continue;
                }

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
            Animator animator = FindAvatarAnimator(visualRoot);

            if (animator != null && animator.avatar != null && animator.avatar.isValid)
            {
                return animator.avatar;
            }

            return LoadFirstAvailableAvatar(EnemyAvatarCandidatePaths);
        }

        private static Animator FindAvatarAnimator(GameObject visualRoot)
        {
            if (visualRoot == null)
            {
                return null;
            }

            Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];

                if (animator != null && animator.avatar != null && animator.avatar.isValid)
                {
                    return animator;
                }
            }

            return animators.Length > 0 ? animators[0] : null;
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

            float desiredLift = actorRoot.position.y - minY - ImportedGroundingInset;

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
