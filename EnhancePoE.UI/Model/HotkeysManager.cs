﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using EnhancePoE.UI.Properties;
using Serilog;

namespace EnhancePoE.UI
{
    /// <summary>
    ///     A class for adding/removing global hotkeys to and from your application,
    ///     meaning these hotkeys can be run even if your application isn't focused.
    /// </summary>
    public static class HotkeysManager
    {
        // Events

        public delegate void HotkeyEvent(GlobalHotkey hotkey);

        // The build in proc ID for telling windows to hook onto the
        // low level keyboard events with the SetWindowsHookEx function
        private const int WH_KEYBOARD_LL = 13;
        private static readonly LowLevelKeyboardProc LowLevelProc = HookCallback;

        // The system hook ID (for storing this application's hook)
        private static IntPtr HookID = IntPtr.Zero;

        public static ModifierKeys refreshModifier;
        public static List<ModifierKeys> refreshModifiers;
        public static Key refreshKey;

        public static ModifierKeys toggleModifier;
        public static List<ModifierKeys> toggleModifiers;
        public static Key toggleKey;

        public static ModifierKeys stashTabModifier;
        public static List<ModifierKeys> stashTabModifiers;
        public static Key stashTabKey;

        private static readonly ILogger _logger;

        //public static ModifierKeys reloadFilterModifier;
        //public static Key reloadFilterKey;

        private static bool IsPressed { get; set; }

        static HotkeysManager()
        {
            _logger = Log.Logger;
            Hotkeys = new List<GlobalHotkey>();
            refreshModifiers = new List<ModifierKeys>();
            toggleModifiers = new List<ModifierKeys>();
            stashTabModifiers = new List<ModifierKeys>();
            RequiresModifierKey = false;
        }

        // All of the Hotkeys
        private static List<GlobalHotkey> Hotkeys { get; set; }

        /// <summary>
        ///     States whether the system keyboard event hook is setup.
        /// </summary>
        public static bool IsHookSetup { get; private set; }

        /// <summary>
        ///     States whether hotkeys require modifier keys to be scanned (and therefore
        ///     have a chance for their callback method to be called). If this is disabled,
        ///     the hotkeys will be checked in every key stroke/event, so pressing just 'A' could
        ///     fire a hotkey if there is one with no modifiers and just it's key set to 'A'.
        ///     <para>
        ///         If enabled, a modifier key is required on hotkeys. if the hotkey
        ///         has no modifiers, then it simply wont be scanned at all.
        ///     </para>
        /// </summary>
        public static bool RequiresModifierKey { get; set; }

        /// <summary>
        ///     Fired when a hotkey is fired (duh lol).
        /// </summary>
        public static event HotkeyEvent HotkeyFired;

        /// <summary>
        ///     Hooks/Sets up this application for receiving keydown callbacks
        /// </summary>
        public static void SetupSystemHook()
        {
            HookID = SetHook(LowLevelProc);
            IsHookSetup = true;
            _logger.Debug("System hooks setup successfully");
        }

        /// <summary>
        ///     Unhooks this application, stopping it from receiving keydown callbacks
        /// </summary>
        public static void ShutdownSystemHook()
        {
            UnhookWindowsHookEx(HookID);
            IsHookSetup = false;
        }

        /// <summary>
        ///     Adds a hotkey to the hotkeys list.
        /// </summary>
        public static void AddHotkey(GlobalHotkey hotkey)
        {
            Hotkeys.Add(hotkey);
        }

        /// <summary>
        ///     Removes a hotkey from the hotkeys list.
        /// </summary>
        /// <param name="hotkey"></param>
        public static void RemoveHotkey(GlobalHotkey hotkey)
        {
            Hotkeys.Remove(hotkey);
        }

        /// <summary>
        ///     Too replace all the shit if statments in CheckHotKeys with more than one modifier
        /// </summary>
        public static bool ValidKeyCombo(GlobalHotkey hotkey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Checks if there are any modifiers are pressed. If so, it checks through every
        ///     Hotkey and matches their Modifier/Key. If they both match, and the hotkey allows
        ///     the callback method to be called, it is called.
        /// </summary>
        private static void CheckHotkeys()
        {
            foreach (var hotkey in Hotkeys)
            {
                if (hotkey.reqModifiers && Keyboard.Modifiers != ModifierKeys.None && !IsPressed)
                {
                    if (hotkey.Modifiers != null)
                    {
                        var ctrl_alt_shift = hotkey.Modifiers.Contains(ModifierKeys.Control) &&
                            hotkey.Modifiers.Contains(ModifierKeys.Shift) &&
                            hotkey.Modifiers.Contains(ModifierKeys.Alt);
                        var ctrl_alt = hotkey.Modifiers.Contains(ModifierKeys.Control) &&
                            hotkey.Modifiers.Contains(ModifierKeys.Alt) && !hotkey.Modifiers.Contains(ModifierKeys.Shift);
                        var ctrl_shift = hotkey.Modifiers.Contains(ModifierKeys.Control) &&
                            hotkey.Modifiers.Contains(ModifierKeys.Shift) && !hotkey.Modifiers.Contains(ModifierKeys.Alt);
                        var shift_alt = hotkey.Modifiers.Contains(ModifierKeys.Shift) &&
                            hotkey.Modifiers.Contains(ModifierKeys.Alt) && !hotkey.Modifiers.Contains(ModifierKeys.Control);

                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift) &&
                            ctrl_alt_shift && Keyboard.IsKeyDown(hotkey.Key))
                        {
                            IsPressed = true;
                            if (hotkey.CanExecute)
                            {
                                hotkey.Callback?.Invoke();
                                HotkeyFired?.Invoke(hotkey);
                            }
                        }
                        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Alt) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) &&
                            ctrl_alt && Keyboard.IsKeyDown(hotkey.Key))
                        {
                            IsPressed = true;
                            if (hotkey.CanExecute)
                            {
                                hotkey.Callback?.Invoke();
                                HotkeyFired?.Invoke(hotkey);
                            }
                        }
                        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) &&
                            ctrl_shift && Keyboard.IsKeyDown(hotkey.Key))
                        {
                            IsPressed = true;
                            if (hotkey.CanExecute)
                            {
                                hotkey.Callback?.Invoke();
                                HotkeyFired?.Invoke(hotkey);
                            }
                        }
                        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift | ModifierKeys.Alt) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                            shift_alt && Keyboard.IsKeyDown(hotkey.Key))
                        {
                            IsPressed = true;
                            if (hotkey.CanExecute)
                            {
                                hotkey.Callback?.Invoke();
                                HotkeyFired?.Invoke(hotkey);
                            }
                        }
                        Debug.WriteLine("IsPressed: " + IsPressed.ToString());
                    }
                    else if (hotkey.Modifiers == null && hotkey.Modifier != ModifierKeys.None && !IsPressed)
                    {
                        if (hotkey.Key != Key.None)
                        {
                            if (Keyboard.IsKeyDown(hotkey.Key))
                            {
                                IsPressed = true;
                                if (hotkey.CanExecute)
                                {
                                    hotkey.Callback?.Invoke();
                                    HotkeyFired?.Invoke(hotkey);
                                }
                            }
                        }
                    }
                }
                else if (!hotkey.reqModifiers && Keyboard.Modifiers == ModifierKeys.None && !IsPressed)
                {
                    if (hotkey.Key != Key.None)
                    {
                        if (Keyboard.Modifiers.HasFlag(hotkey.Modifier) && Keyboard.IsKeyDown(hotkey.Key))
                        {
                            IsPressed = true;
                            if (hotkey.CanExecute)
                            {
                                hotkey.Callback?.Invoke();
                                HotkeyFired?.Invoke(hotkey);
                            }
                        }
                        
                    }
                }
                
                if (Keyboard.IsKeyUp(hotkey.Key) && IsPressed)
                {
                    Debug.WriteLine("I was ran");
                    IsPressed = false;
                }
            }
            /*if (RequiresModifierKey)
            {
                if (Keyboard.Modifiers != ModifierKeys.None)
                {
                    foreach (var hotkey in Hotkeys)
                    {
                        if (hotkey.Modifiers != null)
                        {
                            var ctrl_alt_shift = hotkey.Modifiers.Contains(ModifierKeys.Control) && 
                                hotkey.Modifiers.Contains(ModifierKeys.Shift) && 
                                hotkey.Modifiers.Contains(ModifierKeys.Alt);
                            var ctrl_alt = hotkey.Modifiers.Contains(ModifierKeys.Control) &&
                                hotkey.Modifiers.Contains(ModifierKeys.Alt) && !hotkey.Modifiers.Contains(ModifierKeys.Shift);
                            var ctrl_shift = hotkey.Modifiers.Contains(ModifierKeys.Control) &&
                                hotkey.Modifiers.Contains(ModifierKeys.Shift) && !hotkey.Modifiers.Contains(ModifierKeys.Alt);
                            var shift_alt = hotkey.Modifiers.Contains(ModifierKeys.Shift) &&
                                hotkey.Modifiers.Contains(ModifierKeys.Alt) && !hotkey.Modifiers.Contains(ModifierKeys.Control);

                            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift) &&
                                ctrl_alt_shift && Keyboard.IsKeyDown(hotkey.Key) && !isPressed)
                            {
                                isPressed = true;
                                if (hotkey.CanExecute)
                                {
                                    hotkey.Callback?.Invoke();
                                    HotkeyFired?.Invoke(hotkey);
                                }
                            }
                            else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Alt) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) &&
                                ctrl_alt && Keyboard.IsKeyDown(hotkey.Key) && !isPressed)
                            {
                                isPressed = true;
                                if (hotkey.CanExecute)
                                {
                                    hotkey.Callback?.Invoke();
                                    HotkeyFired?.Invoke(hotkey);
                                }
                            }
                            else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) &&
                                ctrl_shift && Keyboard.IsKeyDown(hotkey.Key) && !isPressed)
                            {
                                isPressed = true;
                                if (hotkey.CanExecute)
                                {
                                    hotkey.Callback?.Invoke();
                                    HotkeyFired?.Invoke(hotkey);
                                }
                            }
                            else if(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift | ModifierKeys.Alt) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                                shift_alt && Keyboard.IsKeyDown(hotkey.Key) && !isPressed)
                            {
                                isPressed = true;
                                if (hotkey.CanExecute)
                                {
                                    hotkey.Callback?.Invoke();
                                    HotkeyFired?.Invoke(hotkey);
                                }
                            }

                            if (Keyboard.IsKeyUp(hotkey.Key) && isPressed)
                            {
                                isPressed = false;
                            }
                        }
                        
                    }
                    
                }
            }
            else
            {
                Debug.WriteLine("I was here");
                foreach (var hotkey in Hotkeys)
                {
                    if (hotkey.Key != Key.None)
                    {
                        if (Keyboard.Modifiers.HasFlag(hotkey.Modifier) && Keyboard.IsKeyDown(hotkey.Key) && !isPressed)
                        {
                            isPressed = true;
                            if (hotkey.CanExecute)
                            {
                                hotkey.Callback?.Invoke();
                                HotkeyFired?.Invoke(hotkey);
                            }
                        }

                        if(Keyboard.IsKeyUp(hotkey.Key) && isPressed)
                        {
                            isPressed = false;
                        }
                    }
                }
            }*/
        }

        /// <summary>
        ///     Finds and returns all hotkeys in the hotkeys list that have matching modifiers and keys given
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="key"></param>
        /// <param name="callbackMethod">If this is not null, the callback method will be checked</param>
        /// <returns></returns>
        public static List<GlobalHotkey> FindHotkeys(ModifierKeys modifier, Key key)
        {
            var hotkeys = new List<GlobalHotkey>();
            foreach (var hotkey in Hotkeys)
                if (hotkey.Key == key && hotkey.Modifier == modifier)
                    hotkeys.Add(hotkey);

            return hotkeys;
        }
        public static List<GlobalHotkey> FindHotkeys(List<ModifierKeys> modifiers, Key key)
        {
            var hotkeys = new List<GlobalHotkey>();
            foreach (var hotkey in Hotkeys)
                if (hotkey.Key == key && hotkey.Modifiers == modifiers)
                    hotkeys.Add(hotkey);

            return hotkeys;
        }

        /// <summary>
        ///     Creates and adds a new hotkey to the hotkeys list.
        /// </summary>
        /// <param name="modifier">The modifier key. ALT Does not work.</param>
        /// <param name="key"></param>
        /// <param name="callbackMethod"></param>
        /// <param name="canExecute"></param>
        public static void AddHotkey(ModifierKeys modifier, Key key, Action callbackMethod,bool reqModifiers = false, bool canExecute = true)
        {
            if(modifier != ModifierKeys.None) reqModifiers = true;
            AddHotkey(new GlobalHotkey(modifier, key, callbackMethod, reqModifiers, canExecute));
        }
        public static void AddHotkey(List<ModifierKeys> modifiers, Key key, Action callbackMethod, bool canExecute = true)
        {
            AddHotkey(new GlobalHotkey(modifiers, key, callbackMethod, canExecute));
        }

        /// <summary>
        ///     Removes a or all hotkey from the hotkeys list (depending on
        ///     <paramref name="removeAllOccourances" />) by going through every hotkey
        ///     and checking it's modifier and key to see if they match. is so, it removes it.
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="key"></param>
        /// <param name="removeAllOccourances">
        ///     If this is false, the first found hotkey will be removed.
        ///     else, every occourance will be removed.
        /// </param>
        public static void RemoveHotkey(ModifierKeys modifier, Key key, bool removeAllOccourances = false)
        {
            var originalHotkeys = Hotkeys;
            var toBeRemoved = FindHotkeys(modifier, key);

            if (toBeRemoved.Count > 0)
            {
                if (removeAllOccourances)
                {
                    foreach (var hotkey in toBeRemoved) originalHotkeys.Remove(hotkey);

                    Hotkeys = originalHotkeys;
                }
                else
                {
                    RemoveHotkey(toBeRemoved[0]);
                }
            }
        }
        public static void RemoveHotkey(List<ModifierKeys> modifiers, Key key, bool removeAllOccourances = false)
        {
            var originalHotkeys = Hotkeys;
            var toBeRemoved = FindHotkeys(modifiers, key);

            if (toBeRemoved.Count > 0)
            {
                if (removeAllOccourances)
                {
                    foreach (var hotkey in toBeRemoved) originalHotkeys.Remove(hotkey);

                    Hotkeys = originalHotkeys;
                }
                else
                {
                    RemoveHotkey(toBeRemoved[0]);
                }
            }
        }

        /// <summary>
        ///     Sets up the Key Up/Down event hooks.
        /// </summary>
        /// <param name="proc">The callback method to be called when a key up/down occours</param>
        /// <returns></returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        /// <summary>
        ///     The method called when a key up/down occours across the system.
        /// </summary>
        /// <param name="nCode">idk tbh</param>
        /// <param name="lParam">LPARAM, contains the key that was pressed. not used atm</param>
        /// <returns>LRESULT</returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Checks if this is called from keydown only because key ups aren't used.
            if (nCode >= 0)
                CheckHotkeys();

            // Cannot use System.Windows' keys because
            // they dont use the same values as windows
            //int vkCode = Marshal.ReadInt32(lParam);
            //System.Windows.Forms.Keys key = (System.Windows.Forms.Keys)vkCode;
            //Debug.WriteLine(key);

            // I think this tells windows that this app has successfully
            // handled the key events and now other apps can handle it next.
            return CallNextHookEx(HookID, nCode, wParam, lParam);
        }

/*        public static void GetRefreshHotkey()
        {
            if (Settings.Default.HotkeyRefresh != "< not set >")
            {
                var refreshString = Settings.Default.HotkeyRefresh.Split('+');

                if (refreshString.Length > 1)
                {
                    if (refreshString[0].Trim() == "Ctrl")
                        refreshModifier = ModifierKeys.Control;
                    else if (refreshString[0].Trim() == "Alt")
                        refreshModifier = ModifierKeys.Alt;
                    else if (refreshString[0].Trim() == "Win")
                        refreshModifier = ModifierKeys.Windows;
                    else if (refreshString[0].Trim() == "Shift")
                        refreshModifier = ModifierKeys.Shift;
                    else
                        refreshModifier = ModifierKeys.None;

                    Enum.TryParse(refreshString[1].Trim(), out refreshKey);
                }
                else
                {
                    Enum.TryParse(refreshString[0].Trim(), out refreshKey);
                    refreshModifier = ModifierKeys.None;
                }
            }
        }*/

        public static void RefreshHotkey(string hotkey)
        {
            if (hotkey != "< not set >")
            {
                string[] split_string = hotkey.Split('+');
                
                if (split_string.Length > 1 && split_string.Length < 3)
                {
                    string compare_string = split_string[0].Trim().ToLower();
                    ModifierKeys temp_key;
                    if (compare_string.Equals("ctrl"))
                    {
                        temp_key = ModifierKeys.Control;
                    }
                    else if (compare_string.Equals("alt"))
                    {
                        temp_key = ModifierKeys.Alt;
                    }
                    else if (compare_string.Equals("shift"))
                    {
                        temp_key = ModifierKeys.Shift;
                    }
                    else if (compare_string.Equals("win"))
                    {
                        temp_key = ModifierKeys.Windows;
                    }
                    else
                    {
                        temp_key = ModifierKeys.None;
                    }

                    SetModifier(hotkey, temp_key);
                    SetKey(hotkey);
                }
                else if (split_string.Length > 2)
                {
                    List<ModifierKeys> temp = new List<ModifierKeys>();
                    for (int i = 0; i < split_string.Length; i++)
                    {
                        string cmp = split_string[i].Trim().ToLower();
                        if (cmp.Contains("alt"))
                        {
                            temp.Add(ModifierKeys.Alt);
                        }
                        else if (cmp.Contains("ctrl"))
                        {
                            temp.Add(ModifierKeys.Control);
                        }
                        else if (cmp.Contains("shift"))
                        {
                            temp.Add(ModifierKeys.Shift);
                        }
                        else if (cmp.Contains("win"))
                        {
                            temp.Add(ModifierKeys.Windows);
                        }
                    }

                    SetModifier(hotkey, temp);
                    SetKey(hotkey);
                }
                else
                {
                    SetModifier(hotkey, ModifierKeys.None);
                    SetKey(hotkey);
                }
            }
        }

        private static void SetModifier(string hotkey, ModifierKeys mod)
        {
            string typeOfHotKey = GetHotkeyType(hotkey);
            if (typeOfHotKey == "") return;

            if (typeOfHotKey == "refresh")
            {
                refreshModifier = mod;
            }
            else if (typeOfHotKey == "toggle")
            {
                toggleModifier = mod;
            }
            else if (typeOfHotKey == "stashtab")
            {
                stashTabModifier = mod;
            }
        }
        private static void SetModifier(string hotkey, List<ModifierKeys> mods)
        {
            string typeOfHotKey = GetHotkeyType(hotkey);
            if (typeOfHotKey == "") return;

            if (typeOfHotKey == "refresh")
            {
                refreshModifiers = mods;
            }
            else if (typeOfHotKey == "toggle")
            {
                toggleModifiers = mods;
            }
            else if (typeOfHotKey == "stashtab")
            {
                stashTabModifiers = mods;
            }
            RequiresModifierKey = true;
        }

        private static string GetHotkeyType(string hotkey)
        {
            if (hotkey.Equals(Settings.Default.HotkeyRefresh))
            {
                return "refresh";
            }
            else if (hotkey.Equals(Settings.Default.HotkeyToggle))
            {
                return "toggle";
            }
            else if (hotkey.Equals(Settings.Default.HotkeyStashTab))
            {
                return "stashtab";
            }
            return "";
        }

        private static void SetKey(string hotkey)
        {
            string[] split_string = hotkey.Split('+');
            string last_element = split_string[split_string.Length - 1].Trim();
            string typeOfHotKey = GetHotkeyType(hotkey);

            if (typeOfHotKey == "refresh")
            {
                Enum.TryParse(last_element, out refreshKey);
            }
            else if (typeOfHotKey == "toggle")
            {
                Enum.TryParse(last_element, out toggleKey);
            }
            else if (typeOfHotKey == "stashtab")
            {
                Enum.TryParse(last_element, out stashTabKey);
            }
        }


/*        public static void GetToggleHotkey()
        {
            if (Settings.Default.HotkeyToggle != "< not set >")
            {
                var toggleString = Settings.Default.HotkeyToggle.Split('+');
                toggleModifiers = new List<ModifierKeys>();

                if (toggleString.Length > 1 && toggleString.Length < 3)
                {
                    if (toggleString[0].Trim() == "Ctrl")
                        toggleModifier = ModifierKeys.Control;
                    else if (toggleString[0].Trim() == "Alt")
                        toggleModifier = ModifierKeys.Alt;
                    else if (toggleString[0].Trim() == "Win")
                        toggleModifier = ModifierKeys.Windows;
                    else if (toggleString[0].Trim() == "Shift")
                        toggleModifier = ModifierKeys.Shift;
                    else
                        toggleModifier = ModifierKeys.None;

                    Enum.TryParse(toggleString[1].Trim(), out toggleKey);
                }
                else if (toggleString.Length > 2)
                {
                    
                    for (int i = 0; i < toggleString.Length; i++)
                    {
                        if (toggleString[i].Trim().ToLower().Contains("alt"))
                        {
                            toggleModifiers.Add(ModifierKeys.Alt);
                        }
                        else if (toggleString[i].Trim().ToLower().Contains("ctrl"))
                        {
                            toggleModifiers.Add(ModifierKeys.Control);
                        }
                        else if (toggleString[i].Trim().ToLower().Contains("shift"))
                        {
                            toggleModifiers.Add(ModifierKeys.Shift);
                        }
                        else if (toggleString[i].Trim().ToLower().Contains("win"))
                        {
                            toggleModifiers.Add(ModifierKeys.Windows);
                        }
                    }

                    Enum.TryParse(toggleString[toggleString.Length - 1], out toggleKey);
                }
                else
                {
                    Enum.TryParse(toggleString[0].Trim(), out toggleKey);
                    toggleModifier = ModifierKeys.None;
                }
            }
        }*/

/*        public static void GetStashTabHotkey()
        {
            if (Settings.Default.HotkeyStashTab != "< not set >")
            {
                var stashTabString = Settings.Default.HotkeyStashTab.Split('+');

                if (stashTabString.Length > 1)
                {
                    if (stashTabString[0].Trim() == "Ctrl")
                        stashTabModifier = ModifierKeys.Control;
                    else if (stashTabString[0].Trim() == "Alt")
                        stashTabModifier = ModifierKeys.Alt;
                    else if (stashTabString[0].Trim() == "Win")
                        stashTabModifier = ModifierKeys.Windows;
                    else if (stashTabString[0].Trim() == "Shift")
                        stashTabModifier = ModifierKeys.Shift;
                    else
                        stashTabModifier = ModifierKeys.None;

                    Enum.TryParse(stashTabString[1].Trim(), out stashTabKey);
                }
                else
                {
                    Enum.TryParse(stashTabString[0].Trim(), out stashTabKey);
                    stashTabModifier = ModifierKeys.None;
                }
            }
        }*/

        //public static void GetReloadFilterHotkey()
        //{
        //    if (Properties.Settings.Default.HotkeyReloadFilter != "< not set >")
        //    {
        //        string[] reloadFilterString = Properties.Settings.Default.HotkeyReloadFilter.Split('+');

        //        if (reloadFilterString.Length > 1)
        //        {
        //            if (reloadFilterString[0].Trim() == "Ctrl")
        //            {
        //                reloadFilterModifier = ModifierKeys.Control;
        //            }
        //            else if (reloadFilterString[0].Trim() == "Alt")
        //            {
        //                reloadFilterModifier = ModifierKeys.Alt;
        //            }
        //            else if (reloadFilterString[0].Trim() == "Win")
        //            {
        //                reloadFilterModifier = ModifierKeys.Windows;
        //            }
        //            else if (reloadFilterString[0].Trim() == "Shift")
        //            {
        //                reloadFilterModifier = ModifierKeys.Shift;
        //            }
        //            else
        //            {
        //                reloadFilterModifier = ModifierKeys.None;
        //            }

        //            Enum.TryParse(reloadFilterString[1].Trim(), out reloadFilterKey);
        //        }
        //        else
        //        {
        //            Enum.TryParse(reloadFilterString[0].Trim(), out reloadFilterKey);
        //            reloadFilterModifier = ModifierKeys.None;
        //        }
        //    }
        //}

        public static void RemoveToggleHotkey()
        {
            if(toggleModifier != ModifierKeys.None)
            {
                RemoveHotkey(toggleModifier, toggleKey);
            }
            else if(toggleModifiers.Count > 1)
            {
                RemoveHotkey(toggleModifiers, toggleKey);
            }
        }

        public static void RemoveStashTabHotkey()
        {
            RemoveHotkey(stashTabModifier, stashTabKey);
        }

        public static void RemoveRefreshHotkey()
        {
            RemoveHotkey(refreshModifier, refreshKey);
        }

        // Callbacks

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        //public static void RemoveReloadFilterHotkey()
        //{
        //    HotkeysManager.RemoveHotkey(reloadFilterModifier, reloadFilterKey);
        //}

        #region Native Methods

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}