using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
//using System.Timers;


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
        LiveSplit.Model.Input.CompositeHook Hook { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            HotkeyPollTimer = new Timer();
            HotkeyPollTimer.Interval = 25;
            HotkeyPollTimer.Tick += new EventHandler(OnHotkeyPoll);
            HotkeyPollTimer.Start();
            Hook = new LiveSplit.Model.Input.CompositeHook();
            Hook.KeyPressed += Hook_OnKeyPress;
        }

        private void Init()
        {

        }


        private void OnHotkeyPoll(object sender, EventArgs e)
        {
            HookInstance.Poll();
        }

        private void OnMainRefreshTimer(object source, EventArgs e)
        {

            //TestLabel.Content = "ficl ypi aföö";//string.Format(@"mm\:ss\.fff",MainStopwatch.Elapsed);
            this.Dispatcher.Invoke(() =>
            {
                TimerLabel.Text = MainStopwatch.Elapsed.ToString(@"mm\:ss\.fff");
            });
            //LiveSplit.Model.Input.KeyboardHook.RegisterHotKey(System.Windows.Forms.Keys.Space);
        }

        void Hook_OnKeyPress(object sender, KeyEventArgs e)
        {


            
            Action action = () =>
            {
                if(Keys.Space == e.KeyCode)
                {
                    Console.Beep();
                }
            };

            new Task(() =>
            {
                try
                {
                   this.Dispatcher.Invoke(action);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }).Start();
            
        }

        private void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                //Console.Beep();
                if (!MainStopwatch.IsRunning)
                {
                    MainStopwatch.Start();
                    MainRefreshTimer = new Timer();
                    MainRefreshTimer.Interval = 50;
                    MainRefreshTimer.Tick += new EventHandler(OnMainRefreshTimer);
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
            if (e.Key == System.Windows.Input.Key.A)
            {
                HookInstance.RegisterHotKey(Keys.Space);
                Console.WriteLine("FAFWF");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
