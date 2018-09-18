﻿using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PCSX2_Configurator_Next.WpfExtensions;
using Unbroken.LaunchBox.Plugins.Data;

namespace PCSX2_Configurator_Next
{
    /// <inheritdoc cref="Window" />
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow
    {
        private readonly IGame _selectedGame;
        private readonly Task<string> _selectedGameRemoteConfigPathTask;

        public ConfigWindow(IGame selectedGame = null)
        {
            _selectedGame = selectedGame;
            _selectedGameRemoteConfigPathTask = Task.Run(() => GameHelper.GetRemoteConfigPath(_selectedGame));
            InitializeComponent();
            InitializeConfigWindow();
            SetupEvents();
        }

        private void InitializeConfigWindow()
        {
            ((OutlinedTextBlock) ConfiguredLbl.Content).Text = "[Game Name]: [Configured]";
            ((OutlinedTextBlock) ConfiguredLbl.Content).Text = ((OutlinedTextBlock) ConfiguredLbl.Content).Text.Replace("[Game Name]", _selectedGame.Title);
            ((OutlinedTextBlock) ConfiguredLbl.Content).Text = GameHelper.IsGameConfigured(_selectedGame)
                ? ((OutlinedTextBlock) ConfiguredLbl.Content).Text.Replace("[Configured]", "Configured")
                : ((OutlinedTextBlock) ConfiguredLbl.Content).Text.Replace("[Configured]", "Not Configured");

            ((OutlinedTextBlock) DownloadConfigBtn.Content).Text = "[Download] Config";
            ((OutlinedTextBlock) DownloadConfigBtn.Content).Text = GameHelper.IsGameUsingRemoteConfig(_selectedGame)
                ? ((OutlinedTextBlock) DownloadConfigBtn.Content).Text.Replace("[Download]", "Update")
                : ((OutlinedTextBlock) DownloadConfigBtn.Content).Text.Replace("[Download]", "Download");

            DisableControl(DownloadConfigBtn);
            _selectedGameRemoteConfigPathTask.ContinueWith(remoteConfigPath =>
            {
                if (!GameHelper.IsGameUsingRemoteConfig(_selectedGame))
                {
                    if (remoteConfigPath.Result != null)
                    {
                        Dispatcher.Invoke(() => EnableControl(DownloadConfigBtn));
                    }
                }
                else
                {
                    if (Configurator.CheckForConfigUpdates(remoteConfigPath.Result))
                    {
                        Dispatcher.Invoke(() => EnableControl(DownloadConfigBtn));
                    }
                }
            });

            if (!GameHelper.IsGameConfigured(_selectedGame))
            {
                DisableControl(RemoveConfigBtn);
                DisableControl(Pcsx2Btn);
            }
            else
            {
                EnableControl(RemoveConfigBtn);
                EnableControl(Pcsx2Btn);
            }
        }

        private static void DisableControl(ContentControl control)
        {
            control.Cursor = null;
            control.IsEnabled = false;
            control.Effect = null;
            ((OutlinedTextBlock) control.Content).Stroke = new SolidColorBrush(Color.FromRgb(0, 27, 115));
            
        }

        private static void EnableControl(ContentControl control)
        {
            control.Cursor = Cursors.Hand;
            control.IsEnabled = true;
            control.Effect = new DropShadowEffect() { Direction = 220, ShadowDepth = 3, Color =  Color.FromRgb(27, 39, 220) };
            ((OutlinedTextBlock)control.Content).Stroke = new SolidColorBrush(Color.FromRgb(3, 148, 255));
        }

        private void SetupEvents()
        {
            MouseDown += delegate { try { DragMove(); } catch { /*ignored*/ } };

            CreateConfigBtn.MouseLeftButtonDown += CreateConfigBtn_Click;
            DownloadConfigBtn.MouseLeftButtonDown += DownloadConfigBtn_Click;
            RemoveConfigBtn.MouseLeftButtonDown += RemoveConfigBtn_Click;
            Pcsx2Btn.MouseLeftButtonDown += Pcsx2Btn_Click;

            CloseBtn.MouseLeftButtonDown += delegate { Close(); };
        }

        private void CreateConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            var createConfig = true;
            if (GameHelper.IsGameConfigured(_selectedGame))
            {
                var msgResult = MessageDialog.Show(this, MessageDialog.Type.Generic, "This will overwrite...");

                if (msgResult != true)
                {
                    createConfig = false;
                }
            }

            if (!createConfig) return;
            Configurator.CreateConfig(_selectedGame);
            MessageDialog.Show(this, MessageDialog.Type.ConfigConfiguredSuccess);
            InitializeConfigWindow();
        }

        private void DownloadConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!GameHelper.IsGameUsingRemoteConfig(_selectedGame))
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var result = Configurator.DownloadConfig(_selectedGame, _selectedGameRemoteConfigPathTask.Result);
                Mouse.OverrideCursor = null;

                MessageDialog.Show(this,
                    result ? MessageDialog.Type.ConfigDownloadSuccess : MessageDialog.Type.ConfigDownloadError);
            }
            else
            {
                Mouse.OverrideCursor = Cursors.Wait;
                Configurator.UpdateGameConfig(_selectedGame, _selectedGameRemoteConfigPathTask.Result);
                Mouse.OverrideCursor = null;

                MessageDialog.Show(this, MessageDialog.Type.ConfigUpdateSuccess);
            }

            InitializeConfigWindow();
        }

        private void RemoveConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            var removeConfig = true;
            var msgResult = MessageDialog.Show(this, MessageDialog.Type.ConfigRemoveConfirm);

            if (msgResult != true)
            {
                removeConfig = false;
            }

            if (!removeConfig) return;
            Configurator.RemoveConfig(_selectedGame);
            InitializeConfigWindow();

            MessageDialog.Show(this, MessageDialog.Type.CongfigRemoveSuccess);
        }

        private void Pcsx2Btn_Click(object sender, RoutedEventArgs e)
        {
            _selectedGame.Configure();
            Close();
        }
    }
}
