using BoardSimulator.Core;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoardSimulator.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly Net _buttonNet = new();
        private readonly GpioPin _buttonGpio = new();
        private readonly ButtonComponent _button = new("button", "Button");
        private readonly LedComponent _led = new("led", "LED");
        private readonly GpioPin _ledGpio = new();
        private readonly Net _ledNet = new();

        public MainWindow()
        {
            InitializeComponent();

            _buttonGpio.SetMode(GpioMode.InputPullUp);
            _buttonNet.Pins.Add(_buttonGpio.Pin);
            _buttonNet.Pins.Add(_button.Signal);

            _ledGpio.SetMode(GpioMode.Output);
            _ledNet.Pins.Add(_ledGpio.Pin);
            _ledNet.Pins.Add(_led.Signal);

            RefreshButtonPin();
            RefreshLedPin();
        }

        private void RefreshButtonPin()
        {
            ButtonPinMarker.ToolTip = $"BTN\nMode: {_buttonGpio.Mode}\nDrive: {_buttonGpio.Pin.Drive}\nPull-up: {_buttonGpio.Pin.PullUpEnabled}\nNet: {_buttonNet.Resolve()}";

            ButtonPinMarker.Fill = _buttonNet.Resolve() switch
            {
                NetState.High => Brushes.Green,
                NetState.Low => Brushes.Red,
                NetState.X => Brushes.Yellow,
                _ => Brushes.Gray
            };
        }

        private void PressButton_Click(object sender, RoutedEventArgs e)
        {
            _button.SetPressed(!_button.IsPressed);
            PressButton.Content = _button.IsPressed ? "Release" : "Press";
            RefreshButtonPin();
            _ledGpio.Write(_buttonNet.Resolve() == NetState.Low ? GpioLevel.High : GpioLevel.Low);
            RefreshLedPin();
        }

        private void RefreshLedPin()
        {
            LedMarker.ToolTip = $"LED\nMode: {_ledGpio.Mode}\nDrive: {_ledGpio.Pin.Drive}\nPull-up: {_ledGpio.Pin.PullUpEnabled}\nNet: {_ledNet.Resolve()}\nState: {_led.GetState(_ledNet.Resolve())}";
            LedMarker.Fill = _led.GetState(_ledNet.Resolve()) switch
            {
                LedState.On => Brushes.White,
                LedState.Off => Brushes.Black,
                _ => Brushes.Gray
            };
        }
    }
}
