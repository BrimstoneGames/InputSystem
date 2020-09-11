using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace InputSystem.OldInputCompatibility
{
    // Small proxy class to get cancelled, isPressed, etc states.
    // TODO: remove this and move everything to InputAction
    internal class ActionStateListener
    {
        public bool isPressed { get; private set; }

        // TODO should this be moved to InputAction?
        private uint lastCanceledInUpdate;

        public bool cancelled => (lastCanceledInUpdate != 0) &&
                                 (lastCanceledInUpdate == InputUpdate.s_UpdateStepCount);

        public ActionStateListener(InputAction action)
        {
            action.started += c =>
            {
                isPressed = true;
            };
            action.canceled += c =>
            {
                isPressed = false;
                lastCanceledInUpdate = InputUpdate.s_UpdateStepCount;
            };
            action.performed += c =>
            {
            };
        }
    }
}