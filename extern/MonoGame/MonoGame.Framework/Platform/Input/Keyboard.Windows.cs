// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Input
{
    public static partial class Keyboard
    {
        private static readonly byte[] DefinedKeyCodes;

        private static readonly byte[] _keyState = new byte[256];
        private static readonly byte[] _keyStateTemp = new byte[256];
        private static readonly List<Keys> _keys = new List<Keys>(10);

        private static bool _isActive;

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);
        private static readonly IntPtr usLayout = LoadKeyboardLayout("00000409", 1);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MapVirtualKeyEx(int uCode, int uMapType, IntPtr dwhkl);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        private static readonly Predicate<Keys> IsKeyReleasedPredicate = key => IsKeyReleased((byte)key);

        static Keyboard()
        {
            var definedKeys = Enum.GetValues(typeof(Keys));
            var keyCodes = new List<byte>(Math.Min(definedKeys.Length, 255));
            foreach (var key in definedKeys)
            {
                var keyCode = (int)key;
                if ((keyCode >= 1) && (keyCode <= 255))
                    keyCodes.Add((byte)keyCode);
            }
            DefinedKeyCodes = keyCodes.ToArray();
        }

        private static KeyboardState PlatformGetState()
        {
            if (_isActive && GetKeyboardState(_keyState))
            {
                _keys.RemoveAll(IsKeyReleasedPredicate);

                foreach (var keyCode in DefinedKeyCodes)
                {
                    if (IsKeyReleased(keyCode))
                        continue;
                    var key = (Keys)keyCode;
                    if (!_keys.Contains(key))
                        _keys.Add(key);
                }
            }

            return new KeyboardState(_keys, Console.CapsLock, Console.NumberLock);
        }

        private static void TranslateKeyboardLayoutFromCurrentToUs(byte[] input, byte[] output)
        {
            for (int i = 0; i < output.Length; i++)
                output[i] = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == 0)
                    continue;

                // Handle these specially, they represent either the left or right version, and scancodes can't represent that
                if (i == 0x10 /* VK_SHIFT */ || i == 0x11 /* VK_CONTROL */ || i == 0x12 /* VK_MENU */)
                {
                    output[i] = input[i];
                    continue;
                }

                int scancode = MapVirtualKey(i, 4 /* MAPVK_VK_TO_VSC_EX */);
                if (scancode == 0)
                    continue;

                int mapped = MapVirtualKeyEx(scancode, 3 /* MAPVK_VSC_TO_VK_EX */, usLayout);
                if (mapped == 0)
                    continue;

                output[mapped] = input[i];
            }
        }

        private static KeyboardState PlatformGetStateByScancode()
        {
            if (_isActive && GetKeyboardState(_keyStateTemp))
            {
                TranslateKeyboardLayoutFromCurrentToUs(_keyStateTemp, _keyState);
                _keys.RemoveAll(IsKeyReleasedPredicate);

                foreach (var keyCode in DefinedKeyCodes)
                {
                    if (IsKeyReleased(keyCode))
                        continue;
                    var key = (Keys)keyCode;
                    if (!_keys.Contains(key))
                        _keys.Add(key);
                }
            }

            return new KeyboardState(_keys, Console.CapsLock, Console.NumberLock);
        }

        private static bool IsKeyReleased(byte keyCode)
        {
            return ((_keyState[keyCode] & 0x80) == 0);
        }

        internal static void SetActive(bool isActive)
        {
            _isActive = isActive;
            if (!_isActive)
                _keys.Clear();
        }
    }
}
