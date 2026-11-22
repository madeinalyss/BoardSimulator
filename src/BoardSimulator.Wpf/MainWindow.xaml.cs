using BoardSimulator.Core;
using BoardSimulator.Runtime;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly Board _board = new Board();
        private readonly BoardHandler _hal;
        private WorkerSession? _session;
        private Task _workerOperation = Task.CompletedTask;
        private bool _closing;
        private bool _allowClose;

        public MainWindow()
        {
            InitializeComponent();

            _hal = new BoardHandler(_board);

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            PressButton.IsEnabled = false;

            RefreshButtonPin();
            RefreshLedPin();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            if (_closing)
                return;

            _workerOperation = InitializeWorkerAsync();
            await _workerOperation;
        }

        private async void PressButton_Click(object sender, RoutedEventArgs e)
        {
            if (_closing || _session is null || !PressButton.IsEnabled)
                return;

            _workerOperation = RunStepAsync();
            await _workerOperation;
        }

        private async Task InitializeWorkerAsync()
        {
            try
            {
                using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));

                _session = await WorkerLauncher.StartAsync(@"D:\Projects\BoardSimulator\artifacts\native\BoardSimulator.Worker\x64\Debug\BoardSimulator.Worker.exe", deadline.Token);

                string response = await _session.SendCommandAsync("SETUP", deadline.Token, _hal.Handle);

                if (response != "DONE SETUP")
                    throw new InvalidOperationException("Unexpected setup response.");

                RefreshButtonPin();
                RefreshLedPin();
                PressButton.IsEnabled = !_closing;
            }
            catch (Exception exception)
            {
                PressButton.IsEnabled = false;

                MessageBox.Show(exception.Message, "Worker error");

                Close();
            }
        }

        private async Task RunStepAsync()
        {
            if (_session is null)
                return;

            PressButton.IsEnabled = false;

            try
            {
                _board.Button.SetPressed(!_board.Button.IsPressed);
                PressButton.Content = _board.Button.IsPressed ? "Release" : "Press";

                using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));

                string response = await _session.SendCommandAsync("STEP", deadline.Token, _hal.Handle);

                if (response != "DONE STEP")
                    throw new InvalidOperationException("Unexpected step response.");

                RefreshButtonPin();
                RefreshLedPin();
                PressButton.IsEnabled = !_closing;
            }
            catch (Exception exception)
            {
                RefreshButtonPin();
                RefreshLedPin();
                MessageBox.Show(exception.Message, "Worker error");
                Close();
            }
        }

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_allowClose)
                return;

            e.Cancel = true;

            if (_closing)
                return;

            _closing = true;
            PressButton.IsEnabled = false;

            try
            {
                try
                {
                    await _workerOperation;
                }
                finally
                {
                    WorkerSession? session = _session;
                    _session = null;

                    if (session is not null)
                        await session.DisposeAsync();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Worker shutdown error");
            }
            finally
            {
                _allowClose = true;
                _ = Dispatcher.BeginInvoke(new Action(Close));
            }
        }

        private void RefreshButtonPin()
        {
            ButtonPinMarker.ToolTip = $"BTN\nMode: {_board.ButtonGpio.Mode}\nDrive: {_board.ButtonGpio.Pin.Drive}\nPull-up: {_board.ButtonGpio.Pin.PullUpEnabled}\nNet: {_board.ButtonNet.Resolve()}";

            ButtonPinMarker.Fill = _board.ButtonNet.Resolve() switch
            {
                NetState.High => Brushes.Green,
                NetState.Low => Brushes.Red,
                NetState.X => Brushes.Yellow,
                _ => Brushes.Gray
            };
        }

        private void RefreshLedPin()
        {
            LedMarker.ToolTip = $"LED\nMode: {_board.LedGpio.Mode}\nDrive: {_board.LedGpio.Pin.Drive}\nPull-up: {_board.LedGpio.Pin.PullUpEnabled}\nNet: {_board.LedNet.Resolve()}\nState: {_board.Led.GetState(_board.LedNet.Resolve())}";
            LedMarker.Fill = _board.Led.GetState(_board.LedNet.Resolve()) switch
            {
                LedState.On => Brushes.White,
                LedState.Off => Brushes.Black,
                _ => Brushes.Gray
            };
        }
    }
}
