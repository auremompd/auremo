/*
 * Copyright 2013 Mikko Teräs and Niilo Säämänen.
 *
 * This file is part of Auremo.
 *
 * Auremo is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, version 2.
 *
 * Auremo is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Auremo. If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Auremo.Properties;

namespace Auremo
{
    public partial class SettingsWindow : Window
    {
        MainWindow m_Parent = null;

        public SettingsWindow(MainWindow parent)
        {
            InitializeComponent();

            m_Parent = parent;
            LoadSettings();
        }

        private void OnNumericOptionPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private void ValidateOptions(object sender, RoutedEventArgs e)
        {
            ClampTextBoxContent(m_PortEntry, 1, 6600, 65536);
            ClampTextBoxContent(m_UpdateIntervalEntry, 100, 500, 5000);
            ClampTextBoxContent(m_ReconnectIntervalEntry, 0, 10, 3600);
            ClampTextBoxContent(m_WheelVolumeStepEntry, 0, 5, 100);
            ClampTextBoxContent(m_WheelSongPositioningPercentEntry, 0, 5, 100);
            ClampTextBoxContent(m_WheelSongPositioningSecondsEntry, 0, 5, 1800);
        }

        private void ClampTextBoxContent(object control, int min, int dfault, int max)
        {
            TextBox textBox = control as TextBox;
            int value = Utils.StringToInt(textBox.Text, dfault);
            textBox.Text = Utils.Clamp(min, value, max).ToString();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            Close();
        }

        private void OnRevertClicked(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void OnApplyClicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void OnOKClicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void LoadSettings()
        {
            m_ServerEntry.Text = Settings.Default.Server;
            m_PortEntry.Text = Settings.Default.Port.ToString();
            m_PasswordEntry.Password = Crypto.DecryptPassword(Settings.Default.Password);
            m_UpdateIntervalEntry.Text = Settings.Default.ViewUpdateInterval.ToString();
            m_EnableVolumeControl.IsChecked = Settings.Default.EnableVolumeControl;
            m_WheelVolumeStepEntry.Text = Settings.Default.MouseWheelAdjustsVolumeBy.ToString();
            m_WheelSongPositioningModeIsPercent.IsChecked = Settings.Default.MouseWheelAdjustsSongPositionInPercent;
            m_WheelSongPositioningModeIsSeconds.IsChecked = !m_WheelSongPositioningModeIsPercent.IsChecked;
            m_WheelSongPositioningPercentEntry.Text = Settings.Default.MouseWheelAdjustsSongPositionPercentBy.ToString();
            m_WheelSongPositioningSecondsEntry.Text = Settings.Default.MouseWheelAdjustsSongPositionSecondsBy.ToString();
        }

        private void SaveSettings()
        {
            int port = Utils.StringToInt(m_PortEntry.Text, 6600);
            string password = Crypto.EncryptPassword(m_PasswordEntry.Password);

            bool reconnectNeeded =
                m_ServerEntry.Text != Settings.Default.Server ||
                port != Settings.Default.Port ||
                password != Settings.Default.Password;

            Settings.Default.Server = m_ServerEntry.Text;
            Settings.Default.Port = port;
            Settings.Default.Password = password;
            Settings.Default.ViewUpdateInterval = Utils.StringToInt(m_UpdateIntervalEntry.Text, 500);
            Settings.Default.EnableVolumeControl = m_EnableVolumeControl.IsChecked == null || m_EnableVolumeControl.IsChecked.Value;
            Settings.Default.MouseWheelAdjustsVolumeBy = Utils.StringToInt(m_WheelVolumeStepEntry.Text, 5);
            Settings.Default.MouseWheelAdjustsSongPositionInPercent = m_WheelSongPositioningModeIsPercent.IsChecked.Value;
            Settings.Default.MouseWheelAdjustsSongPositionPercentBy = Utils.StringToInt(m_WheelSongPositioningPercentEntry.Text, 5);
            Settings.Default.MouseWheelAdjustsSongPositionSecondsBy = Utils.StringToInt(m_WheelSongPositioningSecondsEntry.Text, 5);

            Settings.Default.Save();

            m_Parent.SettingsChanged(reconnectNeeded);
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_Parent.OnChildWindowClosing(this);
        }
    }
}
