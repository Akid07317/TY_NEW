using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusRPG.Input
{
    public sealed class InputReader : MonoBehaviour
    {
        private const string PlayerMapName = "Player";

        [Header("Action Asset")]
        [SerializeField] private InputActionAsset actionsAsset;

        [Header("Player Actions")]
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string lightAttackActionName = "LightAttack";
        [SerializeField] private string heavyAttackActionName = "HeavyAttack";
        [SerializeField] private string blockActionName = "Block";
        [SerializeField] private string dodgeActionName = "Dodge";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string skill1ActionName = "Skill1";
        [SerializeField] private string skill2ActionName = "Skill2";
        [SerializeField] private string lockOnActionName = "LockOn";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string pauseActionName = "Pause";

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction lightAttackAction;
        private InputAction heavyAttackAction;
        private InputAction blockAction;
        private InputAction dodgeAction;
        private InputAction jumpAction;
        private InputAction skill1Action;
        private InputAction skill2Action;
        private InputAction lockOnAction;
        private InputAction interactAction;
        private InputAction pauseAction;
        private InputActionAsset runtimeActionsAsset;
        private bool isInitialized;
        private bool callbacksRegistered;

        public event Action LightAttackPressed;
        public event Action HeavyAttackPressed;
        public event Action DodgePressed;
        public event Action JumpPressed;
        public event Action Skill1Pressed;
        public event Action Skill2Pressed;
        public event Action LockOnPressed;
        public event Action InteractPressed;
        public event Action PausePressed;

        public Vector2 MoveValue => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        public Vector2 LookValue => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        public bool IsBlockHeld => blockAction != null && blockAction.IsPressed();

        public void Initialize()
        {
            if (isInitialized || actionsAsset == null)
            {
                return;
            }

            runtimeActionsAsset = Instantiate(actionsAsset);
            runtimeActionsAsset.name = actionsAsset.name + " (Runtime)";

            moveAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + moveActionName, false);
            lookAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + lookActionName, false);
            lightAttackAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + lightAttackActionName, false);
            heavyAttackAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + heavyAttackActionName, false);
            blockAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + blockActionName, false);
            dodgeAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + dodgeActionName, false);
            jumpAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + jumpActionName, false);
            skill1Action = runtimeActionsAsset.FindAction(PlayerMapName + "/" + skill1ActionName, false);
            skill2Action = runtimeActionsAsset.FindAction(PlayerMapName + "/" + skill2ActionName, false);
            lockOnAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + lockOnActionName, false);
            interactAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + interactActionName, false);
            pauseAction = runtimeActionsAsset.FindAction(PlayerMapName + "/" + pauseActionName, false);

            isInitialized = true;
        }

        private void OnEnable()
        {
            Initialize();

            RegisterCallbacks();

            if (runtimeActionsAsset != null)
            {
                runtimeActionsAsset.Enable();
            }
        }

        private void OnDisable()
        {
            UnregisterCallbacks();

            if (runtimeActionsAsset != null)
            {
                runtimeActionsAsset.Disable();
            }
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();

            if (runtimeActionsAsset != null)
            {
                Destroy(runtimeActionsAsset);
                runtimeActionsAsset = null;
            }
        }

        private void RegisterCallbacks()
        {
            if (callbacksRegistered)
            {
                return;
            }

            RegisterButton(lightAttackAction, OnLightAttackPerformed);
            RegisterButton(heavyAttackAction, OnHeavyAttackPerformed);
            RegisterButton(dodgeAction, OnDodgePerformed);
            RegisterButton(jumpAction, OnJumpPerformed);
            RegisterButton(skill1Action, OnSkill1Performed);
            RegisterButton(skill2Action, OnSkill2Performed);
            RegisterButton(lockOnAction, OnLockOnPerformed);
            RegisterButton(interactAction, OnInteractPerformed);
            RegisterButton(pauseAction, OnPausePerformed);
            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (!callbacksRegistered)
            {
                return;
            }

            UnregisterButton(lightAttackAction, OnLightAttackPerformed);
            UnregisterButton(heavyAttackAction, OnHeavyAttackPerformed);
            UnregisterButton(dodgeAction, OnDodgePerformed);
            UnregisterButton(jumpAction, OnJumpPerformed);
            UnregisterButton(skill1Action, OnSkill1Performed);
            UnregisterButton(skill2Action, OnSkill2Performed);
            UnregisterButton(lockOnAction, OnLockOnPerformed);
            UnregisterButton(interactAction, OnInteractPerformed);
            UnregisterButton(pauseAction, OnPausePerformed);
            callbacksRegistered = false;
        }

        private void OnLightAttackPerformed(InputAction.CallbackContext context)
        {
            LightAttackPressed?.Invoke();
        }

        private void OnHeavyAttackPerformed(InputAction.CallbackContext context)
        {
            HeavyAttackPressed?.Invoke();
        }

        private void OnDodgePerformed(InputAction.CallbackContext context)
        {
            DodgePressed?.Invoke();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            JumpPressed?.Invoke();
        }

        private void OnSkill1Performed(InputAction.CallbackContext context)
        {
            Skill1Pressed?.Invoke();
        }

        private void OnSkill2Performed(InputAction.CallbackContext context)
        {
            Skill2Pressed?.Invoke();
        }

        private void OnLockOnPerformed(InputAction.CallbackContext context)
        {
            LockOnPressed?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            InteractPressed?.Invoke();
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            PausePressed?.Invoke();
        }

        private static void RegisterButton(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                return;
            }

            action.performed += callback;
        }

        private static void UnregisterButton(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                return;
            }

            action.performed -= callback;
        }
    }
}
