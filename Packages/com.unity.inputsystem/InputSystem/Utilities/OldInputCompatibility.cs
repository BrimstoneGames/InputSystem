
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Utilities
{
    public class OldInputCompatibility
    {
        private class ActionWrapper
        {
            public InputAction action { get; }

            // TODO is InputAction.phase is identical to this?
            public bool isPressed { get; private set; }

            // TODO should this be moved to InputAction?
            private uint lastCanceledInUpdate;

            public bool cancelled => (lastCanceledInUpdate != 0) &&
                                     (lastCanceledInUpdate == InputUpdate.s_UpdateStepCount);

            public ActionWrapper(string name)
            {
                action = new InputAction(name);
                action.started += c =>
                {
                    isPressed = true;
                    //Debug.Log($"action {action.name} started at {Time.frameCount}");
                };
                action.canceled += c =>
                {
                    isPressed = false;
                    lastCanceledInUpdate = InputUpdate.s_UpdateStepCount;
                    //Debug.Log($"action {action.name} canceled at {Time.frameCount}");
                };
                action.performed += c =>
                {
                    //Debug.Log($"action {action.name} performed at {Time.frameCount}");
                };
            }
        };

        private static IDictionary<string, string> remapDict = new Dictionary<string, string>
        {
            {"up", "<Keyboard>/upArrow"},
            {"down", "<Keyboard>/downArrow"},
            {"left", "<Keyboard>/leftArrow"},
            {"right", "<Keyboard>/rightArrow"}
        };

        private static IDictionary<string, ActionWrapper> axes = new Dictionary<string, ActionWrapper>();

        public static float GetAxis(string name)
        {
            return axes.TryGetValue(name, out ActionWrapper wrapper) ? wrapper.action.ReadValue<float>() : 0.0f;
        }

        public static bool GetButton(string name)
        {
            return axes.TryGetValue(name, out ActionWrapper wrapper) ? wrapper.isPressed : false;
        }

        public static bool GetButtonDown(string name)
        {
            return axes.TryGetValue(name, out ActionWrapper wrapper) ? wrapper.action.triggered : false;
        }

        public static bool GetButtonUp(string name)
        {
            return axes.TryGetValue(name, out ActionWrapper wrapper) ? wrapper.cancelled : false;
        }

        private static string RemapButtons(string name)
        {
            if (name.Length == 0)
                return null;

            if (name.StartsWith("joystick"))
            {
                // "joystick 1 button 0" format

                var parts = name.Split(' ');
                if (parts.Length < 3 || parts[0] != "joystick" || parts[2] != "button")
                    return null;

                var joyNum = Int32.Parse(parts[1]);
                var button = Int32.Parse(parts[3]);

                // a very rough mapping based on http://wiki.unity3d.com/index.php?title=Xbox360Controller
                // TODO where joyNum goes?
                switch (button)
                {
                    case 0: return $"<Gamepad>/buttonSouth";
                    case 1: return $"<Gamepad>/buttonEast";
                    case 2: return $"<Gamepad>/buttonWest";
                    case 3: return $"<Gamepad>/buttonNorth";
                }

                throw new NotImplementedException($"not supported joystick '{name}'");
            }
            else if (remapDict.TryGetValue(name, out string remap))
                return remap;
            else
                return $"<Keyboard>/{name}";
        }

        private static void ConsumeInputManagerAxisSettings(SerializedProperty p)
        {
            if (p == null)
                return;

            // foreach (SerializedProperty b in p)
            //     Debug.Log($"type={b.propertyType}, name={b.name}");
            // Debug.Log("----");
            // return;

            var name = p.FindPropertyRelative("m_Name").stringValue;

            var mappedButtons = new List<(string axisDirection, string propertyName)>
                {
                    ("Positive", "positiveButton"),
                    ("Negative", "negativeButton"),
                    ("Positive", "altPositiveButton"),
                    ("Negative", "altNegativeButton")
                }
                .Select(t => (t.axisDirection,
                    buttonBinding: RemapButtons(p.FindPropertyRelative(t.propertyName).stringValue)))
                .ToArray();

            var axisType = p.FindPropertyRelative("type").enumValueIndex;
            var axisValue = p.FindPropertyRelative("axis").enumValueIndex;
            var joyNum = p.FindPropertyRelative("joyNum").enumValueIndex;

            ActionWrapper wrap = null;
            if (!axes.TryGetValue(name, out wrap))
            {
                wrap = new ActionWrapper(name);
                axes[name] = wrap;
                Debug.Log($"add action {name}");
            }

            switch (axisType)
            {
                case 0: // button
                    if (mappedButtons.Any())
                    {
                        var binding = wrap.action.AddCompositeBinding("Axis");
                        foreach (var mappedButton in mappedButtons)
                            binding = binding.With(mappedButton.axisDirection, mappedButton.buttonBinding);
                    }

                    break;
                case 1: // mouse
                    throw new NotImplementedException("Mouse axes are not supported");
                case 2: // joystick
                    Debug.Log($"joystick {joyNum} axis {axisValue}");
                    // TODO completely not clear how to combine/split two axes with a 2d controller?

                    switch (axisValue)
                    {
                        case 0:
                            wrap.action.AddBinding("<Gamepad>/leftStick/x");
                            break;
                        case 1:
                            wrap.action.AddBinding("<Gamepad>/leftStick/y");
                            break;
                        case 2:
                            wrap.action.AddBinding("<Gamepad>/rightStick/x");
                            break;
                        case 3:
                            wrap.action.AddBinding("<Gamepad>/rightStick/y");
                            break;
                    }

                    break;
            }

            // * m_Name
            // * negativeButton
            // * positiveButton
            // * altNegativeButton
            // * altPositiveButton
            // * gravity
            // * dead
            // * sensitivity
            // * snap
            // * invert
            // * type
            // * axis
            // * joyNum

            // enum AxisType
            // {
            //     kAxisButton,
            //     kAxisMouse,
            //     kAxisJoystick,
            // };
        }

        public static void BootstrapInputConfiguration()
        {
            // TODO we need a different way to get the configuration
            var axesSettings = Resources.FindObjectsOfTypeAll<UnityEngine.Object>()
                .Where(o => o.GetType().FullName == "UnityEditor.InputManager")
                .Select(o => new SerializedObject(o).FindProperty("m_Axes"))
                .ToArray();

            foreach (SerializedProperty axesSetting in axesSettings)
            foreach (SerializedProperty axisSettings in axesSetting)
                ConsumeInputManagerAxisSettings(axisSettings);

            Input.GetAxisCallback = GetAxis;
            Input.GetButtonCallback = GetButton;
            Input.GetButtonDownCallback = GetButtonDown;
            Input.GetButtonUpCallback = GetButtonUp;
        }

        public static void Enable()
        {
            foreach (var pair in axes)
                pair.Value.action.Enable();
        }

        public static void Disable()
        {
            foreach (var pair in axes)
                pair.Value.action.Disable();
        }

        public static void OnUpdate()
        {
        }
    }
}
