
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Utilities
{
    public class OldInputCompatibility
    {
        private static Input.StateWrapper GetStateWrapper(string name)
        {
            if (Input.axes.TryGetValue(name, out Input.StateWrapper wrapper))
                return wrapper;
            var newWrapper = new Input.StateWrapper();
            Input.axes[name] = newWrapper;
            return newWrapper;
        }

        private class ActionWrapper
        {
            public InputAction action { get; }

            public ActionWrapper(string name)
            {
                action = new InputAction(name);
                action.started += c =>
                {
                    var wrapper = GetStateWrapper(action.name);
                    wrapper.isDown = true;
                    wrapper.isPressed = true;
                };
                action.canceled += c =>
                {
                    var wrapper = GetStateWrapper(action.name);
                    wrapper.isPressed = false;
                    wrapper.isUp = true;
                };
            }

            public void OnUpdate()
            {
                var wrapper = GetStateWrapper(action.name);
                wrapper.axis = action.ReadValue<float>();
                wrapper.isUp = false; // TODO this is completely wrong
                wrapper.isDown = false;
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

        private static string RemapButtons(string name)
        {
            if (name.Length == 0)
                return null;

            if (name.StartsWith("joystick"))
            {
                // "joystick 1 button 0" format

                var parts = name.Split(' ');
                if (parts.Length < 3 || parts[0] != "joystick")
                    return null;

                return $"<Gamepad>/b"; // TODO
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
                    break;
            }


            //buttons["Fire1"] = new ActionWrapper(new InputAction("Fire1", binding: "<Gamepad>/b"));
            //buttons["Fire1"].action.AddBinding("<Keyboard>/space");

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


            // var b = a.FindPropertyRelative("negativeButton");
            // Debug.Log($"type={b.propertyType}, name={b.name}, value={b.stringValue}");

            //foreach (SerializedProperty b in a)
            //    Debug.Log($"type={b.propertyType}, name={b.name}");
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
            foreach (var pair in axes)
                pair.Value.OnUpdate();
        }
    }
}
