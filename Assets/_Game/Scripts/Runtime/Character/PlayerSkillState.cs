using CampusRPG.Skills;

namespace CampusRPG.Character
{
    public sealed class PlayerSkillState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly int skillSlotIndex;

        private SkillDefinitionSO skillDefinition;
        private float remainingCastTime;
        private bool hasCommitted;

        public PlayerSkillState(PlayerCharacter owner, PlayerStateMachine stateMachine, int skillSlotIndex) : base(owner)
        {
            this.stateMachine = stateMachine;
            this.skillSlotIndex = skillSlotIndex;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public override void Enter()
        {
            if (Owner.SkillController == null || !Owner.SkillController.TryBeginCast(skillSlotIndex, out skillDefinition))
            {
                stateMachine.SwitchToLocomotion();
                return;
            }

            remainingCastTime = skillDefinition.CastDurationSeconds;

            if (remainingCastTime <= 0f)
            {
                CommitAndReturn();
            }
        }

        public override void Tick(float deltaTime)
        {
            remainingCastTime -= deltaTime;

            if (remainingCastTime > 0f)
            {
                return;
            }

            CommitAndReturn();
        }

        private void CommitAndReturn()
        {
            if (!hasCommitted)
            {
                Owner.SkillController?.CommitCast(skillDefinition);
                hasCommitted = true;
            }

            stateMachine.SwitchToLocomotion();
        }
    }
}
