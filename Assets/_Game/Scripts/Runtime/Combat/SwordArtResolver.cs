using System.Collections.Generic;
using UnityEngine;

namespace CampusRPG.Combat
{
    public readonly struct SwordArtCommand
    {
        public SwordArtCommand(
            SwordArtTriggerAction triggerAction,
            SwordArtInputDirection direction,
            SwordArtContextTags contextTags = SwordArtContextTags.None,
            float ageSeconds = 0f)
        {
            TriggerAction = triggerAction;
            Direction = direction;
            ContextTags = contextTags;
            AgeSeconds = Mathf.Max(0f, ageSeconds);
        }

        public SwordArtTriggerAction TriggerAction { get; }

        public SwordArtInputDirection Direction { get; }

        public SwordArtContextTags ContextTags { get; }

        public float AgeSeconds { get; }

        public SwordArtCommand WithAge(float ageSeconds)
        {
            return new SwordArtCommand(TriggerAction, Direction, ContextTags, ageSeconds);
        }
    }

    public sealed class SwordArtCommandBuffer
    {
        private SwordArtCommand currentCommand;

        public bool HasCommand { get; private set; }

        public SwordArtCommand CurrentCommand => currentCommand;

        public void Buffer(
            SwordArtTriggerAction triggerAction,
            SwordArtInputDirection direction,
            SwordArtContextTags contextTags = SwordArtContextTags.None)
        {
            if (triggerAction == SwordArtTriggerAction.None)
            {
                Clear();
                return;
            }

            currentCommand = new SwordArtCommand(triggerAction, direction, contextTags);
            HasCommand = true;
        }

        public void Tick(float deltaTime)
        {
            if (!HasCommand)
            {
                return;
            }

            currentCommand = currentCommand.WithAge(currentCommand.AgeSeconds + Mathf.Max(0f, deltaTime));
        }

        public bool TryResolve(
            IReadOnlyList<SwordArtDefinitionSO> definitions,
            out SwordArtDefinitionSO definition,
            bool consumeOnSuccess = true)
        {
            if (!HasCommand)
            {
                definition = null;
                return false;
            }

            bool resolved = SwordArtResolver.TryResolve(definitions, currentCommand, out definition);

            if (resolved && consumeOnSuccess)
            {
                Clear();
            }

            return resolved;
        }

        public void Clear()
        {
            currentCommand = default;
            HasCommand = false;
        }
    }

    public static class SwordArtResolver
    {
        public static bool TryResolve(
            IReadOnlyList<SwordArtDefinitionSO> definitions,
            SwordArtCommand command,
            out SwordArtDefinitionSO definition)
        {
            definition = null;

            if (definitions == null || command.TriggerAction == SwordArtTriggerAction.None)
            {
                return false;
            }

            int bestScore = int.MinValue;

            for (int i = 0; i < definitions.Count; i++)
            {
                SwordArtDefinitionSO candidate = definitions[i];

                if (!Matches(candidate, command))
                {
                    continue;
                }

                int score = CalculateSpecificityScore(candidate);

                if (definition != null && score <= bestScore)
                {
                    continue;
                }

                definition = candidate;
                bestScore = score;
            }

            return definition != null;
        }

        public static bool Matches(SwordArtDefinitionSO definition, SwordArtCommand command)
        {
            if (definition == null || definition.TriggerAction != command.TriggerAction)
            {
                return false;
            }

            if (command.AgeSeconds > definition.TriggerWindowSeconds)
            {
                return false;
            }

            if ((definition.AcceptedDirections & ToDirectionMask(command.Direction)) == 0)
            {
                return false;
            }

            SwordArtContextTags contextTags = ResolveContextTags(command);
            SwordArtContextTags requiredTags = definition.RequiredContextTags;

            if (requiredTags != SwordArtContextTags.None && (contextTags & requiredTags) != requiredTags)
            {
                return false;
            }

            SwordArtContextTags anyTags = definition.AnyContextTags;
            return anyTags == SwordArtContextTags.None || (contextTags & anyTags) != 0;
        }

        public static SwordArtDirectionMask ToDirectionMask(SwordArtInputDirection direction)
        {
            switch (direction)
            {
                case SwordArtInputDirection.Forward:
                    return SwordArtDirectionMask.Forward;
                case SwordArtInputDirection.Backward:
                    return SwordArtDirectionMask.Backward;
                case SwordArtInputDirection.Left:
                    return SwordArtDirectionMask.Left;
                case SwordArtInputDirection.Right:
                    return SwordArtDirectionMask.Right;
                default:
                    return SwordArtDirectionMask.Neutral;
            }
        }

        private static SwordArtContextTags ResolveContextTags(SwordArtCommand command)
        {
            SwordArtContextTags contextTags = command.ContextTags;

            if (command.Direction == SwordArtInputDirection.Forward)
            {
                contextTags |= SwordArtContextTags.ForwardInput;
            }

            return contextTags;
        }

        private static int CalculateSpecificityScore(SwordArtDefinitionSO definition)
        {
            int score = CountFlags(definition.RequiredContextTags) * 10;
            score += CountFlags(definition.AnyContextTags) * 4;

            if (definition.AcceptedDirections != SwordArtDirectionMask.Any)
            {
                score += 2;
            }

            return score;
        }

        private static int CountFlags<TEnum>(TEnum value) where TEnum : System.Enum
        {
            int rawValue = System.Convert.ToInt32(value);
            int count = 0;

            while (rawValue != 0)
            {
                count += rawValue & 1;
                rawValue >>= 1;
            }

            return count;
        }
    }
}
