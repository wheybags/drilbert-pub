// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input
{
    internal static class KeyboardUtil
    {
        static Dictionary<int, Keys> _map;
        static Dictionary<int, Keys> _scancodeMap;

        static KeyboardUtil()
        {
            _map = new Dictionary<int, Keys>();
            _map.Add(8, Keys.Back);
            _map.Add(9, Keys.Tab);
            _map.Add(13, Keys.Enter);
            _map.Add(27, Keys.Escape);
            _map.Add(32, Keys.Space);
            _map.Add(39, Keys.OemQuotes);
            _map.Add(43, Keys.Add);
            _map.Add(44, Keys.OemComma);
            _map.Add(45, Keys.OemMinus);
            _map.Add(46, Keys.OemPeriod);
            _map.Add(47, Keys.OemQuestion);
            _map.Add(48, Keys.D0);
            _map.Add(49, Keys.D1);
            _map.Add(50, Keys.D2);
            _map.Add(51, Keys.D3);
            _map.Add(52, Keys.D4);
            _map.Add(53, Keys.D5);
            _map.Add(54, Keys.D6);
            _map.Add(55, Keys.D7);
            _map.Add(56, Keys.D8);
            _map.Add(57, Keys.D9);
            _map.Add(59, Keys.OemSemicolon);
            _map.Add(60, Keys.OemBackslash);
            _map.Add(61, Keys.OemPlus);
            _map.Add(91, Keys.OemOpenBrackets);
            _map.Add(92, Keys.OemPipe);
            _map.Add(93, Keys.OemCloseBrackets);
            _map.Add(96, Keys.OemTilde);
            _map.Add(97, Keys.A);
            _map.Add(98, Keys.B);
            _map.Add(99, Keys.C);
            _map.Add(100, Keys.D);
            _map.Add(101, Keys.E);
            _map.Add(102, Keys.F);
            _map.Add(103, Keys.G);
            _map.Add(104, Keys.H);
            _map.Add(105, Keys.I);
            _map.Add(106, Keys.J);
            _map.Add(107, Keys.K);
            _map.Add(108, Keys.L);
            _map.Add(109, Keys.M);
            _map.Add(110, Keys.N);
            _map.Add(111, Keys.O);
            _map.Add(112, Keys.P);
            _map.Add(113, Keys.Q);
            _map.Add(114, Keys.R);
            _map.Add(115, Keys.S);
            _map.Add(116, Keys.T);
            _map.Add(117, Keys.U);
            _map.Add(118, Keys.V);
            _map.Add(119, Keys.W);
            _map.Add(120, Keys.X);
            _map.Add(121, Keys.Y);
            _map.Add(122, Keys.Z);
            _map.Add(127, Keys.Delete);
            _map.Add(1073741881, Keys.CapsLock);
            _map.Add(1073741882, Keys.F1);
            _map.Add(1073741883, Keys.F2);
            _map.Add(1073741884, Keys.F3);
            _map.Add(1073741885, Keys.F4);
            _map.Add(1073741886, Keys.F5);
            _map.Add(1073741887, Keys.F6);
            _map.Add(1073741888, Keys.F7);
            _map.Add(1073741889, Keys.F8);
            _map.Add(1073741890, Keys.F9);
            _map.Add(1073741891, Keys.F10);
            _map.Add(1073741892, Keys.F11);
            _map.Add(1073741893, Keys.F12);
            _map.Add(1073741894, Keys.PrintScreen);
            _map.Add(1073741895, Keys.Scroll);
            _map.Add(1073741896, Keys.Pause);
            _map.Add(1073741897, Keys.Insert);
            _map.Add(1073741898, Keys.Home);
            _map.Add(1073741899, Keys.PageUp);
            _map.Add(1073741901, Keys.End);
            _map.Add(1073741902, Keys.PageDown);
            _map.Add(1073741903, Keys.Right);
            _map.Add(1073741904, Keys.Left);
            _map.Add(1073741905, Keys.Down);
            _map.Add(1073741906, Keys.Up);
            _map.Add(1073741907, Keys.NumLock);
            _map.Add(1073741908, Keys.Divide);
            _map.Add(1073741909, Keys.Multiply);
            _map.Add(1073741910, Keys.Subtract);
            _map.Add(1073741911, Keys.Add);
            _map.Add(1073741912, Keys.Enter);
            _map.Add(1073741913, Keys.NumPad1);
            _map.Add(1073741914, Keys.NumPad2);
            _map.Add(1073741915, Keys.NumPad3);
            _map.Add(1073741916, Keys.NumPad4);
            _map.Add(1073741917, Keys.NumPad5);
            _map.Add(1073741918, Keys.NumPad6);
            _map.Add(1073741919, Keys.NumPad7);
            _map.Add(1073741920, Keys.NumPad8);
            _map.Add(1073741921, Keys.NumPad9);
            _map.Add(1073741922, Keys.NumPad0);
            _map.Add(1073741923, Keys.Decimal);
            _map.Add(1073741925, Keys.Apps);
            _map.Add(1073741928, Keys.F13);
            _map.Add(1073741929, Keys.F14);
            _map.Add(1073741930, Keys.F15);
            _map.Add(1073741931, Keys.F16);
            _map.Add(1073741932, Keys.F17);
            _map.Add(1073741933, Keys.F18);
            _map.Add(1073741934, Keys.F19);
            _map.Add(1073741935, Keys.F20);
            _map.Add(1073741936, Keys.F21);
            _map.Add(1073741937, Keys.F22);
            _map.Add(1073741938, Keys.F23);
            _map.Add(1073741939, Keys.F24);
            _map.Add(1073741951, Keys.VolumeMute);
            _map.Add(1073741952, Keys.VolumeUp);
            _map.Add(1073741953, Keys.VolumeDown);
            _map.Add(1073742040, Keys.OemClear);
            _map.Add(1073742044, Keys.Decimal);
            _map.Add(1073742048, Keys.LeftControl);
            _map.Add(1073742049, Keys.LeftShift);
            _map.Add(1073742050, Keys.LeftAlt);
            _map.Add(1073742051, Keys.LeftWindows);
            _map.Add(1073742052, Keys.RightControl);
            _map.Add(1073742053, Keys.RightShift);
            _map.Add(1073742054, Keys.RightAlt);
            _map.Add(1073742055, Keys.RightWindows);
            _map.Add(1073742082, Keys.MediaNextTrack);
            _map.Add(1073742083, Keys.MediaPreviousTrack);
            _map.Add(1073742084, Keys.MediaStop);
            _map.Add(1073742085, Keys.MediaPlayPause);
            _map.Add(1073742086, Keys.VolumeMute);
            _map.Add(1073742087, Keys.SelectMedia);
            _map.Add(1073742089, Keys.LaunchMail);
            _map.Add(1073742092, Keys.BrowserSearch);
            _map.Add(1073742093, Keys.BrowserHome);
            _map.Add(1073742094, Keys.BrowserBack);
            _map.Add(1073742095, Keys.BrowserForward);
            _map.Add(1073742096, Keys.BrowserStop);
            _map.Add(1073742097, Keys.BrowserRefresh);
            _map.Add(1073742098, Keys.BrowserFavorites);
            _map.Add(1073742106, Keys.Sleep);

            _scancodeMap = new Dictionary<int, Keys>();
            _scancodeMap.Add(42, Keys.Back);
            _scancodeMap.Add(43, Keys.Tab);
            _scancodeMap.Add(40, Keys.Enter);
            _scancodeMap.Add(41, Keys.Escape);
            _scancodeMap.Add(44, Keys.Space);
            _scancodeMap.Add(52, Keys.OemQuotes);
            _scancodeMap.Add(54, Keys.OemComma);
            _scancodeMap.Add(45, Keys.OemMinus);
            _scancodeMap.Add(55, Keys.OemPeriod);
            _scancodeMap.Add(56, Keys.OemQuestion);
            _scancodeMap.Add(39, Keys.D0);
            _scancodeMap.Add(30, Keys.D1);
            _scancodeMap.Add(31, Keys.D2);
            _scancodeMap.Add(32, Keys.D3);
            _scancodeMap.Add(33, Keys.D4);
            _scancodeMap.Add(34, Keys.D5);
            _scancodeMap.Add(35, Keys.D6);
            _scancodeMap.Add(36, Keys.D7);
            _scancodeMap.Add(37, Keys.D8);
            _scancodeMap.Add(38, Keys.D9);
            _scancodeMap.Add(51, Keys.OemSemicolon);
            _scancodeMap.Add(100, Keys.OemBackslash);
            _scancodeMap.Add(46, Keys.OemPlus);
            _scancodeMap.Add(47, Keys.OemOpenBrackets);
            _scancodeMap.Add(49, Keys.OemPipe);
            _scancodeMap.Add(48, Keys.OemCloseBrackets);
            _scancodeMap.Add(53, Keys.OemTilde);
            _scancodeMap.Add(4, Keys.A);
            _scancodeMap.Add(5, Keys.B);
            _scancodeMap.Add(6, Keys.C);
            _scancodeMap.Add(7, Keys.D);
            _scancodeMap.Add(8, Keys.E);
            _scancodeMap.Add(9, Keys.F);
            _scancodeMap.Add(10, Keys.G);
            _scancodeMap.Add(11, Keys.H);
            _scancodeMap.Add(12, Keys.I);
            _scancodeMap.Add(13, Keys.J);
            _scancodeMap.Add(14, Keys.K);
            _scancodeMap.Add(15, Keys.L);
            _scancodeMap.Add(16, Keys.M);
            _scancodeMap.Add(17, Keys.N);
            _scancodeMap.Add(18, Keys.O);
            _scancodeMap.Add(19, Keys.P);
            _scancodeMap.Add(20, Keys.Q);
            _scancodeMap.Add(21, Keys.R);
            _scancodeMap.Add(22, Keys.S);
            _scancodeMap.Add(23, Keys.T);
            _scancodeMap.Add(24, Keys.U);
            _scancodeMap.Add(25, Keys.V);
            _scancodeMap.Add(26, Keys.W);
            _scancodeMap.Add(27, Keys.X);
            _scancodeMap.Add(28, Keys.Y);
            _scancodeMap.Add(29, Keys.Z);
            _scancodeMap.Add(76, Keys.Delete);
            _scancodeMap.Add(57, Keys.CapsLock);
            _scancodeMap.Add(58, Keys.F1);
            _scancodeMap.Add(59, Keys.F2);
            _scancodeMap.Add(60, Keys.F3);
            _scancodeMap.Add(61, Keys.F4);
            _scancodeMap.Add(62, Keys.F5);
            _scancodeMap.Add(63, Keys.F6);
            _scancodeMap.Add(64, Keys.F7);
            _scancodeMap.Add(65, Keys.F8);
            _scancodeMap.Add(66, Keys.F9);
            _scancodeMap.Add(67, Keys.F10);
            _scancodeMap.Add(68, Keys.F11);
            _scancodeMap.Add(69, Keys.F12);
            _scancodeMap.Add(70, Keys.PrintScreen);
            _scancodeMap.Add(71, Keys.Scroll);
            _scancodeMap.Add(72, Keys.Pause);
            _scancodeMap.Add(73, Keys.Insert);
            _scancodeMap.Add(74, Keys.Home);
            _scancodeMap.Add(75, Keys.PageUp);
            _scancodeMap.Add(77, Keys.End);
            _scancodeMap.Add(78, Keys.PageDown);
            _scancodeMap.Add(79, Keys.Right);
            _scancodeMap.Add(80, Keys.Left);
            _scancodeMap.Add(81, Keys.Down);
            _scancodeMap.Add(82, Keys.Up);
            _scancodeMap.Add(83, Keys.NumLock);
            _scancodeMap.Add(84, Keys.Divide);
            _scancodeMap.Add(85, Keys.Multiply);
            _scancodeMap.Add(86, Keys.Subtract);
            _scancodeMap.Add(87, Keys.Add);
            _scancodeMap.Add(88, Keys.Enter);
            _scancodeMap.Add(89, Keys.NumPad1);
            _scancodeMap.Add(90, Keys.NumPad2);
            _scancodeMap.Add(91, Keys.NumPad3);
            _scancodeMap.Add(92, Keys.NumPad4);
            _scancodeMap.Add(93, Keys.NumPad5);
            _scancodeMap.Add(94, Keys.NumPad6);
            _scancodeMap.Add(95, Keys.NumPad7);
            _scancodeMap.Add(96, Keys.NumPad8);
            _scancodeMap.Add(97, Keys.NumPad9);
            _scancodeMap.Add(98, Keys.NumPad0);
            _scancodeMap.Add(99, Keys.Decimal);
            _scancodeMap.Add(112, Keys.F21);
            _scancodeMap.Add(113, Keys.F22);
            _scancodeMap.Add(114, Keys.F23);
            _scancodeMap.Add(115, Keys.F24);
            _scancodeMap.Add(216, Keys.OemClear);
            _scancodeMap.Add(220, Keys.Decimal);
            _scancodeMap.Add(224, Keys.LeftControl);
            _scancodeMap.Add(225, Keys.LeftShift);
            _scancodeMap.Add(226, Keys.LeftAlt);
            _scancodeMap.Add(227, Keys.LeftWindows);
            _scancodeMap.Add(228, Keys.RightControl);
            _scancodeMap.Add(229, Keys.RightShift);
            _scancodeMap.Add(154, Keys.RightAlt);
            _scancodeMap.Add(231, Keys.RightWindows);
            _scancodeMap.Add(262, Keys.VolumeMute);
            _scancodeMap.Add(272, Keys.BrowserStop);
        }

        public static Keys ToXna(int key)
        {
            Keys xnaKey;
            if (_map.TryGetValue(key, out xnaKey))
                return xnaKey;

            return Keys.None;
        }

        public static Keys ScancodeToXna(int scancode)
        {
            Keys xnaKey;
            if (_scancodeMap.TryGetValue(scancode, out xnaKey))
                return xnaKey;

            return Keys.None;
        }
    }
}

