using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace InputSystem.OldInputCompatibility
{
    // Helper class to add device usage to devices as they come and go.
    internal static class GamepadsAndJoysticksMonitor
    {
        private static int m_JoystickCount;
        private static InputDevice[] m_Joysticks;

        public static string JoyNumToUsage(int joyNum)
        {
            return $"LegacyJoystick{joyNum}";
        }

        public static void Enable()
        {
            // Collect all joysticks and gamepads.
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
                if (device is Joystick || device is Gamepad)
                {
                    var index = ArrayHelpers.AppendWithCapacity(ref m_Joysticks, ref m_JoystickCount, device);
                    UnityEngine.InputSystem.InputSystem.AddDeviceUsage(device, JoyNumToUsage(index));
                }

            // Monitor joysticks and gamepads.
            UnityEngine.InputSystem.InputSystem.onDeviceChange +=
                (device, change) =>
                {
                    if (change == InputDeviceChange.Removed)
                    {
                        var index = m_Joysticks.IndexOfReference(device);
                        if (index != -1)
                        {
                            ArrayHelpers.EraseAtWithCapacity(m_Joysticks, ref m_JoystickCount, index);
                            UnityEngine.InputSystem.InputSystem.RemoveDeviceUsage(device, JoyNumToUsage(index));
                        }
                    }
                    else if (change == InputDeviceChange.Added && (device is Joystick || device is Gamepad))
                    {
                        var index = ArrayHelpers.AppendWithCapacity(ref m_Joysticks, ref m_JoystickCount, device);
                        UnityEngine.InputSystem.InputSystem.AddDeviceUsage(device, JoyNumToUsage(index));
                    }
                };
        }
    };
}