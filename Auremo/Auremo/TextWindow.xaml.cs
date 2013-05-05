using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for TextWindow.xaml
    /// </summary>
    public partial class TextWindow : Window
    {
        private MainWindow m_Parent = null;

        public TextWindow(string title, string content, MainWindow parent)
        {
            InitializeComponent();
            Title = title;
            m_Content.Text = content;
            m_Parent = parent;
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
