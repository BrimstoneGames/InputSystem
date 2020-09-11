using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace InputSystem.OldInputCompatibility
{
    // Acquire and parse input manager configuration
    internal static class InputManagerConfiguration
    {
        public enum AxisType
        {
            Button,
            Mouse,
            Joystick,
        }

        [Serializable]
        public struct Axis
        {
#pragma warning disable 0649
            // TODO: FormerlySerializedAsAttribute is not supported by JsonUtility.FromJson ( https://fogbugz.unity3d.com/f/cases/1119033/ )
            private string m_Name;
            public string name => m_Name;
            public string negativeButton;
            public string positiveButton;
            public string altNegativeButton;
            public string altPositiveButton;
            public float gravity;
            public float dead;
            public float sensitivity;
            public bool snap;
            public bool invert;
            public AxisType type;
            public int axis;
            public int joyNum;
#pragma warning restore 0649
        }

        public static Axis[] GetCurrent()
        {
            var manager = (from obj in Resources.FindObjectsOfTypeAll<UnityEngine.Object>()
                where obj.GetType().FullName == "UnityEditor.InputManager"
                select new SerializedObject(obj)).FirstOrDefault();

            var axesProperty = manager.FindProperty("m_Axes");
            var count = axesProperty.arraySize;
            var result = new Axis[count];

            for (var i = 0; i < count; ++i)
            {
                var element = axesProperty.GetArrayElementAtIndex(i);
                var json = element.CopyToJson();
                var axis = JsonUtility.FromJson<Axis>(json);
                result[i] = axis;
            }

            return result;
        }
    };
}