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

        //Splitter Variables
        int currentTrackIndex, currentSplitProgress, currentSplitInUI, scrollToIndex = 0;

        int[] currentTrackOrder = new int[15];
        TimeSpan[] Splittimes, PBSplits, UnsavedGoldSplits, GoldSplits = new TimeSpan[15];
        TimeSpan lastSplit;
        bool[] isSplitAGoldSplit = new bool[15];

        /*
        Variable explanation:
        Trackindex: chosen Track to split on when the splitbutton is pressed (0-15)
        currentSplitProgress: # of Splits splittet in the run
        currentSplitInUI: MostBottom existing Split Index (0-15)
        scrollToIndex: Index of SPlit in most bottom Label (scrolled) (4-15)

        currentTrackOrder: Index of Track in place

        Splittimes: Timespans of current splittet Splits (with correct Index e.g. 14 = BC)
        PBSplits: Timespans of saved & loaded PBSplits
        UnsavedGoldSplits: Timespans of eventual GoldSPlits ready to be saved or discarded
        GoldSplits: Timespans of saved & loaded GoldSPlits

        lastSplit: Timespans (point in the run) where the last Split happenend for comparison
        */
        

        public MainWindow()
        {
            InitializeComponent();
            HotkeyPollTimer = new Timer();
            HotkeyPollTimer.Interval = 25;
            HotkeyPollTimer.Tick += new EventHandler(OnHotkeyPoll);
            HotkeyPollTimer.Start();
            Hook = new LiveSplit.Model.Input.CompositeHook();
            Hook.KeyOrButtonPressed += Hook_OnKeyPress;
            
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
                TimerLabel.Content = MainStopwatch.Elapsed.ToString(@"mm\:ss\.fff");               
            });
            //LiveSplit.Model.Input.KeyboardHook.RegisterHotKey(System.Windows.Forms.Keys.Space);
        }

        public void DebugBeep()
        {
            Console.Beep();
        }

        void Hook_OnKeyPress(object sender, LiveSplit.Model.Input.KeyOrButton e)
        {

            Console.WriteLine("pressing triggered");
            
            Action action = () =>
            {
                if(Keys.Space == e.Key)
                {
                    Console.Beep();
                    Console.WriteLine("correct Bitch");
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
                    StartSplitting();
                }
                else
                {
                    MainStopwatch.Stop();
                    TimerLabel.Content = MainStopwatch.Elapsed.ToString(@"mm\:ss\.fff");
                    MainStopwatch.Reset();
                    MainRefreshTimer.Dispose();
                }
            }
            if (e.Key == System.Windows.Input.Key.A)
            {
                Hook.RegisterHotKey(Keys.Space);
            }
            if (e.Key == System.Windows.Input.Key.S)
            {
                
            }

        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        #region Splitting Functions

        void StartSplitting()
        {
            MainStopwatch.Start();
            MainRefreshTimer = new Timer();
            MainRefreshTimer.Interval = 50;
            MainRefreshTimer.Tick += new EventHandler(OnMainRefreshTimer);
            MainRefreshTimer.Start();
            currentTrackIndex = 0;
            lastSplit = TimeSpan.Zero;
        }

        void Split()
        {
            TimeSpan tempTimeSpan = MainStopwatch.Elapsed; // so the time stays the same and isn't influenced by CPU time
            if (currentTrackIndex == 15)
            {
                MainStopwatch.Stop();
                TimerLabel.Content = tempTimeSpan.ToString(@"mm\:ss\.fff");
                MainRefreshTimer.Dispose();
                return;
            }
            else
            {
                currentTrackOrder[currentSplitProgress] = currentTrackIndex;
                Splittimes[currentTrackIndex] = tempTimeSpan - lastSplit;
                if(Splittimes[currentTrackIndex] < GoldSplits[currentTrackIndex])
                {
                    isSplitAGoldSplit[currentTrackIndex] = true;
                }
                lastSplit = tempTimeSpan;
            }
        }

        void Reset()
        {
            MainStopwatch.Stop();
            if (MainRefreshTimer != null) MainRefreshTimer.Dispose();
            TimerLabel.Content = ("00:00.000");
            //todo
        }

        #endregion

        #region Splitselection

        void SelectSplit(int trackIndex)
        {
            //todo
        }

        #endregion

    }
}
