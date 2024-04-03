using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using Color = System.Windows.Media.Color;

namespace JoyelWPF
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point pos);

        [DllImport("user32.dll")]
        private static extern bool GetAsyncKeyState(int button);

        private enum MouseButton
        {
            LeftMouseButton = 0x01,
            RightMouseButton = 0x02,
            MiddleMouseButton = 0x04,
        }

        private struct Point
        {
            public int X;
            public int Y;
        }

        private const short STEERING_MAX = 16384;
        private ViGEmClient client = new ViGEmClient();
        private IXbox360Controller? controller = null;

        public MainWindow()
        {
            InitializeComponent();
            AsyncStart();
        }

        public async Task AsyncStart()
        {
            await Task.Run(() => { Start(); });
        }

        private async void Start()
        {
            ConnectController();

            bool isLeftMousePressed = false;
            bool isRightMousePressed = false;
            bool? isMouseOnly = false;

            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int deadzonePercent = 0;
            int lPadding = 0;
            int rPadding = 0;
            int mousePosX = 0;

            int steerPercent = 0;
            int steerValueUI = 0;
            short steeringValue = 0;

            bool goForward = false;
            bool goBackward = false;

            while (true)
            {
                isLeftMousePressed = IsMouseButtonPressed(MouseButton.LeftMouseButton);
                isRightMousePressed = IsMouseButtonPressed(MouseButton.RightMouseButton);

                isMouseOnly = InvokeFuncOnMain(delegate { return checkBox_mouseOnly.IsChecked; });
                deadzonePercent = InvokeFuncOnMain(delegate { return int.Parse(textBox_deadzone.Text); });
                lPadding = InvokeFuncOnMain(delegate { return int.Parse(textBox_padding.Text); });
                rPadding = screenWidth - lPadding;
                mousePosX = GetMousePosX();

                steerPercent = ((mousePosX - lPadding) * 200) / (rPadding - lPadding);
                steerValueUI = int.Clamp((steerPercent - 100), -100, 100);

                if (steerValueUI < deadzonePercent && steerValueUI > 0) steerValueUI = 0;
                else if (steerValueUI > -deadzonePercent && steerValueUI < 0) steerValueUI = 0;
                UpdateSteeringSlider(steerValueUI);

                steeringValue = short.Clamp((short)(steerValueUI * (STEERING_MAX / 100)), -STEERING_MAX, STEERING_MAX);
                UpdateSteeringText(steerValueUI + "% [" + steeringValue + "]");

                if (isMouseOnly.HasValue && isMouseOnly.Value == true)
                {
                    goForward = (isLeftMousePressed) ? true : false;
                    goBackward = (isRightMousePressed) ? true : false;

                    InputForward(goForward);
                    InputBackward(goBackward);
                    InputSteering(steeringValue);
                    UpdateStatusLed(Colors.Yellow);

                    Thread.Sleep(10);
                    continue;
                }

                Color statusColor = Colors.White;

                if (isRightMousePressed)
                {
                    steeringValue = 0;
                    InputSteering(steeringValue);
                    statusColor = Colors.OrangeRed;
                }

                if (isLeftMousePressed)
                {
                    InputSteering(steeringValue);
                    statusColor = Colors.LightGreen;
                }

                UpdateStatusLed(statusColor);

                Thread.Sleep(10);
            }
        }

        #region UI Actions

        private void checkBox_mouseOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            InputForward(false);
            InputBackward(false);
        }

        #endregion UI Actions

        #region UI Helpers

        private T InvokeFuncOnMain<T>(Func<T> action)
        {
            object result = null;
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                result = action();
            });
            return (T)result;
        }

        private void InvokeActionOnMain(Action action)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { action(); });
        }

        #endregion UI Helpers

        #region UI Methods

        private void UpdateSteeringSlider(int value)
        {
            InvokeActionOnMain(delegate { slider_steeringValue.Value = value; });
        }

        private void UpdateStatusLed(Color color)
        {
            InvokeActionOnMain(delegate { button_inputStatus.Background = new SolidColorBrush(color); });
        }

        private void UpdateSteeringText(string text)
        {
            InvokeActionOnMain(delegate { textBlock_steeringValue.Text = text; });
        }

        #endregion UI Methods

        #region Controller Inputs

        private void ConnectController()
        {
            controller = client.CreateXbox360Controller();
            controller.Connect();
        }

        private async Task InputSteering(short value)
        {
            await Task.Run(() => { controller.SetAxisValue(Xbox360Axis.LeftThumbX, value); });
        }

        private async Task InputBackward(bool value)
        {
            await Task.Run(() => { controller.SetButtonState(Xbox360Button.LeftShoulder, value); });
        }

        private async Task InputForward(bool value)
        {
            await Task.Run(() => { controller.SetButtonState(Xbox360Button.A, value); });
        }

        #endregion Controller Inputs

        #region Helper Methods

        private int GetMousePosX()
        {
            Point pos;
            GetCursorPos(out pos);
            return pos.X;
        }
        private bool IsMouseButtonPressed(MouseButton button)
        {
            return GetAsyncKeyState((int)button) != false;
        }

        #endregion Helper Methods

    }
}