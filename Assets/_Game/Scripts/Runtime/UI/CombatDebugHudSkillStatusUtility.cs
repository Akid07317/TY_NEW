using CampusRPG.Skills;

namespace CampusRPG.UI
{
    public static class CombatDebugHudSkillStatusUtility
    {
        public static string BuildSkillLine(string label, SkillController skillController, int slotIndex)
        {
            if (skillController == null)
            {
                return $"{label}: Missing Controller";
            }

            SkillSlotRuntimeStatus runtimeStatus = skillController.GetSlotRuntimeStatus(slotIndex, out SkillDefinitionSO skillDefinition, out SkillBeginCastBlockReason blockReason);
            string resolvedLabel = ResolveSkillLabel(label, skillDefinition);

            return runtimeStatus switch
            {
                SkillSlotRuntimeStatus.MissingSkill => $"{resolvedLabel}: Empty Slot",
                SkillSlotRuntimeStatus.Pending => $"{resolvedLabel}: {BuildPendingStatus(skillDefinition)}",
                SkillSlotRuntimeStatus.Cooldown => $"{resolvedLabel}: {BuildCooldownStatus(skillController, slotIndex)}",
                SkillSlotRuntimeStatus.Blocked => $"{resolvedLabel}: {BuildBlockedStatus(skillController, skillDefinition, blockReason)}",
                _ => $"{resolvedLabel}: {BuildReadyStatus(skillDefinition)}",
            };
        }

        private static string BuildReadyStatus(SkillDefinitionSO skillDefinition)
        {
            if (skillDefinition == null)
            {
                return "Ready";
            }

            return $"Ready ({skillDefinition.ManaCost:0} MP, {skillDefinition.CastDurationSeconds:0.0}s cast)";
        }

        private static string BuildPendingStatus(SkillDefinitionSO skillDefinition)
        {
            if (skillDefinition == null)
            {
                return "Pending";
            }

            return $"Pending {skillDefinition.CastDurationSeconds:0.0}s ({skillDefinition.ManaCost:0} MP)";
        }

        private static string BuildCooldownStatus(SkillController skillController, int slotIndex)
        {
            float remainingSeconds = skillController.GetRemainingCooldown(slotIndex);
            float progressNormalized = skillController.GetCooldownProgressNormalized(slotIndex);
            SkillDefinitionSO skillDefinition = skillController.GetSkill(slotIndex);
            float totalDuration = skillDefinition != null ? skillDefinition.CooldownSeconds : 0f;
            int progressPercent = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(progressNormalized * 100f), 0, 100);
            return $"Cooldown {remainingSeconds:0.0}s / {totalDuration:0.0}s ({progressPercent}%)";
        }

        private static string ResolveSkillLabel(string fallbackLabel, SkillDefinitionSO skillDefinition)
        {
            if (skillDefinition != null && !string.IsNullOrWhiteSpace(skillDefinition.DisplayName))
            {
                if (!string.IsNullOrWhiteSpace(fallbackLabel) && fallbackLabel.Trim().Length <= 2)
                {
                    return $"{fallbackLabel.Trim()} {skillDefinition.DisplayName}";
                }

                return skillDefinition.DisplayName;
            }

            return fallbackLabel ?? string.Empty;
        }

        private static string BuildBlockedStatus(
            SkillController skillController,
            SkillDefinitionSO skillDefinition,
            SkillBeginCastBlockReason blockReason)
        {
            if (blockReason == SkillBeginCastBlockReason.NotEnoughMana && skillDefinition != null)
            {
                return $"Need More Mana ({skillController.CurrentMana:0}/{skillDefinition.ManaCost:0})";
            }

            if (blockReason == SkillBeginCastBlockReason.OtherPendingCast)
            {
                SkillDefinitionSO pendingSkill = skillController.PendingSkill;
                string pendingSkillName = ResolveSkillLabel(null, pendingSkill);

                if (!string.IsNullOrWhiteSpace(pendingSkillName))
                {
                    return $"Blocked By Pending: {pendingSkillName} ({pendingSkill.CastDurationSeconds:0.0}s cast)";
                }
            }

            return DescribeBlockReason(blockReason);
        }

        private static string DescribeBlockReason(SkillBeginCastBlockReason blockReason)
        {
            return blockReason switch
            {
                SkillBeginCastBlockReason.MissingSkill => "Missing Skill",
                SkillBeginCastBlockReason.MissingManaComponent => "Missing Mana",
                SkillBeginCastBlockReason.Cooldown => "Cooling Down",
                SkillBeginCastBlockReason.NotEnoughMana => "Need More Mana",
                SkillBeginCastBlockReason.OtherPendingCast => "Blocked By Other Pending Cast",
                _ => "Blocked",
            };
        }
    }
}
