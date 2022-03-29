using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Rectangle = System.Drawing.Rectangle;
using WPoint = System.Windows.Point;
using DPoint = System.Drawing.Point;
using Screen = System.Windows.Forms.Screen;
using Button = System.Windows.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Input;
using System.Windows.Interop;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using PiP_Tool.DataModel;
using PiP_Tool.Interfaces;
using PiP_Tool.MachineLearning;
using PiP_Tool.Native;
using PiP_Tool.Shared;
using PiP_Tool.Shared.Helpers;
using PiP_Tool.Views;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Drawing.Point;
using PiP_Tool.Classes;
using CSCore.CoreAudioAPI;
using System.Windows.Media;

namespace PiP_Tool.ViewModels
{
    public class PiPModeViewModel : ViewModelBase, ICloseable, IDisposable
    {

        #region public

        /// <summary>
        /// Gets or sets window title
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }

        public const int MinSize = 100;
        public const float DefaultSizePercentage = 0.25f;
        public const float DefaultPositionPercentage = 0.1f;
        public const int SideBarWidth = 30;

        private double opacity = 1;
        public double Opacity
        {
            get => opacity;
            set
            {
                opacity = value;
                RaisePropertyChanged();
                var bc = new BrushConverter();
                this.BackgroundBrush = (Brush)bc.ConvertFrom(this.opacity >= 1 ? "#FF2D2D30" : "#002D2D30");
                this.BackgroundOpacity = opacity < 1 ? 0.01 : 1;
                UpdateDwmThumbnail();
            }
        }

        private Brush backgroundBrush = (Brush)(new BrushConverter()).ConvertFrom("#FF2D2D30");
        public Brush BackgroundBrush
        {
            get => backgroundBrush;
            set
            {
                backgroundBrush = value;
                RaisePropertyChanged();
            }
        }

        private double backgroundOpacity = 1;
        public double BackgroundOpacity
        {
            get => backgroundOpacity;
            set
            {
                backgroundOpacity = value;
                RaisePropertyChanged();
            }
        }

        private bool forwardInputs = false;

        private bool mouseOver = false;

        public event EventHandler<EventArgs> RequestClose;

        public ICommand LoadedCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ClosingCommand { get; }
        public ICommand ChangeSelectedWindowCommand { get; }
        public ICommand SetVolumeCommand { get; }
        public ICommand SwitchToSelectedWindowCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand SetOpacityCommand { get; }
        public ICommand MouseEnterCommand { get; }
        public ICommand MouseMoveCommand { get; }
        public ICommand MouseDownCommand { get; }
        public ICommand MouseUpCommand { get; }
        public ICommand MouseLeaveCommand { get; }
        public ICommand ForwardInputsCommand { get; }
        public ICommand DpiChangedCommand { get; }

        /// <summary>
        /// Gets or sets min height property of the window
        /// </summary>
        public int MinHeight
        {
            get => _minHeight;
            set
            {
                _minHeight = value;
                UpdateDwmThumbnail();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets min width property of the window
        /// </summary>
        public int MinWidth
        {
            get => _minWidth;
            set
            {
                _minWidth = value;
                UpdateDwmThumbnail();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets top property of the window
        /// </summary>
        public int Top
        {
            get => _top;
            set
            {
                _top = value;
                UpdateDwmThumbnail();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets left property of the window
        /// </summary>
        public int Left
        {
            get => _left;
            set
            {
                _left = value;
                UpdateDwmThumbnail();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets height property of the window
        /// </summary>
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                UpdateDwmThumbnail();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets width property of the window
        /// </summary>
        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                UpdateDwmThumbnail();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets ratio of the pip region
        /// </summary>
        public float Ratio { get; private set; }
        /// <summary>
        /// Gets or sets visibility of the topbar
        /// </summary>
        public Visibility SideBarVisibility
        {
            get => _sideBarVisibility;
            set
            {
                _sideBarVisibility = value;
                UpdateDwmThumbnail();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets if topbar is visible
        /// </summary>
        public bool SideBarIsVisible => SideBarVisibility == Visibility.Visible;

        #endregion

        #region private

        private string _title;

        private float _dpiX = 1;
        private float _dpiY = 1;
        private int _widthOffset;
        private Visibility _sideBarVisibility;
        private bool _renderSizeEventDisabled;
        private int _minHeight;
        private int _minWidth;
        private int _top;
        private int _left;
        private int _height;
        private int _width;
        private IntPtr _targetHandle, _thumbHandle;
        private SelectedWindow _selectedWindow;

        private enum Position { TopLeft, TopRight, BottomLeft, BottomRight }

        private CancellationTokenSource _mlSource;
        private CancellationToken _mlToken;
        private VolumeDialog volumeDialog = null;
        private OpacityDialog opacityDialog = null;

        #endregion

        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public PiPModeViewModel()
        {
            Logger.Instance.Info("   ====== PiPModeWindow ======   ");

            LoadedCommand = new RelayCommand(LoadedCommandExecute);
            CloseCommand = new RelayCommand(CloseCommandExecute);
            ClosingCommand = new RelayCommand(ClosingCommandExecute);
            ChangeSelectedWindowCommand = new RelayCommand(ChangeSelectedWindowCommandExecute);
            SetVolumeCommand = new RelayCommand<object>(SetVolumeCommandExecute);
            SwitchToSelectedWindowCommand = new RelayCommand(SwitchToSelectedWindowCommandExecute);
            MinimizeCommand = new RelayCommand(MinimizeCommandExecute);
            SetOpacityCommand = new RelayCommand<object>(SetOpacityCommandExecute);
            MouseEnterCommand = new RelayCommand<MouseEventArgs>(MouseEnterCommandExecute);
            MouseMoveCommand = new RelayCommand<MouseEventArgs>(MouseMoveCommandExecute);
            MouseDownCommand = new RelayCommand<MouseEventArgs>(MouseDownCommandExecute);
            MouseUpCommand = new RelayCommand<MouseEventArgs>(MouseUpCommandExecute);
            MouseLeaveCommand = new RelayCommand<MouseEventArgs>(MouseLeaveCommandExecute);
            ForwardInputsCommand = new RelayCommand<object>(ForwardInputsCommandExecute);
            DpiChangedCommand = new RelayCommand(DpiChangedCommandExecute);

            MessengerInstance.Register<SelectedWindow>(this, InitSelectedWindow);

            MinHeight = MinSize;
            MinWidth = MinSize;
        }

        /// <summary>
        /// Set selected region' data. Set position and size of this window
        /// </summary>
        /// <param name="selectedWindow">Selected window to use in pip mode</param>
        private void InitSelectedWindow(SelectedWindow selectedWindow)
        {
            if (selectedWindow == null || selectedWindow.WindowInfo == null)
            {
                Logger.Instance.Error("Can't init PiP mode");
                return;
            }

            Logger.Instance.Info("Init PiP mode : " + selectedWindow.WindowInfo.Title);

            MessengerInstance.Unregister<SelectedWindow>(this);

            Title = selectedWindow.WindowInfo.Title + " - PiP Mode - PiP-Tool";

            _selectedWindow = selectedWindow;
            _renderSizeEventDisabled = true;
            SideBarVisibility = Visibility.Hidden;
            _widthOffset = 0;
            Ratio = _selectedWindow.Ratio;

            DpiChangedCommandExecute();
            Height = (int)(_selectedWindow.SelectedRegion.Height / _dpiY);
            Width = (int)(_selectedWindow.SelectedRegion.Width / _dpiX);
            Top = 200;
            Left = 200;

            Train();

            // set Min size
            if (Height < Width)
                MinWidth = MinSize * (int)Ratio;
            else if (Width < Height)
                MinHeight = MinSize * (int)_selectedWindow.RatioHeightByWidth;

            _renderSizeEventDisabled = false;

            SetSize(DefaultSizePercentage);
            SetPosition(Position.BottomLeft);

            InitDwmThumbnail();
        }

        /// <summary>
        /// Register dwm thumbnail properties
        /// </summary>
        private void InitDwmThumbnail()
        {
            if (_selectedWindow == null || _selectedWindow.WindowInfo.Handle == IntPtr.Zero || _targetHandle == IntPtr.Zero)
                return;

            if (_thumbHandle != IntPtr.Zero)
                NativeMethods.DwmUnregisterThumbnail(_thumbHandle);

            if (NativeMethods.DwmRegisterThumbnail(_targetHandle, _selectedWindow.WindowInfo.Handle, out _thumbHandle) == 0)
                UpdateDwmThumbnail();
        }


        private byte DoubleToByte(double x)
        {
            return (byte)(255 * x);
        }

        private double ByteToDouble(byte x)
        {
            return x / 255.0;
        }

        private NativeStructs.Rect DestRect()
        {
            return new NativeStructs.Rect(0, 0, (int)(_width * _dpiX) - _widthOffset, (int)(_height * _dpiY));
        }

        /// <summary>
        /// Update dwm thumbnail properties
        /// </summary>
        private void UpdateDwmThumbnail()
        {
            if (_thumbHandle == IntPtr.Zero)
                return;
            
            var dest = this.DestRect();
            //dest.Right -= _widthOffset;
            var rcSource = _selectedWindow.SelectedRegion;
            rcSource = new NativeStructs.Rect(rcSource);
            var ratio = rcSource.Height / (float)_height / _dpiY;
            //rcSource.Top += (int)(_heightOffset * ratio);
            var props = new NativeStructs.DwmThumbnailProperties
            {
                fVisible = true,
                dwFlags = (int)(DWM_TNP.DWM_TNP_VISIBLE | DWM_TNP.DWM_TNP_RECTDESTINATION | DWM_TNP.DWM_TNP_OPACITY | DWM_TNP.DWM_TNP_RECTSOURCE),
                opacity = DoubleToByte(this.Opacity),
                rcDestination = dest,
                rcSource = rcSource
            };

            NativeMethods.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
        }

        /// <summary>
        /// Set size of this window
        /// </summary>
        private void SetSize(float sizePercentage)
        {
            _renderSizeEventDisabled = true;
            var resolution = Screen.PrimaryScreen.Bounds;
            if (Height > resolution.Height * sizePercentage)
            {
                Height = (int)(resolution.Height * sizePercentage);
                Width = Convert.ToInt32(Height * Ratio);
            }
            if (Width > resolution.Width * sizePercentage)
            {
                Width = (int)(resolution.Width * sizePercentage);
                Height = Convert.ToInt32(Width * _selectedWindow.RatioHeightByWidth);
            }
            _renderSizeEventDisabled = false;
        }

        /// <summary>
        /// Set position of this window
        /// </summary>
        private void SetPosition(Position position)
        {
            _renderSizeEventDisabled = true;
            var resolution = Screen.PrimaryScreen.Bounds;
            resolution.Width = (int)(resolution.Width / _dpiX);
            resolution.Height = (int)(resolution.Height / _dpiY);
            var top = 0;
            var left = 0;
            switch (position)
            {
                case Position.TopLeft:
                    top = (int)(resolution.Height * DefaultPositionPercentage);
                    left = (int)(resolution.Width * DefaultPositionPercentage);
                    break;
                case Position.TopRight:
                    top = (int)(resolution.Height * DefaultPositionPercentage);
                    left = resolution.Width - Width - (int)(resolution.Width * DefaultPositionPercentage);
                    break;
                case Position.BottomLeft:
                    top = resolution.Height - Height - (int)(resolution.Height * DefaultPositionPercentage);
                    left = (int)(resolution.Width * DefaultPositionPercentage);
                    break;
                case Position.BottomRight:
                    top = resolution.Height - Height - (int)(resolution.Height * DefaultPositionPercentage);
                    left = resolution.Width - Width - (int)(resolution.Width * DefaultPositionPercentage);
                    break;
            }
            Top = top;
            Left = left;
            _renderSizeEventDisabled = false;
        }

        /// <summary>
        /// Gets this window
        /// </summary>
        /// <returns>This window</returns>
        public Window ThisWindow()
        {
            var windowsList = Application.Current.Windows.Cast<Window>();
            return windowsList.FirstOrDefault(window => window.DataContext == this);
        }

        /// <summary>
        /// Add Selected region to data (machine learning) and update the model
        /// </summary>
        private void Train()
        {
            var windowNoBorder = _selectedWindow.WindowInfo.RectNoBorder;
            var regionNoBorder = _selectedWindow.SelectedRegionNoBorder;

            var region =
                    $"{regionNoBorder.Top} " +
                    $"{regionNoBorder.Left} " +
                    $"{regionNoBorder.Height} " +
                    $"{regionNoBorder.Width}";

            _mlSource = new CancellationTokenSource();
            _mlToken = _mlSource.Token;
            Task.Run(() =>
            {
                MachineLearningService.Instance.AddData(
                    region,
                    _selectedWindow.WindowInfo.Program,
                    _selectedWindow.WindowInfo.Title,
                    windowNoBorder.Y,
                    windowNoBorder.X,
                    windowNoBorder.Height,
                    windowNoBorder.Width);

                MachineLearningService.Instance.TrainAsync().ContinueWith(obj => { Console.WriteLine("Trained"); }, _mlToken);
            }, _mlToken);
        }

        /// <summary>
        /// Keep aspect ratio on window resize
        /// https://stackoverflow.com/questions/2471867/resize-a-wpf-window-but-maintain-proportions
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="msg">The message ID.</param>
        /// <param name="wParam">The message's wParam value.</param>
        /// <param name="lParam">The message's lParam value.</param>
        /// <param name="handled">A value that indicates whether the message was handled. Set the value to true if the message was handled; otherwise, false.</param>
        /// <returns>The appropriate return value depends on the particular message. See the message documentation details for the Win32 message being handled.</returns>
        private IntPtr MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var msg2 = (WM)msg;

            /*
            if (msg2 == WM.MOUSEHOVER)
            {
                OnMouseEnter();
            }
            else if (msg2 == WM.MOUSELEAVE)
            {
                OnMouseLeave();
            }
            else if (msg2 == WM.MOUSEMOVE)
            {
                OnMouseMove();
            }
            */

            if (this.forwardInputs)
            {
                ForwardInputs(msg, wParam, lParam, ref handled);
                if (handled)
                    return IntPtr.Zero;
            }

            if (msg2 == WM.LBUTTONDOWN)
                SetNoResize();
            else if (msg2 == WM.LBUTTONUP)
                SetResizeGrip();

            if (msg2 == WM.WINDOWPOSCHANGING)
                return DragMove(hwnd, lParam, ref handled);


            return IntPtr.Zero;
        }

        private IntPtr ForwardInputs(int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var msg2 = (WM)msg;
            switch (msg2)
            {
                case WM.KEYDOWN:
                case WM.KEYUP:
                case WM.IME_KEYDOWN:
                case WM.IME_KEYUP:
                case WM.SYSKEYDOWN:
                case WM.SYSKEYUP:
                case WM.HOTKEY:
                case WM.MOUSEACTIVATE:
                case WM.MOUSELEAVE:
                case WM.NCMOUSELEAVE:
                case WM.HSCROLL:
                case WM.VSCROLL:
                    //Logger.Instance.Debug("Sending key " + msg2);
                    NativeMethods.SendMessage(
                        this._selectedWindow.WindowInfo.Handle,
                        (uint)msg,
                        wParam,
                        lParam
                    );
                    switch (msg2)
                    {
                        case WM.MOUSELEAVE:
                        case WM.NCMOUSELEAVE:
                            break;
                        default:
                            handled = true;
                            break;
                    }
                    break;
                case WM.MOUSEHOVER:
                case WM.MOUSEMOVE:
                case WM.MOUSEWHEEL:
                case WM.MOUSEHWHEEL:
                case WM.LBUTTONDOWN:
                case WM.LBUTTONUP:
                case WM.MBUTTONDOWN:
                case WM.MBUTTONUP:
                case WM.RBUTTONDOWN:
                case WM.RBUTTONUP:
                case WM.XBUTTONDOWN:
                case WM.XBUTTONUP:
                case WM.LBUTTONDBLCLK:
                case WM.MBUTTONDBLCLK:
                case WM.RBUTTONDBLCLK:
                case WM.XBUTTONDBLCLK:
                case WM.NCMOUSEHOVER:
                case WM.NCMOUSEMOVE:
                case WM.NCLBUTTONDOWN:
                case WM.NCLBUTTONUP:
                case WM.NCMBUTTONDOWN:
                case WM.NCMBUTTONUP:
                case WM.NCRBUTTONDOWN:
                case WM.NCRBUTTONUP:
                case WM.NCXBUTTONDOWN:
                case WM.NCXBUTTONUP:
                case WM.NCLBUTTONDBLCLK:
                case WM.NCMBUTTONDBLCLK:
                case WM.NCRBUTTONDBLCLK:
                case WM.NCXBUTTONDBLCLK:
                    try { 
                        var x = NativeMethods.LParamToX((uint)lParam);
                        var y = NativeMethods.LParamToY((uint)lParam);
                        var selectedRect = this._selectedWindow.SelectedRegion;
                        var thisRect = this.DestRect();
                        switch (msg2)
                        {
                            case WM.MOUSEHOVER:
                            case WM.MOUSEMOVE:
                            case WM.MOUSEWHEEL:
                            case WM.MOUSEHWHEEL:
                                break;
                            default:
                                if (x >= thisRect.Width && x <= thisRect.Width + _widthOffset)
                                    return IntPtr.Zero;
                                break;
                        }
                        x = (short)((double)x * selectedRect.Width / thisRect.Width);
                        y = (short)((double)y * selectedRect.Height / thisRect.Height);
                        //Logger.Instance.Debug("Sending click " + msg2 + " at " + x + ", " + y);
                        var lParam2 = NativeMethods.CoordToLParam(x, y);
                        NativeMethods.SendMessage(
                            this._selectedWindow.WindowInfo.Handle,
                            (uint)msg,
                            wParam,
                            (IntPtr)lParam2
                        );
                        switch (msg2)
                        {
                            case WM.MOUSEHOVER:
                            case WM.MOUSEMOVE:
                            case WM.NCMOUSEHOVER:
                            case WM.NCMOUSELEAVE:
                                break;
                            default:
                                handled = true;
                                break;
                        }
                    }
                    catch (System.OverflowException ex)
                    {

                    }
                    break;
            }

            return IntPtr.Zero;
        }

        private IntPtr DragMove(IntPtr hwnd, IntPtr lParam, ref bool handled)
        {
            if (_renderSizeEventDisabled)
                return IntPtr.Zero;

            var position = (NativeStructs.WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(NativeStructs.WINDOWPOS));
            if ((position.flags & (int)SWP.NOMOVE) != 0 ||
                HwndSource.FromHwnd(hwnd)?.RootVisual == null) return IntPtr.Zero;


            /*
            var topBarHeight = 0;
            if (TopBarIsVisible)
                topBarHeight = TopBarHeight;
            position.cx = (int)((position.cy - topBarHeight) * Ratio);
            */
            position.cx = (int)((position.cy) * Ratio);


            Marshal.StructureToPtr(position, lParam, true);
            handled = true;
            return IntPtr.Zero;
        }

        /// <inheritdoc />
        /// <summary>
        /// Remove DragHook
        /// </summary>
        public void Dispose()
        {
            _mlSource?.Cancel();
            ((HwndSource)PresentationSource.FromVisual(ThisWindow()))?.RemoveHook(MessageHook);
        }

        #region commands

        /// <summary>
        /// Executed when the window is loaded. Get handle of the window and call <see cref="InitDwmThumbnail"/> 
        /// </summary>
        private void LoadedCommandExecute()
        {
            ((HwndSource)PresentationSource.FromVisual(ThisWindow()))?.AddHook(MessageHook);
            var windowsList = Application.Current.Windows.Cast<Window>();
            var thisWindow = windowsList.FirstOrDefault(x => x.DataContext == this);
            if (thisWindow != null)
                _targetHandle = new WindowInteropHelper(thisWindow).Handle;
            InitDwmThumbnail();
        }

        /// <summary>
        /// Executed on click on close button. Close this window
        /// </summary>
        private void CloseCommandExecute()
        {
            MessengerInstance.Unregister<SelectedWindow>(this);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Executed when the window is closing.
        /// </summary>
        private void ClosingCommandExecute()
        {
            Logger.Instance.Info("   |||||| Close PiPModeWindow ||||||   ");
            Dispose();
        }

        /// <summary>
        /// Executed on click on change selected window button. Close this window and open <see cref="MainWindow"/>
        /// </summary>
        private void ChangeSelectedWindowCommandExecute()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            CloseCommandExecute();
        }

        private double GetWindowLeft(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                var leftField = typeof(Window).GetField("_actualLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (double)leftField.GetValue(window);
            }
            else
                return window.Left;
        }

        private double GetWindowTop(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                var leftField = typeof(Window).GetField("_actualTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (double)leftField.GetValue(window);
            }
            else
                return window.Top;
        }


        /// <summary>
        /// Executed on click on switch to button. />
        /// </summary>
        private void SwitchToSelectedWindowCommandExecute()
        {
            var thisWindow = ThisWindow();
            var selectedHandle = this._selectedWindow.WindowInfo.Handle;

            NativeMethods.ShowWindow(
                selectedHandle,
                NativeMethods.IsZoomed(selectedHandle) ? ShowWindowCommands.ShowMaximized : ShowWindowCommands.Restore
            );
            NativeMethods.SetForegroundWindow(selectedHandle);
            NativeMethods.SetActiveWindow(selectedHandle);
            NativeMethods.BringWindowToTop(selectedHandle);
            NativeMethods.SetFocus(selectedHandle);
            NativeMethods.SwitchToThisWindow(selectedHandle, true);
            SystemCommands.MinimizeWindow(thisWindow);
        }

        /// <summary>
        /// Executed on click on minimize button. />
        /// </summary>
        private void MinimizeCommandExecute()
        {
            var thisWindow = ThisWindow();
            SystemCommands.MinimizeWindow(thisWindow);
        }

        /// <summary>
        /// Executed on click on set volume button. Opens <see cref="VolumeDialog"/>
        /// </summary>
        private void SetVolumeCommandExecute(object button)
        {
            if (volumeDialog != null)
                volumeDialog.Close();


            var selectedWindow = new ProcessInfo(this._selectedWindow.WindowInfo);
            var audioControls = new AudioControls(selectedWindow);

            var task = Task.Run(() => {
                audioControls.GetControls(DataFlow.Render);
            });

            var thisWindow = ThisWindow();
            var volumeButton = (Button)button;
            
            volumeDialog = new VolumeDialog();
            InitButtonDialog(volumeDialog, volumeButton, thisWindow);
            volumeDialog.Closed += OnVolumeDialogClose;

            task.Wait();
            if (!audioControls.HasControls)
            {
                volumeDialog.Close();
                return;
            }
            MessengerInstance.Send(audioControls);
            volumeDialog.Show();

        }

        private void OnVolumeDialogClose(object source, System.EventArgs e)
        {
            volumeDialog = null;
        }

        private void InitButtonDialog(Window dialog, Button button, Window parent)
        {
            var buttonPosition = button.TransformToAncestor(parent).Transform(new WPoint(0, 0));
            dialog.Owner = parent;
            dialog.Top = parent.Top + buttonPosition.Y + button.Height;
            dialog.Left = parent.Left + buttonPosition.X;
        }

        /// <summary>
        /// Executed on click on set opacity button. Opens <see cref="OpacityDialog"/>
        /// </summary>
        private void SetOpacityCommandExecute(object button)
        {
            if (volumeDialog != null)
                volumeDialog.Close();
            var thisWindow = ThisWindow();
            var opacityButton = (Button)button;

            opacityDialog = new OpacityDialog();
            InitButtonDialog(opacityDialog, opacityButton, thisWindow);
            opacityDialog.Closed += OnOpacityDialogClose;

            MessengerInstance.Send(new PiPWindowInfo(this));
            opacityDialog.Show();
        }

        private void OnOpacityDialogClose(object source, System.EventArgs e)
        {
            opacityDialog = null;
        }

        /// <summary>
        /// Executed on mouse enter. Open top bar
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void MouseEnterCommandExecute(MouseEventArgs e)
        {
            OnMouseEnter();
            e.Handled = true;
        }

        private void OnMouseEnter()
        {
            mouseOver = true;
            this.ThisWindow().ReleaseMouseCapture();
            ShowSidebar();
        }
        private bool ShowSidebar()
        {
            if (SideBarIsVisible)
                return true;
            _renderSizeEventDisabled = true;
            SideBarVisibility = Visibility.Visible;
            var sideBarWidth = SideBarWidth;
            _widthOffset = (int)(SideBarWidth * _dpiX);
            //Top = Top - topBarHeight;
            Width = Width + sideBarWidth;
            MinWidth = MinWidth + sideBarWidth;
            _renderSizeEventDisabled = false;
            return true;
        }
        private void MouseMoveCommandExecute(MouseEventArgs e)
        {
            OnMouseMove();
            e.Handled = true;
        }

        private void OnMouseMove()
        {

            if (mouseOver)
            {
                ShowSidebar();
            }
            else
            {
                HideSidebar();
            }
        }

        /// <summary>
        /// Executed on mouse down. Disable Resize
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void MouseDownCommandExecute(MouseEventArgs e)
        {
            Logger.Instance.Debug("Mousedown");
            if (
                e.LeftButton == MouseButtonState.Pressed
                || System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed
            )
            {
                Logger.Instance.Debug("Left mouse pressed");
                SetNoResize();
            }
            e.Handled = true;
        }

        private void SetNoResize()
        {
            var thisWindow = ThisWindow();
            // this prevents win7 aerosnap
            if (thisWindow.ResizeMode != System.Windows.ResizeMode.NoResize)
            {
                Logger.Instance.Debug("Set no resize");
                thisWindow.ResizeMode = System.Windows.ResizeMode.NoResize;
                thisWindow.UpdateLayout();
            }

        }

        /// <summary>
        /// Executed on mouse up. Enable Resize
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void MouseUpCommandExecute(MouseEventArgs e)
        {
            SetResizeGrip();
            e.Handled = true;
        }

        private void SetResizeGrip()
        {

            var thisWindow = ThisWindow();
            if (thisWindow.ResizeMode == System.Windows.ResizeMode.NoResize)
            {
                // restore resize grips
                thisWindow.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;
                thisWindow.UpdateLayout();
            }
        }

        /// <summary>
        /// Executed on mouse leave. Close top bar
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void MouseLeaveCommandExecute(MouseEventArgs e)
        {
            OnMouseLeave();
            e.Handled = true;
        }

        private void OnMouseLeave()
        {
            mouseOver = false;
            HideSidebar();
        }
        private bool HideSidebar()
        {
            // Prevent OnMouseEnter, OnMouseLeave loop
            if (!SideBarIsVisible)
                return true;
            Thread.Sleep(50);
            if (!SideBarIsVisible)
                return true;
            if (IsMouseOver())
            {
                WaitMouseLeave();
                return false;
            }
            SideBarVisibility = Visibility.Hidden;
            try
            {
                this.ThisWindow().ReleaseMouseCapture();
            }catch(NullReferenceException ex)
            {

            }
            _renderSizeEventDisabled = true;
            _widthOffset = 0;

            var sideBarWidth = SideBarWidth;
            //Top = Top + topBarHeight;
            MinWidth = MinWidth - sideBarWidth;
            Width = Width - sideBarWidth;
            _renderSizeEventDisabled = false;
            return true;
        }

        public bool IsMouseOver()
        {

            NativeMethods.GetCursorPos(out var p);
            var r = new Rectangle(
                Convert.ToInt32(Left * _dpiX),
                Convert.ToInt32(Top * _dpiY),
                Convert.ToInt32(Width * _dpiX),
                Convert.ToInt32(Height * _dpiY)
            );
            var pa = new Point(Convert.ToInt32(p.X), Convert.ToInt32(p.Y));

            return r.Contains(pa);
        }


        private System.Windows.Threading.DispatcherTimer mouseLeaveTimer = null;
        public void WaitMouseLeave()
        {
            if (mouseLeaveTimer != null)
            {
                if (mouseLeaveTimer.IsEnabled)
                    return;
                else
                    StopWaitMouseLeave();
            }
            mouseLeaveTimer = new System.Windows.Threading.DispatcherTimer();
            mouseLeaveTimer.Tick += new EventHandler(WaitMouseLeaveTick);
            mouseLeaveTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            mouseLeaveTimer.Start();
        }

        private void WaitMouseLeaveTick(object sender, EventArgs e)
        {
            if (mouseOver)
            {
                StopWaitMouseLeave();
                return;
            }else if (HideSidebar())
            {
                StopWaitMouseLeave();
            }
        }

        private void StopWaitMouseLeave()
        {
            mouseLeaveTimer.Stop();
            mouseLeaveTimer = null;
        }

        /// <summary>
        /// Executed on click forward input button.
        /// </summary>
        private void ForwardInputsCommandExecute(object button)
        {
            this.forwardInputs = !this.forwardInputs;
            //SetStrikethrough((Button)button, this.forwardInputs);
            var bc = new BrushConverter();
            ((Button)button).Background = (Brush)bc.ConvertFrom(this.forwardInputs ? "#FF7C7C7C" : "#007C7C7C");
        }

        private void SetStrikethrough(Button b, Boolean strikethrough)
        {
            var textBlock = (TextBlock)b.Content;

            if (strikethrough)
            {
                if (!textBlock.TextDecorations.Any())
                    textBlock.TextDecorations.Add(
                        new TextDecoration { Location = TextDecorationLocation.Strikethrough });
            }
            else
            {
                textBlock.TextDecorations.Clear();
            }
        }

        /// <summary>
        /// Executed on DPI change to handle multi-screen with different DPI
        /// </summary>
        private void DpiChangedCommandExecute()
        {
            DpiHelper.GetDpi(_targetHandle, out _dpiX, out _dpiY);
        }

        #endregion

    }
}
