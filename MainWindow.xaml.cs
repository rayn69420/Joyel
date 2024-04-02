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
        public static extern bool GetCursorPos(out Point pos);

        [DllImport("user32.dll")]
        public static extern bool GetAsyncKeyState(int button);

        public enum MouseButton
        {
            LeftMouseButton = 0x01,
            RightMouseButton = 0x02,
            MiddleMouseButton = 0x04,
        }

        public struct Point
        {
            public int X;
            public int Y;
        }

        const short STEERING_MAX = 16384;

        ViGEmClient client = new ViGEmClient();
        IXbox360Controller? controller = null;

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

            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;

            //InputForward(false);
            //InputBackward(false);

            while (true)
            {
                bool isLeftMousePressed = IsMouseButtonPressed(MouseButton.LeftMouseButton);
                bool isRightMousePressed = IsMouseButtonPressed(MouseButton.RightMouseButton);

                int deadzonePercent = InvokeFuncOnMain(delegate { return int.Parse(textBox_deadzone.Text); });
                int lPadding = InvokeFuncOnMain(delegate { return int.Parse(textBox_padding.Text); });
                int rPadding = screenWidth - lPadding;
                int mousePosX = GetMousePosX();

                int steerPercent = ((mousePosX - lPadding) * 200) / (rPadding - lPadding);
                int steerValueUI = int.Clamp((steerPercent - 100), -100, 100);

                if (steerValueUI < deadzonePercent && steerValueUI > 0) steerValueUI = 0;
                else if (steerValueUI > -deadzonePercent && steerValueUI < 0) steerValueUI = 0;
                UpdateSteeringSlider(steerValueUI);

                short steeringValue = short.Clamp((short)(steerValueUI * (STEERING_MAX / 100)), -STEERING_MAX, STEERING_MAX);
                UpdateSteeringText(steerValueUI + "% [" + steeringValue + "]");

                Color statusColor = Colors.White;

                if (isLeftMousePressed)
                    statusColor = Colors.LightGreen;

                if (isRightMousePressed)
                {
                    steeringValue = 0;
                    statusColor = Colors.OrangeRed;
                }

                UpdateStatusLed(statusColor);
                InputSteering(steeringValue);

                Thread.Sleep(10);
            }
        }

        #region UI Helpers

        private int InvokeFuncOnMain(Func<int> action)
        {
            int result = -1;
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                result = action();
            });
            return result;
        }

        private void InvokeActionOnMain(Action action)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { action(); });
        }

        #endregion

        #region UI Methods

        void UpdateSteeringSlider(int value)
        {
            InvokeActionOnMain(delegate { slider_steeringValue.Value = value; });
        }

        void UpdateStatusLed(Color color)
        {
            InvokeActionOnMain(delegate { button_inputStatus.Background = new SolidColorBrush(color); });
        }

        void UpdateSteeringText(string text)
        {
            InvokeActionOnMain(delegate { textBlock_steeringValue.Text = text; });
        }

        #endregion UI Methods

        #region Controller Inputs

        void ConnectController()
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