using UnityEngine; public static class TreeFixtureInput { public static KeyCode Key; public static bool Pressed(KeyCode key) => key != KeyCode.None && Key == key; }
