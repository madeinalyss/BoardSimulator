using BoardSimulator;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Net _buttonNet = new();
        private readonly GpioPin _buttonGpio = new();

        public MainWindow()
        {
            InitializeComponent();

            _buttonGpio.SetMode(GpioMode.InputPullUp);
            _buttonNet.Pins.Add(_buttonGpio.Pin);
            ButtonPinMarker.ToolTip = $"BTN\nMode: {_buttonGpio.Mode}\nDrive: {_buttonGpio.Pin.Drive}\nPull-up: {_buttonGpio.Pin.PullUpEnabled}\nNet: {_buttonNet.Resolve()}";
        }
    }
}