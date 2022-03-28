using System;
using System.Linq;
using System.Windows;
using Slider = System.Windows.Controls.Slider;
using CSCore.CoreAudioAPI;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using PiP_Tool.DataModel;
using PiP_Tool.Interfaces;
using PiP_Tool.Shared;
using System.Windows.Input;
using PiP_Tool.Classes;
using PiP_Tool.Native;

namespace PiP_Tool.ViewModels
{
    public class OpacityViewModel : ViewModelBase, ICloseable, IDisposable
    {

        private double opacity = 1;
        public double Opacity
        {
            get => opacity;
            set
            {
                opacity = value;
                RaisePropertyChanged();
                //NotifyPropertyChanged("Opacity");
                SetOpacity(value);
            }
        }

        private PiPWindowInfo mainWindow;
        private Window thisWindow;
        private Slider opacitySlider;
        public ICommand LoadedCommand { get; }
        //public ICommand SliderChangedCommand { get; }

        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int LWA_ALPHA = 0x2;
        const int LWA_COLORKEY = 0x1;
        uint crKey = 0;
        byte bAlpha = 255;
        uint dwFlags = LWA_ALPHA;

        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public OpacityViewModel()
        {
            Logger.Instance.Debug("   ====== OpacityDialog ======   ");

            MessengerInstance.Register<PiPWindowInfo>(this, InitWindow);
            LoadedCommand = new RelayCommand(LoadedCommandExecute);
            //SliderChangedCommand = new RelayCommand(SliderChangedCommandExecute);
        }

        /// <summary>
        /// Executed when the window is loaded. Get handle of the window and call <see cref="InitDwmThumbnail"/> 
        /// </summary>
        private void LoadedCommandExecute()
        {
            thisWindow = ThisWindow();
            opacitySlider = (Slider)thisWindow.FindName("OpacitySlider");

            InitOpacitySlider();
        }

        /// <summary>
        /// Gets this window
        /// </summary>
        /// <returns>This window</returns>
        private Window ThisWindow()
        {
            var windowsList = Application.Current.Windows.Cast<Window>();
            return windowsList.FirstOrDefault(window => window.DataContext == this);
        }

        /// <summary>
        /// Set selected region' data. Set position and size of this window
        /// </summary>
        /// <param name="audioControls">Audio controls of the selected window</param>
        private void InitWindow(PiPWindowInfo mainWindow)
        {
            if (mainWindow == null)
            {
                Logger.Instance.Error("Can't init main window");
                return;
            }
            this.mainWindow = mainWindow;
            MessengerInstance.Unregister<PiPWindowInfo>(this);

            InitOpacitySlider();
        }
        private void InitOpacitySlider()
        {
            if (this.mainWindow == null || this.opacitySlider == null)
                return;
            // NativeMethods.GetLayeredWindowAttributes(this.mainWindow.Handle, out crKey, out bAlpha, out dwFlags);
            Logger.Instance.Debug("Init Opacity Slider " + bAlpha);
            //this.Opacity = ByteToDouble(bAlpha);
            this.Opacity = this.mainWindow.ViewModel.Opacity;
        }

        private byte DoubleToByte(double x)
        {
            return (byte)(255 * x);
        }

        private double ByteToDouble(byte x)
        {
            return x / 255.0;
        }

        public event EventHandler<EventArgs> RequestClose;

        /// <inheritdoc />
        /// <summary>
        /// Remove DragHook
        /// </summary>
        public void Dispose()
        {
            this.mainWindow.Dispose();
        }

        /// <summary>
        /// Executed on click on close button. Close this window
        /// </summary>
        private void CloseCommandExecute()
        {
            MessengerInstance.Unregister<PiPWindowInfo>(this);
        }

        private void SetOpacity(double opacity)
        {
            Logger.Instance.Debug("Setting opacity to " + opacity);
            this.SetOpacity(DoubleToByte(opacity));
        }
        private void SetOpacity(byte opacity)
        {
            Logger.Instance.Debug("Setting opacity to " + opacity);

            this.mainWindow.ViewModel.Opacity = ByteToDouble(opacity);

            /*
            NativeMethods.SetWindowLong(
                this.mainWindow.Handle,
                GWL_EXSTYLE,
                (uint)NativeMethods.GetWindowLong(this.mainWindow.Handle, GWL_EXSTYLE) ^ WS_EX_LAYERED
            );
            
            NativeMethods.SetLayeredWindowAttributes(
                this.mainWindow.Handle,
                0,
                opacity,
                LWA_ALPHA
            );
            */
        }

        private void SetSliderValue(byte value)
        {
            SetSliderValue(ByteToDouble(value));
        }

        private void SetSliderValue(double value)
        {
            if (opacitySlider.Value != value)
                opacitySlider.Value = value;
        }

        public void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var value = e.NewValue;
            Logger.Instance.Debug("Slider changed to " + value);
            if (opacity != value)
                opacity = value;
            SetOpacity(value);
        }
    }
}
