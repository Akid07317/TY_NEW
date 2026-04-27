using UnityEngine;

namespace CampusRPG.Combat
{
    public enum SwordArtTriggerAction
    {
        None = 0,
        LightAttack = 1,
        HeavyAttack = 2
    }

    public enum SwordArtInputDirection
    {
        Neutral = 0,
        Forward = 1,
        Backward = 2,
        Left = 3,
        Right = 4
    }

    [System.Flags]
    public enum SwordArtDirectionMask
    {
        None = 0,
        Neutral = 1 << 0,
        Forward = 1 << 1,
        Backward = 1 << 2,
        Left = 1 << 3,
        Right = 1 << 4,
        Any = Neutral | Forward | Backward | Left | Right
    }

    [System.Flags]
    public enum SwordArtContextTags
    {
        None = 0,
        AfterDodge = 1 << 0,
        Airborne = 1 << 1,
        ForwardInput = 1 << 2,
        AfterBlock = 1 << 3,
        AfterHeavy = 1 << 4,
        AfterCombatRoll = 1 << 5,
        AfterAirDodge = 1 << 6
    }

    [CreateAssetMenu(fileName = "SO_SwordArt", menuName = "CampusRPG/Combat/Sword Art Definition")]
    public sealed class SwordArtDefinitionSO : ScriptableObject
    {
        [SerializeField] private string artId = "SwordArt_Id";
        [SerializeField] private string displayName = "Sword Art";
        [SerializeField] private AttackDefinitionSO attackDefinition;
        [SerializeField] private SwordArtTriggerAction triggerAction = SwordArtTriggerAction.LightAttack;
        [SerializeField] private SwordArtDirectionMask acceptedDirections = SwordArtDirectionMask.Any;
        [SerializeField] private SwordArtContextTags requiredContextTags;
        [SerializeField] private SwordArtContextTags anyContextTags;
        [SerializeField] private float triggerWindowSeconds = 0.25f;
        [SerializeField] private float cancelWindowSeconds = 0.2f;
        [SerializeField] private float resourceCost;

        public string ArtId => artId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? artId : displayName;

        public AttackDefinitionSO AttackDefinition => attackDefinition;

        public SwordArtTriggerAction TriggerAction => triggerAction;

        public SwordArtDirectionMask AcceptedDirections => acceptedDirections != SwordArtDirectionMask.None
            ? acceptedDirections
            : SwordArtDirectionMask.Any;

        public SwordArtContextTags RequiredContextTags => requiredContextTags;

        public SwordArtContextTags AnyContextTags => anyContextTags;

        public float TriggerWindowSeconds => Mathf.Max(0f, triggerWindowSeconds);

        public float CancelWindowSeconds => Mathf.Max(0f, cancelWindowSeconds);

        public float ResourceCost => Mathf.Max(0f, resourceCost);
    }
}
