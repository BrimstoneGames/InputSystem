using UnityEngine;

namespace InputSystem.OldInputCompatibility
{
    internal class ControlPathMapper
    {
        public static string GetKeyboardControlPathForKeyCode(KeyCode keyCode, string usage)
        {
            switch (keyCode)
            {
                // Bind by display name rather than key code. Means we respect keyboard layouts
                // like the old input system does.
                case KeyCode.A: return $"<Keyboard>{usage}/#{{a}}";
                case KeyCode.B: return $"<Keyboard>{usage}/#{{b}}";
                //etc.
            }

            return null;
        }

        public static string GetMouseControlPathForKeyCode(KeyCode keyCode, string usage)
        {
            switch (keyCode)
            {
                case KeyCode.Mouse0: return $"<Mouse>{usage}/leftButton";
                case KeyCode.Mouse1: return $"<Mouse>{usage}/rightButton";
                case KeyCode.Mouse2: return $"<Mouse>{usage}/middleButton";
                ////REVIEW: With these two, is it this way around or the other?
                case KeyCode.Mouse3: return $"<Mouse>{usage}/forwardButton";
                case KeyCode.Mouse4: return $"<Mouse>{usage}/backButton";
            }

            return null;
        }

        public static string GetJoystickControlPathForKeyCode(KeyCode button, string usage)
        {
            switch (button)
            {
                case KeyCode.JoystickButton0: return $"<Joystick>{usage}/trigger";
                case KeyCode.JoystickButton1: return $"<Joystick>{usage}/button2";
                case KeyCode.JoystickButton2: return $"<Joystick>{usage}/button3";
                //etc.
            }

            return null;
        }

        public static string GetGamepadControlPathForKeyCode(KeyCode button, string usage)
        {
            ////REVIEW: If we do it platform-dependent like this and move things into the importer, we'll
            ////        have to make the imported asset dependent on the build target.

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Follow Xbox layout for gamepads.
            switch (button)
            {
                // https://answers.unity.com/questions/1350081/xbox-one-controller-mapping-solved.html
                case KeyCode.JoystickButton0: return $"<Gamepad>{usage}/buttonSouth";
                case KeyCode.JoystickButton1: return $"<Gamepad>{usage}/buttonEast";
                case KeyCode.JoystickButton2: return $"<Gamepad>{usage}/buttonWest";
                //etc.
            }
#endif

            ////TODO: other platforms
            ////TODO: fallback (maybe just use Windows path?)

            return null;
        }

        private const int kMaxJoysticks = 16;
        private const int kMaxButtonsPerJoystick = 19;

        public static KeyCode MapJoystickButtonToJoystick0(KeyCode code)
        {
            for (var i = 0; i < kMaxJoysticks; ++i)
            {
                var min = KeyCode.Joystick1Button0 + i * kMaxButtonsPerJoystick;
                var max = min + kMaxButtonsPerJoystick;
                if (code >= min && code <= max)
                    return KeyCode.Joystick1Button0 + ((int)code - (int)min);
            }
            return code;
        }

        public static int GetJoystickNumber(KeyCode keyCode)
        {
            for (var i = 0; i < kMaxJoysticks; ++i)
            {
                var min = KeyCode.Joystick1Button0 + i * kMaxButtonsPerJoystick;
                var max = min + kMaxButtonsPerJoystick;
                if (keyCode >= min && keyCode <= max)
                    return i;
            }
            return -1;
        }


        // For joystick buttons and axes, we have the situation that we don't just want to blindly
        // map to joystick and gamepad buttons of the input system. Instead, for the controllers that
        // the input system explicitly supports, we want to retain the mapping that buttons and axes
        // have (on the specific platform) in the old system. This means we can end up with several
        // bindings for each such joystick button/axis in the old system.


        // public static IEnumerable<string> GetControlPathsForJoystickAxis(string button)
        // {
        //     throw new NotImplementedException();
        // }
    }
}