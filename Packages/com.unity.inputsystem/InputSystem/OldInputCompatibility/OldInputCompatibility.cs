using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace InputSystem.OldInputCompatibility
{
    public class OldInputCompatibility
    {
        /*
        // TODO remove
        private static IDictionary<string, string> remapDict = new Dictionary<string, string>
        {
            {"up", "<Keyboard>/upArrow"},
            {"down", "<Keyboard>/downArrow"},
            {"left", "<Keyboard>/leftArrow"},
            {"right", "<Keyboard>/rightArrow"}
        };

        private static IDictionary<string, ctionWrapper> axes = new Dictionary<string, ActionWrapper>();

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
                    //throw new NotImplementedException("Mouse axes are not supported");
                    break;
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
        */

        private static InputActionMap s_Actions;

        public static void BootstrapInputConfiguration()
        {
            GamepadsAndJoysticksMonitor.Enable();

            s_Actions = new InputActionMap("InputManagerLegacy");
            foreach(var axis in InputManagerConfiguration.GetCurrent())
            {
                // Add action, if we haven't already.
                ////REVIEW: Unlike the old input manager, FindAction is case-insensitive. Might be undesirable here.
                var action = s_Actions.FindAction(axis.name);
                if (action == null)
                {
                    // All InputManager axis are float values. We don't really know from the configuration
                    // what's considered a button and was is considered just an axis.
                    action = s_Actions.AddAction(axis.name, InputActionType.Value);
                    action.expectedControlType = "Axis";
                }

                switch (axis.type)
                {
                    case InputManagerConfiguration.AxisType.Button:
                        AddButtonBindings(action, axis);
                        break;

                    case InputManagerConfiguration.AxisType.Mouse:
                        ////TODO
                        break;

                    case InputManagerConfiguration.AxisType.Joystick:
                        ////TODO
                        break;
                }
            }

            // JoystickMonitor.Setup();
            //
            // // TODO we need a different way to get the configuration
            // var axesSettings = Resources.FindObjectsOfTypeAll<UnityEngine.Object>()
            //     .Where(o => o.GetType().FullName == "UnityEditor.InputManager")
            //     .Select(o => new SerializedObject(o).FindProperty("m_Axes"))
            //     .ToArray();
            //
            // foreach (SerializedProperty axesSetting in axesSettings)
            // foreach (SerializedProperty axisSettings in axesSetting)
            //     ConsumeInputManagerAxisSettings(axisSettings);
            //

            Input.provider = new ApiShimDataProvider();
        }

                private static void AddButtonBindings(InputAction action, InputManagerConfiguration.Axis axis)
        {
            ////TODO: add support for having only negativeButton/altNegativeButton (i.e. [-1..0])

            ////REVIEW: These probably don't apply to buttons?
            var processors = StringHelpers.Join(new[]
            {
                axis.invert ? "invert" : null,
                !Mathf.Approximately(axis.sensitivity, 0) ? $"scale(factor={axis.sensitivity}" : null,
                ////TODO: snap
                ////TODO: gravity
            }, ",");

            if (!string.IsNullOrEmpty(axis.positiveButton) && !string.IsNullOrEmpty(axis.negativeButton))
                AddAxisComposite(action, axis.positiveButton, axis.negativeButton, processors);
            else if (string.IsNullOrEmpty(axis.positiveButton))
                action.AddInputManagerBinding(axis.positiveButton, processors);
            if (!string.IsNullOrEmpty(axis.altPositiveButton) && !string.IsNullOrEmpty(axis.altNegativeButton))
                AddAxisComposite(action, axis.altPositiveButton, axis.altNegativeButton, processors);
            else if (string.IsNullOrEmpty(axis.altPositiveButton))
                action.AddInputManagerBinding(axis.altPositiveButton, processors);
        }

        private static void AddAxisComposite(InputAction action, string positive, string negative, string processors)
        {
            action.AddCompositeBinding("1DAxis")
                .WithInputManagerBinding("Positive", positive, processors)
                .WithInputManagerBinding("Negative", negative, processors);
        }

        private static InputActionSetupExtensions.CompositeSyntax WithInputManagerBinding(this InputActionSetupExtensions.CompositeSyntax composite, string partName, string binding, string processors)
        {
            var keyCode = KeyNames.NameToKey(binding);

            // If the binding is associated with a particular joystick, reflect that
            // through a usage tag on the binding.
            var joyNum = ControlPathMapper.GetJoystickNumber(keyCode);
            var usage = default(string);
            if (joyNum >= 1)
                usage = GamepadsAndJoysticksMonitor.JoyNumToUsage(joyNum);

            keyCode = ControlPathMapper.MapJoystickButtonToJoystick0(keyCode);

            var gamepad = ControlPathMapper.GetGamepadControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(gamepad))
                composite.With(partName, gamepad, processors: processors);

            var joystick = ControlPathMapper.GetJoystickControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(joystick))
                composite.With(partName, joystick, processors: processors);

            var keyboard = ControlPathMapper.GetKeyboardControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(keyboard))
                composite.With(partName, keyboard, processors: processors);

            var mouse = ControlPathMapper.GetMouseControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(mouse))
                composite.With(partName, mouse, processors: processors);

            return composite;
        }

        private static void AddInputManagerBinding(this InputAction action, string binding, string processors)
        {
            var keyCode = StringToKey(binding);

            // If the binding is associated with a particular joystick, reflect that
            // through a usage tag on the binding.
            var joyNum = ControlPathMapper.GetJoystickNumber(keyCode);
            var usage = default(string);
            if (joyNum >= 1)
                usage = GamepadsAndJoysticksMonitor.JoyNumToUsage(joyNum);

            keyCode = ControlPathMapper.MapJoystickButtonToJoystick0(keyCode);

            var gamepad = ControlPathMapper.GetGamepadControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(gamepad))
                action.AddBinding(gamepad, processors: processors);

            var joystick = ControlPathMapper.GetJoystickControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(joystick))
                action.AddBinding(joystick, processors: processors);

            var keyboard = ControlPathMapper.GetKeyboardControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(keyboard))
                action.AddBinding(keyboard, processors: processors);

            var mouse = ControlPathMapper.GetMouseControlPathForKeyCode(keyCode, usage);
            if (!string.IsNullOrEmpty(mouse))
                action.AddBinding(mouse, processors: processors);
        }


        public static void Enable()
        {
            //foreach (var pair in axes)
            //    pair.Value.action.Enable();
        }

        public static void Disable()
        {
            //foreach (var pair in axes)
            //    pair.Value.action.Disable();
        }

        public static void OnUpdate()
        {
        }
    }
}
