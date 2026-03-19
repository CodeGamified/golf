// Copyright CodeGamified 2025-2026
// MIT License — Golf
using UnityEngine;
using UnityEngine.InputSystem;

namespace Golf.Scripting
{
    /// <summary>
    /// Encodes keyboard/gamepad input as a single float readable by GET_INPUT opcode.
    /// Arrow keys / WASD → angle in degrees (0-360). Space → power burst (100).
    /// </summary>
    public class GolfInputProvider : MonoBehaviour
    {
        public static GolfInputProvider Instance { get; private set; }
        public float CurrentInput { get; private set; }

        private InputAction _moveAction;
        private InputAction _fireAction;

        private void Awake()
        {
            Instance = this;
            _moveAction = new InputAction("Move", InputActionType.Value, binding: "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.Enable();

            _fireAction = new InputAction("Fire", InputActionType.Button, binding: "<Keyboard>/space");
            _fireAction.AddBinding("<Gamepad>/buttonSouth");
            _fireAction.Enable();
        }

        private void Update()
        {
            // If fire pressed, output 999 (signal to putt)
            if (_fireAction.IsPressed())
            {
                CurrentInput = 999f;
                return;
            }

            // If directional input, encode as angle (0-360)
            var move = _moveAction.ReadValue<Vector2>();
            if (move.sqrMagnitude > 0.1f)
            {
                float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;
                CurrentInput = angle;
            }
            else
            {
                CurrentInput = -1f; // no input
            }
        }

        private void OnDestroy()
        {
            _moveAction?.Disable();
            _moveAction?.Dispose();
            _fireAction?.Disable();
            _fireAction?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
