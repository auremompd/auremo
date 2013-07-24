using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Auremo
{
    // A standard button with an extra property "IsDown" for buttons that have a
    // server-dependent glow state.
    public class StickyButton : Button
    {
        public bool IsDown
        {
            get
            {
                return (bool)GetValue(IsDownProperty);
            }
            set
            {
                SetValue(IsDownProperty, value);
            }
        }

        public static readonly DependencyProperty IsDownProperty = DependencyProperty.Register("IsDown", typeof(bool), typeof(StickyButton), new UIPropertyMetadata(false));
    }
}
