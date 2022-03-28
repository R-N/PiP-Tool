
using PiP_Tool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace PiP_Tool.DataModel
{
    internal class PiPWindowInfo : IDisposable
    {
        public IntPtr Handle { get; private set; }
        public PiPModeViewModel ViewModel { get; private set; }
        public Window Window { get; private set; }

        public PiPWindowInfo(PiPModeViewModel viewModel)
        {
            this.ViewModel = viewModel;
            this.Window = viewModel.ThisWindow();
            this.Handle = new WindowInteropHelper(Window).Handle;
        }

        public void Dispose()
        {
        }
    }
}
