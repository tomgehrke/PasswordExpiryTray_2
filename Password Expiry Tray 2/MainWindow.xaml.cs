using ActiveDirectoryUtilityLibrary;

using System;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace Password_Expiry_Tray_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon MainNotifyIcon = new NotifyIcon();
        private Timer CheckExpirationTimer = new Timer();
        internal ContextMenuStrip notificationContextMenuStrip = new ContextMenuStrip();

        // Application settings object
        private Settings AppSettings = new Settings();

        // Identity objects
        private ActiveDirectoryUser currentActiveDirectoryUser = new ActiveDirectoryUser();
        private WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();

        // Current alert states
        private DateTime LastNotified;
        private Priority CurrentPriority = Priority.Unknown;

        // Alert priorities
        private enum Priority
        {
            Unknown, None, Warn, Alert
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotificationIcon();
            InitializeTimer();

            string[] userNameParts = currentIdentity.Name.Split('\\');
            currentActiveDirectoryUser.Domain = userNameParts[0];
            currentActiveDirectoryUser.UserName = userNameParts[1];
        }

        private void ExitApplication()
        {
            MainNotifyIcon.Visible = false;
            Close();
        }

        private void RefreshSettings()
        {
            AppSettings.Load();
            CheckExpirationTimer.Interval = AppSettings.TimerInterval * 60000;
        }

        private void RefreshMainWindow()
        {
            DateTime currentTime = DateTime.Now;
            DateTime lastChecked = currentTime;
            bool priorityChanged = false;

            // Get current user info
            currentActiveDirectoryUser.Update();

            // Create "report"
            AlertTextBlock.Text = String.Format("{0:dddd, MMMM dd, yyyy}", currentActiveDirectoryUser.PasswordExpirationDate);

            StringBuilder messageStringBuilder = new StringBuilder();
            StringBuilder tooltipStringBuilder = new StringBuilder();

            messageStringBuilder.AppendFormat("{0} ({1}), you are logged in with a {2} account.\n\n", currentIdentity.Name, currentActiveDirectoryUser.FullName, currentActiveDirectoryUser.Context);

            if (currentActiveDirectoryUser.PasswordRequired)
            {
                if (currentActiveDirectoryUser.PasswordNeverExpires)
                {
                    CurrentPriority = Priority.Unknown;
                    AlertTextBlock.Text = "Never!";
                    messageStringBuilder.Append("Your password never expires.\n\n");
                    tooltipStringBuilder.AppendFormat("Your {0} account password never expires", currentActiveDirectoryUser.Context);
                }
                else
                {
                    Priority originalPriority = CurrentPriority;
                    CurrentPriority = GetCurrentPriority(currentTime);
                    priorityChanged = !(originalPriority == CurrentPriority);
                    messageStringBuilder.AppendFormat("Your password was last changed on {0:d} at {0:t}. You have {1} days until it will need to be changed.\n\n", currentActiveDirectoryUser.PasswordLastChangedDate, (currentActiveDirectoryUser.PasswordExpirationDate - currentTime).Days);
                    tooltipStringBuilder.AppendFormat("Your password expires in {0} days on {1:d}", (currentActiveDirectoryUser.PasswordExpirationDate - currentTime).Days, currentActiveDirectoryUser.PasswordExpirationDate);
                }
            }
            else
            {
                CurrentPriority = Priority.Unknown;
                messageStringBuilder.Append("You do not required a password.");
                tooltipStringBuilder.Append("You do not required a password");
            }

            messageStringBuilder.AppendFormat("Password last checked on {0:d} at {0:t}", lastChecked);

            MessageTextBlock.Text = messageStringBuilder.ToString();

            switch (CurrentPriority)
            {
                case Priority.None:
                    AlertTextBlock.Background = Brushes.Green;
                    AlertTextBlock.Foreground = Brushes.LightGray;
                    MainNotifyIcon.Icon = Properties.Resources.pWhite;
                    break;
                case Priority.Warn:
                    AlertTextBlock.Background = Brushes.Yellow;
                    AlertTextBlock.Foreground = Brushes.DimGray;
                    MainNotifyIcon.Icon = Properties.Resources.pYellow;
                    break;
                case Priority.Alert:
                    AlertTextBlock.Background = Brushes.Red;
                    AlertTextBlock.Foreground = Brushes.LightGray;
                    MainNotifyIcon.Icon = Properties.Resources.pRed;
                    break;
                default:
                    AlertTextBlock.Background = Brushes.DarkGray;
                    AlertTextBlock.Foreground = Brushes.LightGray;
                    MainNotifyIcon.Icon = Properties.Resources.pBlack;
                    break;
            }

            // Tooltips cannot be longer than 64 characters or an exception will be thrown
            MainNotifyIcon.Text = tooltipStringBuilder.ToString();

            // Update settings related components
            if (String.IsNullOrEmpty(AppSettings.Action))
            {
                ActionButton.Visibility = Visibility.Hidden;
            }
            else
            {
                ActionButton.Visibility = Visibility.Visible;
            }

            // Check notification status
            if (priorityChanged || NotificationRequired(currentTime))
            {
                ShowNotification();
                LastNotified = currentTime;
            }
        }

        private void ShowNotification()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void HideNotification()
        {
            this.Hide();
            this.WindowState = WindowState.Minimized;
        }

        private Priority GetCurrentPriority(DateTime currentTime)
        {
            Priority priority = Priority.None;

            if ((currentActiveDirectoryUser.PasswordExpirationDate - currentTime).Days <= AppSettings.WarnThreshold)
            {
                priority = Priority.Warn;
                if ((currentActiveDirectoryUser.PasswordExpirationDate - currentTime).Days <= AppSettings.AlertThreshold)
                {
                    priority = Priority.Alert;
                }
            }

            return priority;
        }

        private bool NotificationRequired(DateTime currentTime)
        {
            bool notificationRequired = false;

            switch (CurrentPriority)
            {
                case Priority.Unknown:
                    notificationRequired = false;
                    break;
                case Priority.None:
                    notificationRequired = false;
                    break;
                case Priority.Warn:
                    if ((LastNotified - currentTime).Hours >= AppSettings.WarnThreshold) { notificationRequired = true; }
                    break;
                case Priority.Alert:
                    if ((LastNotified - currentTime).Hours >= AppSettings.AlertInterval) { notificationRequired = true; }
                    break;
                default:
                    break;
            }

            return notificationRequired;
        }

        #region CUSTOM INITIALIZATION

        // Prepare the notification icon
        private void InitializeNotificationIcon()
        {
            MainNotifyIcon.Text = "Password Expiry Tray";
            MainNotifyIcon.Visible = true;
            MainNotifyIcon.Icon = Properties.Resources.pBlack;

            // Add Menu Items to Context Menu
            ToolStripItem UpdateNowItem = notificationContextMenuStrip.Items.Add("Update Now");
            ToolStripItem ShowNotificationItem = notificationContextMenuStrip.Items.Add("Show Notification");
            notificationContextMenuStrip.Items.Add(new ToolStripSeparator());
            ToolStripItem SettingsItem = notificationContextMenuStrip.Items.Add("Settings...");
            notificationContextMenuStrip.Items.Add(new ToolStripSeparator());
            ToolStripItem ExitItem = notificationContextMenuStrip.Items.Add("Exit");

            // Add event handlers
            UpdateNowItem.Click += new EventHandler(UpdateNowMenuItem_Click);
            ShowNotificationItem.Click += new EventHandler(ShowNotificationItem_Click);
            SettingsItem.Click += new EventHandler(SettingsMenuItem_Click);
            ExitItem.Click += new EventHandler(ExitMenuItem_Click);

            // Add Context Menu to the Notification Icon
            MainNotifyIcon.ContextMenuStrip = notificationContextMenuStrip;

            // Add event handler
            MainNotifyIcon.DoubleClick += new EventHandler(MainNotifyIcon_DoubleClick);
        }

        private void InitializeTimer()
        {
            // Add event handler
            CheckExpirationTimer.Tick += new EventHandler(CheckExpirationTimer_Tick);
            CheckExpirationTimer.Enabled = true;
            CheckExpirationTimer.Start();
        }

        #endregion

        #region EVENT HANDLERS

        private void UpdateNowMenuItem_Click(object sender, EventArgs e)
        {
            RefreshMainWindow();
        }

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            Window settingsWindow = new SettingsWindow();
            notificationContextMenuStrip.Enabled = false;
            settingsWindow.ShowDialog();
            notificationContextMenuStrip.Enabled = true;
            RefreshSettings();
            RefreshMainWindow();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e) => ExitApplication();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => HideNotification();

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshSettings();
            RefreshMainWindow();

            this.Hide();
        }

        private void MainNotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Hidden)
            {
                ShowNotification();
            }
            else
            {
                HideNotification();
            }
        }

        private void CheckExpirationTimer_Tick(object sender, EventArgs e)
        {
            RefreshMainWindow();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(AppSettings.Action);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "PET Error!");
            }
        }

        private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void ShowNotificationItem_Click(object sender, EventArgs e)
        {
            ShowNotification();
        }

        #endregion

    }
}
