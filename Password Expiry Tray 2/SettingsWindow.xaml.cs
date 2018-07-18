using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Password_Expiry_Tray_2
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private Settings AppSettings = new Settings();

        public SettingsWindow()
        {
            InitializeComponent();
            RefreshWindow();

            if (AppSettings.Source != Settings.SettingSource.Local)
            {
                TimerIntervalTextBox.IsEnabled = false;
                WarningIntervalTextBox.IsEnabled = false;
                WarningThresholdTextBox.IsEnabled = false;
                AlertIntervalTextBox.IsEnabled = false;
                AlertThresholdTextBox.IsEnabled = false;
                ActionTextBox.IsEnabled = false;
                SaveButton.IsEnabled = false;
            }
        }

        private void RefreshWindow()
        {
            TimerIntervalTextBox.Text = AppSettings.TimerInterval.ToString();
            WarningIntervalTextBox.Text = AppSettings.WarnInterval.ToString();
            WarningThresholdTextBox.Text = AppSettings.WarnThreshold.ToString();
            AlertIntervalTextBox.Text = AppSettings.AlertInterval.ToString();
            AlertThresholdTextBox.Text = AppSettings.AlertThreshold.ToString();
            ActionTextBox.Text = AppSettings.Action;
        }

        #region Event Handlers

        private void SaveButton_Click(object sender, EventArgs e)
        {
            AppSettings.TimerInterval = int.Parse(TimerIntervalTextBox.Text);
            AppSettings.WarnInterval = int.Parse(WarningIntervalTextBox.Text);
            AppSettings.WarnThreshold = int.Parse(WarningThresholdTextBox.Text);
            AppSettings.AlertInterval = int.Parse(AlertIntervalTextBox.Text);
            AppSettings.AlertThreshold = int.Parse(AlertThresholdTextBox.Text);
            AppSettings.Action = ActionTextBox.Text;

            AppSettings.Save();
            if (String.IsNullOrEmpty(AppSettings.ErrorMessage))
            {
                MessageBox.Show("Settings saved!");
            }
            else
            {
                MessageBox.Show(AppSettings.ErrorMessage);
                AppSettings.ClearErrorMessage();
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void TimerIntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex isNumericRegex = new Regex("[^0-9]+");
            e.Handled = isNumericRegex.IsMatch(e.Text);
        }

        private void WarningIntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex isNumericRegex = new Regex("[^0-9]+");
            e.Handled = isNumericRegex.IsMatch(e.Text);
        }

        private void WarningThresholdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex isNumericRegex = new Regex("[^0-9]+");
            e.Handled = isNumericRegex.IsMatch(e.Text);
        }

        private void AlertIntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex isNumericRegex = new Regex("[^0-9]+");
            e.Handled = isNumericRegex.IsMatch(e.Text);
        }

        private void AlertThresholdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex isNumericRegex = new Regex("[^0-9]+");
            e.Handled = isNumericRegex.IsMatch(e.Text);
        }

        private void DefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.LoadDefaults();
            RefreshWindow();
        }

        #endregion
    }
}
