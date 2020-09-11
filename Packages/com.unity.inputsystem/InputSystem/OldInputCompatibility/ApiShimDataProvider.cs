using UnityEngine;

namespace InputSystem.OldInputCompatibility
{
    internal class ApiShimDataProvider : Input.DataProvider
    {
        public ApiShimDataProvider()
        {

        }

        public override float GetAxis(string name)
        {
            //return axes.TryGetValue(name, out var wrapper) ? wrapper.action.ReadValue<float>() : 0.0f;
            return 0.0f;
        }

        public override bool GetButton(string name)
        {
            //return axes.TryGetValue(name, out var wrapper) && wrapper.isPressed;
            return false;
        }

        public override bool GetButtonDown(string name)
        {
            //return axes.TryGetValue(name, out var wrapper) && wrapper.action.triggered;
            return false;
        }

        public override bool GetButtonUp(string name)
        {
            //return axes.TryGetValue(name, out var wrapper) && wrapper.cancelled;
            return false;
        }
    };
}