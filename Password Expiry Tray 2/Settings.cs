using Microsoft.Win32;

using System;

namespace Password_Expiry_Tray_2
{
    public class Settings
    {
        public enum SettingSource { Local = 0, UserPolicy, ComputerPolicy }
        public enum Setting { All = 0, Action, AlertInterval, AlertThreshold, TimerInterval, WarnInterval, WarnThreshold }

        public int TimerInterval { get; set; }
        public int WarnThreshold { get; set; }
        public int WarnInterval { get; set; }
        public int AlertThreshold { get; set; }
        public int AlertInterval { get; set; }
        public string Action { get; set; }
        public SettingSource Source { get; set; }
        public string ErrorMessage { get; private set; } = "";

        private const string PolicySettingsSubKey = "Software\\Policies\\PasswordExpiryTray";
        private const string LocalSettingsSubKey = "Software\\PasswordExpiryTray";

        public Settings()
        {
            Load();
        }

        public void Load()
        {
            ClearErrorMessage();

            RegistryKey localMachineRegistryKey = Registry.LocalMachine;
            RegistryKey computerPolicyRegistryKey = localMachineRegistryKey.OpenSubKey(PolicySettingsSubKey, false);
            if (computerPolicyRegistryKey != null) // Take settings from Computer policy
            {
                Source = SettingSource.ComputerPolicy;
                GetRegistrySettings(computerPolicyRegistryKey);
                computerPolicyRegistryKey.Close();
                localMachineRegistryKey.Close();
            }
            else
            {
                RegistryKey currentUserRegistryKey = Registry.CurrentUser;
                RegistryKey userPolicyRegistryKey = currentUserRegistryKey.OpenSubKey(PolicySettingsSubKey, false);
                if (userPolicyRegistryKey != null) // Take settings from User policy
                {
                    Source = SettingSource.UserPolicy;
                    GetRegistrySettings(userPolicyRegistryKey);
                    userPolicyRegistryKey.Close();
                }
                else
                {
                    RegistryKey localUserSettings = currentUserRegistryKey.OpenSubKey(LocalSettingsSubKey, false);
                    if (localUserSettings != null) // Take settings from default app location
                    {
                        Source = SettingSource.Local;
                        GetRegistrySettings(localUserSettings);
                        localUserSettings.Close();
                    }
                    else
                    {
                        LoadDefaults();
                        ClearErrorMessage();
                        SetRegistrySettings();
                    }
                }
                currentUserRegistryKey.Close();
            }
        }

        internal void ClearErrorMessage()
        {
            ErrorMessage = "";
        }

        public void Save()
        {
            if (Source == SettingSource.Local)
            {
                SetRegistrySettings();
            }
            else
            {
                ErrorMessage = "Settings are set by policy and cannot be changed by the user.";
            }
        }

        private void GetRegistrySettings(RegistryKey registryKey)
        {
            bool fixRequired = false;

            try
            {
                //Read values from registry
                TimerInterval = Convert.ToInt16(registryKey.GetValue("TimerInterval"));
                WarnInterval = Convert.ToInt16(registryKey.GetValue("WarnInterval"));
                WarnThreshold = Convert.ToInt16(registryKey.GetValue("WarnThreshold"));
                AlertInterval = Convert.ToInt16(registryKey.GetValue("AlertInterval"));
                AlertThreshold = Convert.ToInt16(registryKey.GetValue("AlertThreshold"));
                Action = Convert.ToString(registryKey.GetValue("Action"));

                // Check & fix values just in case someone got cute with the registry
                if (TimerInterval < 1) { LoadDefaults(Setting.TimerInterval); fixRequired = true; }
                if (AlertThreshold < 1) { LoadDefaults(Setting.AlertThreshold); fixRequired = true; }
                if (AlertInterval < 1) { LoadDefaults(Setting.AlertInterval); fixRequired = true; }
                if (WarnThreshold < 1) { LoadDefaults(Setting.WarnThreshold); fixRequired = true; }
                if (WarnInterval < 1) { LoadDefaults(Setting.WarnInterval); fixRequired = true; }
            }
            catch (Exception)
            {
                throw;
            }

            // Update the registry with fixed values
            // Note: Can only fix local settings. Values pushed by policy will need an AD administrator to address.
            if (String.IsNullOrEmpty(ErrorMessage) && fixRequired && Source == SettingSource.Local)
            {
                SetRegistrySettings();
            }
        }

        private void SetRegistrySettings()
        {
            try
            {
                RegistryKey currentUserKey = Registry.CurrentUser;
                RegistryKey petKey = currentUserKey.CreateSubKey(LocalSettingsSubKey);

                petKey.SetValue("TimerInterval", TimerInterval.ToString());
                petKey.SetValue("WarnInterval", WarnInterval.ToString());
                petKey.SetValue("WarnThreshold", WarnThreshold.ToString());
                petKey.SetValue("AlertInterval", AlertInterval.ToString());
                petKey.SetValue("AlertThreshold", AlertThreshold.ToString());
                petKey.SetValue("Action", Action);

                petKey.Close();
                currentUserKey.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void LoadDefaults()
        {
            LoadDefaults(Setting.All);
        }

        public void LoadDefaults(Setting setting)
        {
            if (setting == Setting.All || setting == Setting.TimerInterval) { TimerInterval = 60; }
            if (setting == Setting.All || setting == Setting.WarnInterval) { WarnInterval = 24; }
            if (setting == Setting.All || setting == Setting.WarnThreshold) { WarnThreshold = 21; }
            if (setting == Setting.All || setting == Setting.AlertInterval) { AlertInterval = 4; }
            if (setting == Setting.All || setting == Setting.AlertThreshold) { AlertThreshold = 7; }
            if (setting == Setting.All || setting == Setting.Action) { Action = ""; }
        }
    }
}
