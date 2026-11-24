using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeoVeil.Core
{
    /// <summary>
    /// Modular Input wrapper based on Unity's New Input System.
    /// Works out-of-the-box with built-in ZZZ-style PC + Gamepad bindings,
    /// and optionally supports assigning a custom InputActionAsset in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "InputReader", menuName = "NeoVeil/Core/Input Reader")]
    public class InputReader : ScriptableObject
    {
        [Header("Optional Custom Input Asset")]
        [Tooltip("Leave empty to use built-in default bindings (WASD, Mouse, LMB, RMB/Shift, E, Q, Space/Tab, F).")]
        [SerializeField] private InputActionAsset customInputAsset;
        [SerializeField] private string gameplayActionMapName = "Gameplay";

        private InputActionMap _gameplayMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _dodgeAction;
        private InputAction _sprintAction;
        private InputAction _attackAction;
        private InputAction _specialAction;
        private InputAction _ultimateAction;
        private InputAction _switchNextAction;
        private InputAction _switchSlot1Action;
        private InputAction _switchSlot2Action;
        private InputAction _switchSlot3Action;
        private InputAction _interactAction;

        public Vector2 MoveInput => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 LookInput => _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool IsSprintHeld => _sprintAction != null && _sprintAction.IsPressed();
        public bool IsDodgeHeld => _dodgeAction != null && _dodgeAction.IsPressed();

        public event Action OnJumpPressed;
        public event Action OnDodgePressed;
        public event Action OnAttackPressed;
        public event Action OnSpecialPressed;
        public event Action OnUltimatePressed;
        public event Action OnSwitchNextPressed;
        public event Action<int> OnSwitchSlotPressed;
        public event Action OnInteractPressed;

        private void OnEnable()
        {
            InitializeActions();
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            DisableGameplayInput();
            UnbindCallbacks();
        }

        public void EnableGameplayInput()
        {
            _gameplayMap?.Enable();
        }

        public void DisableGameplayInput()
        {
            _gameplayMap?.Disable();
        }

        private void InitializeActions()
        {
            UnbindCallbacks();

            if (customInputAsset != null)
            {
                _gameplayMap = customInputAsset.FindActionMap(gameplayActionMapName, throwIfNotFound: false);
            }

            if (_gameplayMap == null)
            {
                _gameplayMap = CreateDefaultGameplayMap();
            }

            _moveAction = _gameplayMap.FindAction("Move");
            _lookAction = _gameplayMap.FindAction("Look");
            _jumpAction = _gameplayMap.FindAction("Jump");
            _dodgeAction = _gameplayMap.FindAction("Dodge");
            _sprintAction = _gameplayMap.FindAction("Sprint");
            _attackAction = _gameplayMap.FindAction("Attack");
            _specialAction = _gameplayMap.FindAction("Special");
            _ultimateAction = _gameplayMap.FindAction("Ultimate");
            _switchNextAction = _gameplayMap.FindAction("SwitchNext");
            _switchSlot1Action = _gameplayMap.FindAction("SwitchSlot1");
            _switchSlot2Action = _gameplayMap.FindAction("SwitchSlot2");
            _switchSlot3Action = _gameplayMap.FindAction("SwitchSlot3");
            _interactAction = _gameplayMap.FindAction("Interact");

            BindCallbacks();
        }

        private static InputActionMap CreateDefaultGameplayMap()
        {
            var map = new InputActionMap("Gameplay");

            // Move (WASD + Gamepad Left Stick)
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            // Look (Mouse Delta + Gamepad Right Stick)
            var look = map.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick");

            // Jump (Spacebar for urban exploration / Gamepad South)
            var jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            // Dodge (Right Click or Left Shift — classic ZZZ PC controls)
            var dodge = map.AddAction("Dodge", InputActionType.Button);
            dodge.AddBinding("<Mouse>/rightButton");
            dodge.AddBinding("<Keyboard>/leftShift");
            dodge.AddBinding("<Gamepad>/rightShoulder");

            // Sprint (Hold Left Shift or Right Click after Dodge)
            var sprint = map.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Mouse>/rightButton");
            sprint.AddBinding("<Gamepad>/rightShoulder");

            // Attack (Left Click / Gamepad West)
            var attack = map.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/buttonWest");

            // EX Special (E / Gamepad North)
            var special = map.AddAction("Special", InputActionType.Button);
            special.AddBinding("<Keyboard>/e");
            special.AddBinding("<Gamepad>/buttonNorth");

            // Ultimate (Q / Gamepad Right Trigger)
            var ultimate = map.AddAction("Ultimate", InputActionType.Button);
            ultimate.AddBinding("<Keyboard>/q");
            ultimate.AddBinding("<Gamepad>/rightTrigger");

            // Squad Switch & Defensive Assist Parry (Tab or C / Gamepad Left Shoulder)
            var switchNext = map.AddAction("SwitchNext", InputActionType.Button);
            switchNext.AddBinding("<Keyboard>/tab");
            switchNext.AddBinding("<Keyboard>/c");
            switchNext.AddBinding("<Gamepad>/leftShoulder");

            // Direct Squad Slot Switch (1, 2, 3)
            var slot1 = map.AddAction("SwitchSlot1", InputActionType.Button);
            slot1.AddBinding("<Keyboard>/1");
            var slot2 = map.AddAction("SwitchSlot2", InputActionType.Button);
            slot2.AddBinding("<Keyboard>/2");
            var slot3 = map.AddAction("SwitchSlot3", InputActionType.Button);
            slot3.AddBinding("<Keyboard>/3");

            // Interact (F / Gamepad East)
            var interact = map.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/f");
            interact.AddBinding("<Gamepad>/buttonEast");

            return map;
        }

        private void BindCallbacks()
        {
            if (_jumpAction != null) _jumpAction.performed += HandleJump;
            if (_dodgeAction != null) _dodgeAction.performed += HandleDodge;
            if (_attackAction != null) _attackAction.performed += HandleAttack;
            if (_specialAction != null) _specialAction.performed += HandleSpecial;
            if (_ultimateAction != null) _ultimateAction.performed += HandleUltimate;
            if (_switchNextAction != null) _switchNextAction.performed += HandleSwitchNext;
            if (_switchSlot1Action != null) _switchSlot1Action.performed += HandleSwitchSlot1;
            if (_switchSlot2Action != null) _switchSlot2Action.performed += HandleSwitchSlot2;
            if (_switchSlot3Action != null) _switchSlot3Action.performed += HandleSwitchSlot3;
            if (_interactAction != null) _interactAction.performed += HandleInteract;
        }

        private void UnbindCallbacks()
        {
            if (_jumpAction != null) _jumpAction.performed -= HandleJump;
            if (_dodgeAction != null) _dodgeAction.performed -= HandleDodge;
            if (_attackAction != null) _attackAction.performed -= HandleAttack;
            if (_specialAction != null) _specialAction.performed -= HandleSpecial;
            if (_ultimateAction != null) _ultimateAction.performed -= HandleUltimate;
            if (_switchNextAction != null) _switchNextAction.performed -= HandleSwitchNext;
            if (_switchSlot1Action != null) _switchSlot1Action.performed -= HandleSwitchSlot1;
            if (_switchSlot2Action != null) _switchSlot2Action.performed -= HandleSwitchSlot2;
            if (_switchSlot3Action != null) _switchSlot3Action.performed -= HandleSwitchSlot3;
            if (_interactAction != null) _interactAction.performed -= HandleInteract;
        }

        private void HandleJump(InputAction.CallbackContext _) => OnJumpPressed?.Invoke();
        private void HandleDodge(InputAction.CallbackContext _) => OnDodgePressed?.Invoke();
        private void HandleAttack(InputAction.CallbackContext _) => OnAttackPressed?.Invoke();
        private void HandleSpecial(InputAction.CallbackContext _) => OnSpecialPressed?.Invoke();
        private void HandleUltimate(InputAction.CallbackContext _) => OnUltimatePressed?.Invoke();
        private void HandleSwitchNext(InputAction.CallbackContext _) => OnSwitchNextPressed?.Invoke();
        private void HandleSwitchSlot1(InputAction.CallbackContext _) => OnSwitchSlotPressed?.Invoke(0);
        private void HandleSwitchSlot2(InputAction.CallbackContext _) => OnSwitchSlotPressed?.Invoke(1);
        private void HandleSwitchSlot3(InputAction.CallbackContext _) => OnSwitchSlotPressed?.Invoke(2);
        private void HandleInteract(InputAction.CallbackContext _) => OnInteractPressed?.Invoke();
    }
}
