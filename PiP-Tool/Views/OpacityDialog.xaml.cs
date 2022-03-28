
using System;
using System.Windows;

namespace PiP_Tool.Views
{
    /// <summary>
    /// Interaction logic for VolumeDialog.xaml
    /// </summary>
    public partial class OpacityDialog : Window
    {
        public OpacityDialog()
        {
            InitializeComponent();
        }
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Close();
        }
    }
}
