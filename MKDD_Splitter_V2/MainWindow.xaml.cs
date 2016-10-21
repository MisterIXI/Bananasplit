using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Diagnostics;
using System.Timers;

namespace MKDD_Splitter_V2
{ //LowLevelKeyboardHook << steal this from Livesplit
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Stopwatch MainStopwatch = new Stopwatch();
        Timer MainRefreshTimer;
        Timer HotkeyPollTimer;
        LiveSplit.Model.Input.KeyboardHook HookInstance = new LiveSplit.Model.Input.KeyboardHook();

        public MainWindow()
        {
            InitializeComponent();
            HotkeyPollTimer = new Timer(25);
            HotkeyPollTimer.Elapsed += new ElapsedEventHandler(OnHotkeyPoll);
            HotkeyPollTimer.Start();
        }

        private void Init()
        {

        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Space)
            {
                //Console.Beep();
                if (!MainStopwatch.IsRunning)
                {
                    MainStopwatch.Start();
                    MainRefreshTimer = new Timer(50);
                    MainRefreshTimer.Elapsed += new ElapsedEventHandler(OnMainRefreshTimer);
                    MainRefreshTimer.Start();
                }
                else
                {
                    MainStopwatch.Stop();
                    TimerLabel.Text = MainStopwatch.Elapsed.ToString(@"mm\:ss\.fff");
                    MainStopwatch.Reset();                    
                    MainRefreshTimer.Dispose();
                }
            }
            if (e.Key == Key.A)
            {
                HookInstance.RegisterHotKey(System.Windows.Forms.Keys.Space);
                Console.WriteLine("FAFWF");
            }
            
        }

        private void OnHotkeyPoll(object source, ElapsedEventArgs e)
        {
            HookInstance.Poll();
        }

        private void OnMainRefreshTimer(object source, ElapsedEventArgs e)
        {
            
            //TestLabel.Content = "ficl ypi aföö";//string.Format(@"mm\:ss\.fff",MainStopwatch.Elapsed);
            this.Dispatcher.Invoke(() =>
            {
                TimerLabel.Text = MainStopwatch.Elapsed.ToString(@"mm\:ss\.fff");
            });
            //LiveSplit.Model.Input.KeyboardHook.RegisterHotKey(System.Windows.Forms.Keys.Space);
        }
    }
}
