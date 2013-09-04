using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Auremo
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private MainWindow m_Parent = null;

        public AboutWindow()
        {
            InitializeComponent();
        }

        public AboutWindow(MainWindow parent)
        {
            InitializeComponent();

            m_Parent = parent;
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            m_VersionNumber.Text = "Version " + ver.Major + "." + ver.Minor + "." + ver.Build;
                
                //.GetExecutingAssembly().ImageRuntimeVersion;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_Parent.OnChildWindowClosing(this);
        }
    }
}
