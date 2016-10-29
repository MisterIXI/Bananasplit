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
using System.Xml.Linq;
//using System.Timers;


namespace BananaSplit
{ //LowLevelKeyboardHook << steal this from Livesplit
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Stopwatch MainStopwatch = new Stopwatch();
        Stopwatch SegmentStopwatch = new Stopwatch();
        Timer MainRefreshTimer;
        Timer HotkeyPollTimer;
        LiveSplit.Model.Input.KeyboardHook HookInstance = new LiveSplit.Model.Input.KeyboardHook();
        LiveSplit.Model.Input.CompositeHook Hook { get; set; }

        #region XMLVariables

        XDocument saveFile;
        string defaultSaveFileName = "MKDDSplitter_SaveFile.xml";

        #endregion


        #region SplitterVariables
        //Splitter Variables
        int currentTrackIndex, currentSplitProgress = 0;

        int[] currentTrackOrder = new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};
        TimeSpan[] Splittimes = new TimeSpan[16], PBSplits = new TimeSpan[16], UnsavedGoldSplits = new TimeSpan[16], GoldSplits = new TimeSpan[16];
        TimeSpan lastSplit;
        bool[] isSplitAGoldSplit = new bool[16];
        bool isPendingTrackSelection;

        /*
        Variable explanation:

        currentTrackindex: chosen Track to split on when the splitbutton is pressed (0-15)
        currentSplitProgress: # of Splits splittet in the run

        currentTrackOrder: Index of Track in place

        Splittimes: Timespans of current splittet Splits (with correct Index e.g. 14 = BC)
        PBSplits: Timespans of saved & loaded PBSplits
        UnsavedGoldSplits: Timespans of eventual GoldSPlits ready to be saved or discarded
        GoldSplits: Timespans of saved & loaded GoldSPlits

        lastSplit: Timespans (point in the run) where the last Split happenend for comparison

        isSplitAGoldSplit: Set Flag if Split in array Position is a Gold Split
        */
        #endregion

        #region InterfaceVariables
        //Interface Variables
        int  ScrollOffset = 0;

        TimeSpan[] splitTimesInRun = new TimeSpan[16];
        Image[] trackSelectionImages; 
        System.Windows.Controls.Label[,] SplitLabelArray;

        /*
        Variable Explanation:



        */
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            HotkeyPollTimer = new Timer();
            HotkeyPollTimer.Interval = 25;
            HotkeyPollTimer.Tick += new EventHandler(OnHotkeyPoll);
            HotkeyPollTimer.Start();
            Hook = new LiveSplit.Model.Input.CompositeHook();
            Hook.KeyOrButtonPressed += Hook_OnKeyPress;
            Hook.RegisterHotKey(Keys.Space);

            SplitLabelArray = new System.Windows.Controls.Label[,] {
                { L_TrackID1, L_GoldDiff1, L_SplitDiff1, L_SplitT1 },
                { L_TrackID2, L_GoldDiff2, L_SplitDiff2, L_SplitT2 },
                { L_TrackID3, L_GoldDiff3, L_SplitDiff3, L_SplitT3 },
                { L_TrackID4, L_GoldDiff4, L_SplitDiff4, L_SplitT4 },
                { L_TrackID5, L_GoldDiff5, L_SplitDiff5, L_SplitT5 },
                { L_TrackID6, L_GoldDiff6, L_SplitDiff6, L_SplitT6 },
                { L_TrackID7, L_GoldDiff7, L_SplitDiff7, L_SplitT7 }};
            trackSelectionImages = new Image[] { TrackLogo1, TrackLogo2, TrackLogo3, TrackLogo4, TrackLogo5, TrackLogo6, TrackLogo7, TrackLogo8, TrackLogo9, TrackLogo10, TrackLogo11, TrackLogo12, TrackLogo13, TrackLogo14, TrackLogo15, TrackLogo16 };
            //LoadInXMLFile(defaultSaveFileName);
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
                TimerLabel.Content = MainStopwatch.Elapsed.ToString(@"mm\:ss");
                SegmentLabel.Content = SegmentStopwatch.Elapsed.ToString(@"mm\:ss");          
            });
            //LiveSplit.Model.Input.KeyboardHook.RegisterHotKey(System.Windows.Forms.Keys.Space);
        }

        public void DebugBeep()
        {
            Console.Beep();
        }

        void Hook_OnKeyPress(object sender, LiveSplit.Model.Input.KeyOrButton e)
        {

            //Console.WriteLine("pressing triggered");
            
            Action action = () =>
            {
                if(Keys.Space == e.Key)
                {
                    if (MainStopwatch.IsRunning) Split();
                    else StartSplitting();
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
            /*if (e.Key == System.Windows.Input.Key.Space)
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
            */
            if(e.Key == System.Windows.Input.Key.M)
            if (e.Key == System.Windows.Input.Key.Up)
            {
                ScrollUp();
            }
            if (e.Key == System.Windows.Input.Key.Down)
            {
                ScrollDown();
            }
            

        }


        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if(e.Delta > 0)
            {
                ScrollUp();
            }
            if(e.Delta < 0)
            {
                ScrollDown();
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
            SegmentStopwatch.Start();
            MainRefreshTimer = new Timer();
            MainRefreshTimer.Interval = 50;
            MainRefreshTimer.Tick += new EventHandler(OnMainRefreshTimer);
            MainRefreshTimer.Start();
            currentTrackIndex = 0;
            currentSplitProgress = 0;
            lastSplit = TimeSpan.Zero;
            TrackLogo1.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        void Split()
        {
            if (!isPendingTrackSelection)
            {
                TimeSpan TempTimeSpan = MainStopwatch.Elapsed;
                SegmentStopwatch.Stop();
                if (currentTrackIndex == 15)
                {
                    //setting the Trackinfo in the TrackOrder Array
                    currentTrackOrder[15] = currentTrackIndex;

                    //saving the actual Splitted time in the Splittime Array
                    Splittimes[currentTrackIndex] = SegmentStopwatch.Elapsed;
                    splitTimesInRun[currentTrackIndex] = TempTimeSpan;

                    MainStopwatch.Stop();
                    TimerLabel.Content = SegmentStopwatch.Elapsed.ToString(@"mm\:ss");
                    MainRefreshTimer.Dispose();
                }
                else
                {
                    //setting the Trackinfo in the TrackOrder Array
                    currentTrackOrder[currentSplitProgress] = currentTrackIndex;

                    //saving the actual Splitted time in the Splittime Array
                    Splittimes[currentTrackIndex] = SegmentStopwatch.Elapsed;
                    splitTimesInRun[currentTrackIndex] = TempTimeSpan;

                    lastSplit = SegmentStopwatch.Elapsed;



                    SegmentStopwatch.Reset();
                    SegmentStopwatch.Start();
                }

                //checking if Gold Split happenend      
                if (Splittimes[currentTrackIndex] < GoldSplits[currentTrackIndex])
                {
                    //Setting Gold Tag for Display function
                    isSplitAGoldSplit[currentTrackIndex] = true;

                    //caching Gold Split in Array
                    UnsavedGoldSplits[currentTrackIndex] = Splittimes[currentTrackIndex];
                }

                ScrollOffset = 0;
                currentSplitProgress++;
                isPendingTrackSelection = true;
                UpdateLabels();
            }
        }

        void Reset()
        {
            MainStopwatch.Stop();
            if (MainRefreshTimer != null) MainRefreshTimer.Dispose();
            TimerLabel.Content = ("00:00");
            currentTrackOrder = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            foreach (Image stuff in trackSelectionImages) stuff.Visibility = System.Windows.Visibility.Visible;
            currentSplitProgress = 0;
            currentTrackIndex = 0;
        }


        #endregion
/*
        #region LoadandSave

        void LoadInXMLFile(string givenFilePath)
        {
            try
            {
                saveFile = XDocument.Load(givenFilePath);
            }
            catch (System.IO.FileNotFoundException e)
            {
                saveFile = XDocument.Parse
                    (@"
                    <MKDD Splitter V2>
                    ");
            }
        }

        void LoadSplits()
        {
            
        }

        void SaveSplits()
        {

        }

        void LoadGolds()
        {

        }

        void SaveGolds()
        {

        }

        void LoadMiscellaneous()
        {

        }

        void SaveMiscellaneous()
        {

        }

        #endregion
    */
        #region ScrollSplits

        void UpdateLabels()
        {
            for(int i = 0; i <= 6; i++)
            {
                if (currentSplitProgress -1 >= 6)
                {
                    //Track Initials
                    switch (currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]) //currentSplitProgress -1 -6 + i + ScrollOffset  == 
                    {
                        case 0:
                            SplitLabelArray[i, 0].Content = "LC";
                            break;
                        case 1:
                            SplitLabelArray[i, 0].Content = "PB";
                            break;
                        case 2:
                            SplitLabelArray[i, 0].Content = "BP";
                            break;
                        case 3:
                            SplitLabelArray[i, 0].Content = "DDD";
                            break;
                        case 4:
                            SplitLabelArray[i, 0].Content = "MB";
                            break;
                        case 5:
                            SplitLabelArray[i, 0].Content = "MaC";
                            break;
                        case 6:
                            SplitLabelArray[i, 0].Content = "DC";
                            break;
                        case 7:
                            SplitLabelArray[i, 0].Content = "WS";
                            break;
                        case 8:
                            SplitLabelArray[i, 0].Content = "SL";
                            break;
                        case 9:
                            SplitLabelArray[i, 0].Content = "MuC";
                            break;
                        case 10:
                            SplitLabelArray[i, 0].Content = "YC";
                            break;
                        case 11:
                            SplitLabelArray[i, 0].Content = "DKM";
                            break;
                        case 12:
                            SplitLabelArray[i, 0].Content = "WC";
                            break;
                        case 13:
                            SplitLabelArray[i, 0].Content = "DDJ";
                            break;
                        case 14:
                            SplitLabelArray[i, 0].Content = "BC";
                            break;
                        case 15:
                            SplitLabelArray[i, 0].Content = "RRR";
                            break;
                        default:
                            SplitLabelArray[i, 0].Content = "Error";
                            break;
                    }

                    //Gold Delta
                    if (GoldSplits[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] != TimeSpan.Zero)
                    {
                        if (splitTimesInRun[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] >= GoldSplits[currentSplitProgress -1 -6 + i + ScrollOffset])
                        {
                            SplitLabelArray[i, 1].Content = (splitTimesInRun[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] - GoldSplits[currentSplitProgress -1 -6 + i + ScrollOffset]).ToString(@"\+\ ss\:f");
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                        }
                        else
                        {
                            SplitLabelArray[i, 1].Content = (GoldSplits[currentSplitProgress -1 -6 + i + ScrollOffset] - splitTimesInRun[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]]).ToString(@"\-\ ss\:f");
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Gold;
                        }
                    }
                    else
                    {
                        SplitLabelArray[i, 1].Content = "-";
                        SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                    }

                    //Update PB Difference Label
                    if (PBSplits[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] > TimeSpan.Zero)
                    {
                        if (splitTimesInRun[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] > PBSplits[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]])
                        {
                            SplitLabelArray[i, 2].Content = (splitTimesInRun[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] - PBSplits[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]]).ToString(@"\+\ ss\:f");
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Red;
                        }
                        else
                        {
                            SplitLabelArray[i, 2].Content = (PBSplits[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] - splitTimesInRun[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]]).ToString(@"\-\ ss\:f");
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Green;
                        }
                    }
                    else
                    {
                        SplitLabelArray[i, 2].Content = "-";
                    }

                    //Update Split Time Label
                    SplitLabelArray[i, 3].Content = splitTimesInRun[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]].ToString(@"mm\:ss\:f");

                }
                else
                {
                    if (i <= currentSplitProgress -1)
                    {
                        //Track Initials
                        switch (currentTrackOrder[i])
                        {
                            case 0:
                                SplitLabelArray[i, 0].Content = "LC";
                                break;
                            case 1:
                                SplitLabelArray[i, 0].Content = "PB";
                                break;
                            case 2:
                                SplitLabelArray[i, 0].Content = "BP";
                                break;
                            case 3:
                                SplitLabelArray[i, 0].Content = "DDD";
                                break;
                            case 4:
                                SplitLabelArray[i, 0].Content = "MB";
                                break;
                            case 5:
                                SplitLabelArray[i, 0].Content = "MaC";
                                break;
                            case 6:
                                SplitLabelArray[i, 0].Content = "DC";
                                break;
                            case 7:
                                SplitLabelArray[i, 0].Content = "WS";
                                break;
                            case 8:
                                SplitLabelArray[i, 0].Content = "SL";
                                break;
                            case 9:
                                SplitLabelArray[i, 0].Content = "MuC";
                                break;
                            case 10:
                                SplitLabelArray[i, 0].Content = "YC";
                                break;
                            case 11:
                                SplitLabelArray[i, 0].Content = "DKM";
                                break;
                            case 12:
                                SplitLabelArray[i, 0].Content = "WC";
                                break;
                            case 13:
                                SplitLabelArray[i, 0].Content = "DDJ";
                                break;
                            case 14:
                                SplitLabelArray[i, 0].Content = "BC";
                                break;
                            case 15:
                                SplitLabelArray[i, 0].Content = "RRR";
                                break;
                            default:
                                SplitLabelArray[i, 0].Content = "Error";
                                break;
                        }

                        //Gold Delta
                        if (GoldSplits[currentTrackOrder[i]] != TimeSpan.Zero)
                        {
                            if (splitTimesInRun[currentTrackOrder[i]] >= GoldSplits[currentTrackOrder[i]])
                            {
                                SplitLabelArray[i, 1].Content = (splitTimesInRun[currentTrackOrder[i]] - GoldSplits[currentTrackOrder[i]]).ToString(@"\+\ ss\:f");
                                SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                            }
                            else
                            {
                                SplitLabelArray[i, 1].Content = (GoldSplits[currentTrackOrder[i]] - splitTimesInRun[currentTrackOrder[i]]).ToString(@"\-\ ss\:f");
                                SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Gold;
                            }
                        }
                        else
                        {
                            SplitLabelArray[i, 1].Content = "-";
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                        }

                        //Update PB Difference Label
                        if (PBSplits[currentTrackOrder[i]] > TimeSpan.Zero)
                        {
                            if (splitTimesInRun[currentTrackOrder[i]] > PBSplits[currentTrackOrder[i]])
                            {
                                SplitLabelArray[i, 2].Content = (splitTimesInRun[currentTrackOrder[i]] - PBSplits[currentTrackOrder[i]]).ToString(@"\+\ ss\:f");
                                SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Red;
                            }
                            else
                            {
                                SplitLabelArray[i, 2].Content = (PBSplits[currentTrackOrder[i]] - splitTimesInRun[currentTrackOrder[i]]).ToString(@"\-\ ss\:f");
                                SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Green;
                            }
                        }
                        else
                        {
                            SplitLabelArray[i, 2].Content = "-";
                        }

                        //Update Split Time Label
                        SplitLabelArray[i, 3].Content = splitTimesInRun[currentTrackOrder[i]].ToString(@"mm\:ss\:f");
                    }
                    else
                    {
                        SplitLabelArray[i, 0].Content = "-";
                        SplitLabelArray[i, 1].Content = "-";
                        SplitLabelArray[i, 2].Content = "-";
                        SplitLabelArray[i, 3].Content = "-";
                    }
                }

            }



        }



        void ScrollUp()
        {
            if (currentSplitProgress -1 > 6)
            {
                if ((currentSplitProgress -1 + ScrollOffset) > 6)
                {
                    ScrollOffset--;
                    UpdateLabels();
                }
            }
        }

        void ScrollDown()
        {
            if (ScrollOffset < 0)
            {
                ScrollOffset++;
                UpdateLabels();
            }          
        }

       

        void ResetScroll()
        {

        }

        #endregion

        #region TrackImagesEvents
        private void TrackLogo1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 0;
            TrackLogo1.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo1_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo1.Opacity = 1;
        }
        
        private void TrackLogo1_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo1.Opacity = .8;
        }

        private void TrackLogo2_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 1;
            TrackLogo2.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo2_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo2.Opacity = 1;
        }

        private void TrackLogo2_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo2.Opacity = .8;
        }

        private void TrackLogo3_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 2;
            TrackLogo3.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo3_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo3.Opacity = 1;
        }

        private void TrackLogo3_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo3.Opacity = .8;
        }

        private void TrackLogo4_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 3;
            TrackLogo4.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo4_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo4.Opacity = 1;
        }

        private void TrackLogo4_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo4.Opacity = .8;
        }

        private void TrackLogo5_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 4;
            TrackLogo5.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo5_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo5.Opacity = 1;
        }

        private void TrackLogo5_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo5.Opacity = .8;
        }

        private void TrackLogo6_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 5;
            TrackLogo6.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo6_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo6.Opacity = 1;
        }

        private void TrackLogo6_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo6.Opacity = .8;
        }

        private void TrackLogo7_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 6;
            TrackLogo7.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo7_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo7.Opacity = 1;
        }

        private void TrackLogo7_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo7.Opacity = .8;
        }

        private void TrackLogo8_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 7;
            TrackLogo8.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo8_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo8.Opacity = 1;
        }

        private void TrackLogo8_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo8.Opacity = .8;
        }

        private void TrackLogo9_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 8;
            TrackLogo9.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo9_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo9.Opacity = 1;
        }

        private void TrackLogo9_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo9.Opacity = .8;
        }

        private void TrackLogo10_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 9;
            TrackLogo10.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo10_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo10.Opacity = 1;
        }

        private void TrackLogo10_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo10.Opacity = .8;
        }

        private void TrackLogo11_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 10;
            TrackLogo11.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo11_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo11.Opacity = 1;
        }

        private void TrackLogo11_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo11.Opacity = .8;
        }

        private void TrackLogo12_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 11;
            TrackLogo12.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo12_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo12.Opacity = 1;
        }

        private void TrackLogo12_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo12.Opacity = .8;
        }

        private void TrackLogo13_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 12;
            TrackLogo13.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo13_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo13.Opacity = 1;
        }

        private void TrackLogo13_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo13.Opacity = .8;
        }

        private void TrackLogo14_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 13;
            TrackLogo14.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo14_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo14.Opacity = 1;
        }

        private void TrackLogo14_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo14.Opacity = .8;
        }

        private void TrackLogo15_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 14;
            TrackLogo15.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo15_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo15.Opacity = 1;
        }

        private void TrackLogo15_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo15.Opacity = .8;
        }

        private void TrackLogo16_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 15;
            TrackLogo16.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
        }

        private void TrackLogo16_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo16.Opacity = 1;
        }

        private void TrackLogo16_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TrackLogo16.Opacity = 0.8;
            
        }
        #endregion

    }
}
