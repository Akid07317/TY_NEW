using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.UI
{
    public sealed class CombatDebugHUD : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField] private LockOnTargetSelector lockOnTargetSelector;
        [SerializeField] private bool showControlHelp = true;

        private GUIStyle labelStyle;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnGUI()
        {
            ResolveReferences();

            if (playerCharacter == null)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    richText = true
                };
            }

            float y = 16f;
            DrawLine(ref y, $"<b>State</b>: {playerCharacter.StateMachine?.CurrentState?.GetType().Name ?? "None"}");
            DrawLine(ref y, $"HP: {playerCharacter.Health?.CurrentValue:0}/{playerCharacter.Health?.MaxValue:0}");
            DrawLine(ref y, $"MP: {playerCharacter.Mana?.CurrentValue:0}/{playerCharacter.Mana?.MaxValue:0}");
            DrawLine(ref y, $"Counter: {playerCharacter.Gauges?.CounterGauge:0}");
            DrawLine(ref y, $"Agility: {playerCharacter.Gauges?.AgilityGauge:0}");

            if (playerCharacter.SkillController != null)
            {
                DrawLine(ref y, $"Skill1 CD: {playerCharacter.SkillController.GetRemainingCooldown(0):0.0}s");
                DrawLine(ref y, $"Skill2 CD: {playerCharacter.SkillController.GetRemainingCooldown(1):0.0}s");
            }

            Transform target = lockOnTargetSelector != null ? lockOnTargetSelector.CurrentTarget : null;
            DrawLine(ref y, $"Lock Target: {(target != null ? target.name : "None")}");

            if (target != null)
            {
                HealthComponent targetHealth = target.GetComponentInParent<HealthComponent>();

                if (targetHealth != null)
                {
                    DrawLine(ref y, $"Target HP: {targetHealth.CurrentValue:0}/{targetHealth.MaxValue:0}");
                }
            }

            if (!showControlHelp)
            {
                return;
            }

            y += 12f;
            DrawLine(ref y, "LMB: Light  RMB: Heavy  Shift: Dodge  Ctrl: Block");
            DrawLine(ref y, "Q/E: Skills  Tab: LockOn  Space: Jump");
        }

        private void DrawLine(ref float y, string text)
        {
            GUI.Label(new Rect(16f, y, 420f, 22f), text, labelStyle);
            y += 20f;
        }

        private void ResolveReferences()
        {
            playerCharacter = SceneRuntimeReferenceUtility.ResolvePlayerCharacter(playerCharacter);

            if (playerCharacter == null)
            {
                return;
            }

            lockOnTargetSelector = SceneRuntimeReferenceUtility.ResolveLockOnTargetSelector(lockOnTargetSelector, playerCharacter);
        }
    }
}
