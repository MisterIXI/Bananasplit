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
using System.Xml;


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

        #region SettingsVariables

        SettingsWindow var_SettingsWindow;

        static Keys splitKeyCode, resetKeyCode, skipSplitKeyCode, undoSelectionKeyCode;

        public static Keys SplitKeyCode
        {
            get
            {
                return splitKeyCode;
            }
            set
            {
                if (splitKeyCode != value)
                {
                    splitKeyCode = value;
                    unsavedChangesFlag = true;
                }
            }
        }
        public static Keys ResetKeyCode
        {
            get
            {
                return resetKeyCode;
            }
            set
            {
                if (resetKeyCode != value)
                {
                    resetKeyCode = value;
                    unsavedChangesFlag = true;
                }
            }
        }
        public static Keys SkipSplitKeyCode
        {
            get
            {
                return skipSplitKeyCode;
            }
            set
            {
                if (skipSplitKeyCode != value)
                {
                    skipSplitKeyCode = value;
                    unsavedChangesFlag = true;
                }
            }
        }
        public static Keys UndoSelectionKeyCode
        {
            get
            {
                return undoSelectionKeyCode;
            }
            set
            {
                if (undoSelectionKeyCode != value)
                {
                    undoSelectionKeyCode = value;
                    unsavedChangesFlag = true;
                }
            }
        }


        static bool useGlobalHotkeys = false, unsavedChangesFlag; 
        public static bool UseGlobalHotkeys
        {
            get
            {
                return useGlobalHotkeys;
            }
            set
            {
                if (useGlobalHotkeys != value)
                {
                    useGlobalHotkeys = value;
                    unsavedChangesFlag = true;
                }
            }
        }



        /*

            var_SettingsWindow


        useGlobalHotkeys: Should the Hotkeys be globally reachable
        unsavedChangesFlag: Flag that describes if there is 
        */

        #endregion

        #region XMLVariables

        public static XDocument saveFile;
        public static XElement saveFileElement;
        public static string defaultSaveFileName = "BananaSplit_SaveFile.xml";//C:\\temp\\BananaSplit_SaveFile.xml

        #endregion

        #region SplitterVariables
        //Splitter Variables
        int currentTrackIndex, currentSplitProgress = 0;

        int[] currentTrackOrder = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        TimeSpan[] Splittimes = new TimeSpan[16], PBSplits = new TimeSpan[16], UnsavedGoldSplits = new TimeSpan[16], GoldSplits = new TimeSpan[16], currentTotalTimes = new TimeSpan[16], PBTotalTimes = new TimeSpan[16];
        TimeSpan lastSplit, totalCurrentRunTime, totalCurrentPBTime, PBEndTime, currentEndTime;
        bool[] isSplitAGoldSplit = new bool[16];
        bool isPendingTrackSelection, isPBMissingSplits, isPBMissingGoldSplits, wasLastSplitSkipped, waitingForFirstSplit;

        /*
        Variable explanation:

        currentTrackindex: chosen Track to split on when the splitbutton is pressed (0-15)
        currentSplitProgress: # of Splits splittet in the run

        currentTrackOrder: Index of Track in place

        Splittimes: Timespans of current splittet Splits (with correct Index e.g. 14 = BC)
        PBSplits: Timespans of saved & loaded PBSplits
        UnsavedGoldSplits: Timespans of eventual GoldSPlits ready to be saved or discarded
        GoldSplits: Timespans of saved & loaded GoldSPlits
        currentTotalTime: Sum of splits happenend up to this point for comparison Reasons (still in correct Track Order == 0:Luigi Circuit; 1:Peach Beach; 15:Rainbow Road...)
        PBTotalTime: same as above, but with the PB Splits so both can compare to each other

        lastSplit: Timespans (point in the run) where the last Split happenend for comparison
        totalCurrentRunTime: A Sum of all current Splits for comparison reasons
        totalCurrentPBTime: A Sum of all tracks that were splitted but with PBTime
        PBEndTime: MainStop.Elapsed of PB after the RR Split (actual RTA rating of the PB Run)
        currentEndTime: MainsTopwatch.Elapsed after RR Split

        isSplitAGoldSplit: Set Flag if Split in array Position is a Gold Split

        isPendingTrackSelection: to prevent splitting without picking a track
        isPBMissingSplits: Set Flag if PB is Missing one or some of its Split.
        isPBMissingGoldSplits: Set Flag if one or more Gold Values are missing.
        isCurrentRunMissingSplits: Set Flag if one or more Splits in this run are missing.
        */
        #endregion

        #region MiscellaneousVariables

        int startedRunCount, completedRunCount;

        /*
          
         startedRunCount: count of started runs in total.
         completedRunCount: count of completed runs.
        startedRuns - completed runs = Resets

        */

        #endregion

        #region InterfaceVariables
        //Interface Variables
        int ScrollOffset = 0;
        Image[] trackSelectionImages;
        System.Windows.Controls.Label[,] SplitLabelArray;
        TimeSpan[] PBSplits_afterRunInLabel = new TimeSpan[16], PBTotalTimes_afterRunInLabel = new TimeSpan[16];
        TimeSpan totalCurrentPBTime_afterRunInLabel = TimeSpan.Zero, PBEndTime_afterRunInLabel = TimeSpan.Zero;

        /*

            
        Variable Explanation:
        ScrollOffset: -9 > x > 1 (= int has to be in between -8 and 0 (-8 and 0 are included)) This describes the offset of the scrollview. (e.g. -1 is one scrolled up) 

        trackSelecitonImages: A collection of all 16 Track images used to select the tracks.
    
        SplitLabelArray: All 7 Label rows in Array form to go through them in a for loop.

        unsavedChangesFlag: Flag that describes if there is 
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

            SplitLabelArray = new System.Windows.Controls.Label[,] {
                { L_TrackID1, L_GoldDiff1, L_SplitDiff1, L_SplitT1 },
                { L_TrackID2, L_GoldDiff2, L_SplitDiff2, L_SplitT2 },
                { L_TrackID3, L_GoldDiff3, L_SplitDiff3, L_SplitT3 },
                { L_TrackID4, L_GoldDiff4, L_SplitDiff4, L_SplitT4 },
                { L_TrackID5, L_GoldDiff5, L_SplitDiff5, L_SplitT5 },
                { L_TrackID6, L_GoldDiff6, L_SplitDiff6, L_SplitT6 },
                { L_TrackID7, L_GoldDiff7, L_SplitDiff7, L_SplitT7 }};
            trackSelectionImages = new Image[] { TrackLogo1, TrackLogo2, TrackLogo3, TrackLogo4, TrackLogo5, TrackLogo6, TrackLogo7, TrackLogo8, TrackLogo9, TrackLogo10, TrackLogo11, TrackLogo12, TrackLogo13, TrackLogo14, TrackLogo15, TrackLogo16 };
            LoadInXMLFile(defaultSaveFileName);
            LoadSplits();
            LoadMiscellaneous();
            currentTrackOrder = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            if (!isPBMissingSplits)
            {
                for (int i = 0; i < 16; i++)
                {
                    if(i == 0)
                    {
                        currentTotalTimes[i] = PBSplits[i];
                    }
                    else
                    {
                        currentTotalTimes[i] = currentTotalTimes[i - 1] + PBSplits[i];
                    }                    
                }
            }
            currentSplitProgress = 16;
            ScrollOffset = -9;
            LoadSettings();
            PBSplits_afterRunInLabel = PBSplits;
            Splittimes = PBSplits;
            PBTotalTimes_afterRunInLabel = currentTotalTimes;
            Previous_Segment_Label_Number.Content = "-";
            if (useGlobalHotkeys)
            {
                RegisterAllHotkeys();
            }
            ProgramInfoLabel.Content = "Press split to begin run.";
            UpdateLabels();
        }

        private void OnMainRefreshTimer(object source, EventArgs e)
        {

            //TestLabel.Content = "ficl ypi aföö";//string.Format(@"mm\:ss\.fff",MainStopwatch.Elapsed);
            this.Dispatcher.Invoke(() =>
            {
                TimerLabel.Content = MainStopwatch.Elapsed.ToString(@"mm\:ss");
            });
            //LiveSplit.Model.Input.KeyboardHook.RegisterHotKey(System.Windows.Forms.Keys.Space);
        }

        public void DebugBeep()
        {
            Console.Beep();
        }

        #region KeyFunctions
        void RegisterAllHotkeys()
        {
            if(splitKeyCode != default(Keys))Hook.RegisterHotKey(splitKeyCode);
            if (resetKeyCode != default(Keys)) Hook.RegisterHotKey(resetKeyCode);
            if (skipSplitKeyCode != default(Keys)) Hook.RegisterHotKey(skipSplitKeyCode);
            if (undoSelectionKeyCode != default(Keys)) Hook.RegisterHotKey(undoSelectionKeyCode);
        }

        void SplitKey()
        {
            if (MainStopwatch.IsRunning) Split();
            else if (MainStopwatch.Elapsed == TimeSpan.Zero) StartSplitting();
        }

        void ResetKey()
        {

            if (CheckIfGolds())
            {
                switch (System.Windows.MessageBox.Show("You have beaten some of your best times. Do you want to update them?", "Unsaved Times", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    case MessageBoxResult.Yes:
                        UpdateGolds();
                        Reset();
                        break;
                    case MessageBoxResult.No:
                        isSplitAGoldSplit = new bool[16];
                        UnsavedGoldSplits = new TimeSpan[16];
                        Reset();
                        break;
                    case MessageBoxResult.Cancel:
                        break;
                }

            }
            else Reset();

        }

        void UndoSelectionKey()
        {
            
            trackSelectionImages[currentTrackIndex].IsEnabled = true;
            trackSelectionImages[currentTrackIndex].Opacity = .5;
            isPendingTrackSelection = true;
            ProgramInfoLabel.Content = "Select the correct track now.";
            currentTrackIndex = -1;
        }

        void SkipSplitKey()
        {
            SkipSplit();
        }

        void LoadKey()
        {
            LoadInXMLFile(defaultSaveFileName);
            LoadSplits();
            Console.WriteLine(saveFileElement);
        }

        void OnHotkeyPoll(object sender, EventArgs e)
        {
            HookInstance.Poll();
        }

        void SaveKey()
        {
            UpdateSettings();
            UpdateGolds();
            UpdateRecords();
            UpdateMiscellaneous();
            SaveTheSaveFile();
            System.Windows.MessageBox.Show("All changes have been saved.", "Save Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion



        void Hook_OnKeyPress(object sender, LiveSplit.Model.Input.KeyOrButton e)
        {
            if (useGlobalHotkeys)
            {
                Action action = () =>
                {
                    if (e.Key == splitKeyCode) SplitKey();
                    if (e.Key == resetKeyCode) ResetKey();
                    if (e.Key == skipSplitKeyCode) SkipSplitKey();
                    if (e.Key == undoSelectionKeyCode) UndoSelectionKey();
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
        }

        #region WindowEvents

        private void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!useGlobalHotkeys)
            {
                if (e.Key == System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)splitKeyCode)) SplitKey();
                if (e.Key == System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)resetKeyCode)) ResetKey();
                if (e.Key == System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)skipSplitKeyCode)) SkipSplitKey();
                if (e.Key == System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)undoSelectionKeyCode)) UndoSelectionKey();
                
            }

            if (e.Key == System.Windows.Input.Key.S && System.Windows.Forms.Control.ModifierKeys == Keys.Control) SaveKey();
        }


        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ScrollUp();
            }
            if (e.Delta < 0)
            {
                ScrollDown();
            }

        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        void on_Window_Close(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CheckIfGolds())
            {
                switch (System.Windows.MessageBox.Show("There are not yet updated gold splits. Do you want to update them and save everything to the save file?", "Save Times?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    case MessageBoxResult.Yes:
                        UpdateGolds();
                        UpdateRecords();
                        UpdateSettings();
                        UpdateMiscellaneous();
                        SaveTheSaveFile();
                        e.Cancel = false;
                        break;
                    case MessageBoxResult.No:
                        e.Cancel = false;
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;


                }
            }
            else if (unsavedChangesFlag)
                switch (System.Windows.MessageBox.Show("There are unsaved changes. Do you want to save them to the save file?", "Save Splits?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    case MessageBoxResult.Yes:
                        UpdateSettings();
                        UpdateRecords();
                        UpdateMiscellaneous();
                        SaveTheSaveFile();
                        e.Cancel = false;
                        break;
                    case MessageBoxResult.No:
                        e.Cancel = false;
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
        }

        private void ContextMenueSettingClick(object sender, RoutedEventArgs e)
        {
            var_SettingsWindow = new SettingsWindow();
            var_SettingsWindow.ShowDialog();
        }

        private void ContextMenueAboutClick(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("BananaSplit Version 1.0" + Environment.NewLine + "Programmed by Yannik Brändle (aka MisterIXI)" + Environment.NewLine + Environment.NewLine + "For questions, bug reports or anything else regarding this splitter, just send an e-mail at misterixi@t-online.de or contact me on Twitch or Youtube." + Environment.NewLine + Environment.NewLine + "Big shoutouts to GoombaNL for the great help during the development of this program." + Environment.NewLine + Environment.NewLine + "Thank you, Livesplit, for being open-source. You gave Inspiration to the design and the added global hotkey functionality.", "BananaSplit About", MessageBoxButton.OK, MessageBoxImage.None);
        }

        private void ContextMenueCloseClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();                        
        }


        #endregion

        #region Splitting Functions

        void StartSplitting()
        {
            ResetVariables();
            MainStopwatch.Start();
            SegmentStopwatch.Start();
            MainRefreshTimer = new Timer();
            MainRefreshTimer.Interval = 50;
            MainRefreshTimer.Tick += new EventHandler(OnMainRefreshTimer);
            MainRefreshTimer.Start();
            currentSplitProgress = 0;
            lastSplit = TimeSpan.Zero;
            TrackLogo1.Opacity = .2;
            totalCurrentRunTime = TimeSpan.Zero;
            totalCurrentPBTime = TimeSpan.Zero;
            ProgramInfoLabel.Content = "Press splitkey to split on Luigi Circuit.";
            isPendingTrackSelection = true;
            startedRunCount++;
            unsavedChangesFlag = true;
            UpdateLabels();

            waitingForFirstSplit = true;
            System.Windows.Threading.DispatcherTimer leTimer = new System.Windows.Threading.DispatcherTimer();
            leTimer.Interval = TimeSpan.FromSeconds(3);
            leTimer.Tag = new Action(delegate { GeneralTrackLogo_MouseDown(0); });
            leTimer.Tick += new EventHandler(leTimer_Tick);
            leTimer.Start();
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
                    currentTotalTimes[currentTrackIndex] = TempTimeSpan;
                    currentEndTime = TempTimeSpan;

                    lastSplit = SegmentStopwatch.Elapsed;

                    MainStopwatch.Stop();

                    MainRefreshTimer.Dispose();

                    PBTotalTimes_afterRunInLabel = PBTotalTimes;
                    PBSplits_afterRunInLabel = PBSplits;
                    completedRunCount++;
                    unsavedChangesFlag = true;
                }
                else
                {
                    //setting the Trackinfo in the TrackOrder Array
                    currentTrackOrder[currentSplitProgress] = currentTrackIndex;

                    //saving the actual Splitted time in the Splittime Array
                    Splittimes[currentTrackIndex] = SegmentStopwatch.Elapsed;
                    currentTotalTimes[currentTrackIndex] = TempTimeSpan;

                    lastSplit = SegmentStopwatch.Elapsed;



                    SegmentStopwatch.Reset();
                    SegmentStopwatch.Start();
                }

                //checking if Gold Split happenend      
                if (Splittimes[currentTrackIndex] < GoldSplits[currentTrackIndex] || GoldSplits[currentTrackIndex] == TimeSpan.Zero)
                {
                    //Setting Gold Tag for Display function
                    isSplitAGoldSplit[currentTrackIndex] = true;

                    //caching Gold Split in Array
                    UnsavedGoldSplits[currentTrackIndex] = Splittimes[currentTrackIndex];
                }

                //Updating Timeloss/Timesave in Previous Segment
                if(PBSplits[currentTrackIndex] != TimeSpan.Zero)
                {
                    if(PBSplits[currentTrackIndex] > Splittimes[currentTrackIndex])
                    {
                        Previous_Segment_Label_Number.Content = "- " + (Splittimes[currentTrackIndex] - PBSplits[currentTrackIndex]).ToString(@"ss\.f");
                    }
                    else
                    {
                        Previous_Segment_Label_Number.Content = "+ " + (PBSplits[currentTrackIndex] - Splittimes[currentTrackIndex]).ToString(@"ss\.f");
                    }
                }
                else
                {
                    Previous_Segment_Label_Number.Content = "-";
                }

                totalCurrentRunTime += Splittimes[currentTrackIndex];
                totalCurrentPBTime += PBSplits[currentTrackIndex];
                currentTotalTimes[currentTrackIndex] = totalCurrentRunTime;
                if (!isPBMissingSplits)
                {
                    PBTotalTimes[currentTrackIndex] = totalCurrentPBTime;
                }

                ScrollOffset = 0;
                currentSplitProgress++;
                isPendingTrackSelection = true;
                currentTrackIndex = -1;
                ProgramInfoLabel.Content = "Select the next track.";
                if (!MainStopwatch.IsRunning)if (CheckIfPB()) UpdatePB();

                UpdateLabels();

            }
        }

        void SkipSplit()
        {
            if (!isPendingTrackSelection)
            {
                TimeSpan TempTimeSpan = MainStopwatch.Elapsed;
                if (currentTrackIndex != 15)
                {
                    //setting the Trackinfo in the TrackOrder Array
                    currentTrackOrder[currentSplitProgress] = currentTrackIndex;

                    //saving the actual Splitted time in the Splittime Array
                    Splittimes[currentTrackIndex] = TimeSpan.Zero;
                    currentTotalTimes[currentTrackIndex] = TimeSpan.Zero;

                    lastSplit = TimeSpan.Zero;




                }


                    Previous_Segment_Label_Number.Content = "Skipped";

                totalCurrentPBTime += PBSplits[currentTrackIndex];
                currentTotalTimes[currentTrackIndex] = TimeSpan.Zero;
                if (!isPBMissingSplits)
                {
                    PBTotalTimes[currentTrackIndex] = totalCurrentPBTime;
                }

                ScrollOffset = 0;
                currentSplitProgress++;
                isPendingTrackSelection = true;
                currentTrackIndex = -1;
                ProgramInfoLabel.Content = "Select the next track.";
                UpdateLabels();
                wasLastSplitSkipped = true;

            }
        }

        bool CheckIfPB()
        {
            bool tempCheckingBool = false;
            if (PBEndTime > currentEndTime && currentEndTime > TimeSpan.Zero)
            {
                tempCheckingBool = true;
            }
            else if (SumUpTimeArray(PBSplits) == TimeSpan.Zero || PBEndTime == TimeSpan.Zero)
            {
                tempCheckingBool = true;
            }
            if (tempCheckingBool)
            {
                ProgramInfoLabel.Content = "Congratulations!";
                return true;
            }
            else
            {
                ProgramInfoLabel.Content = "Better luck next time!";
                return false;
            }
        }

        bool CheckIfGolds()
        {
            bool tempCheckingBool = false;
            for (int i = 0; i <= 15; i++)
            {
                if (isSplitAGoldSplit[i])
                {
                    tempCheckingBool = true;
                }
            }
            if (tempCheckingBool) return true;
            else return false;
        }

        void UpdatePB()
        {

            PBSplits = Splittimes;
            PBTotalTimes = currentTotalTimes;
            totalCurrentPBTime = totalCurrentRunTime;
            PBEndTime = currentEndTime;
            unsavedChangesFlag = true;
            UpdateRecords();
        }

        void Reset()
        {

            MainStopwatch.Stop();
            MainStopwatch.Reset();
            if (MainRefreshTimer != null) MainRefreshTimer.Dispose();
            TimerLabel.Content = ("00:00");
            ProgramInfoLabel.Content = ("Press split to begin run.");            
            isPendingTrackSelection = false;            
            currentSplitProgress = 16;
            currentTrackOrder = new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            Splittimes = PBSplits;
            totalCurrentPBTime = TimeSpan.Zero;
            for (int i = 0; i < 16; i++)
            {
                totalCurrentPBTime += PBSplits[i];
                PBTotalTimes[i] = totalCurrentPBTime;
            }
            foreach (Image stuff in trackSelectionImages)
            {
                stuff.IsEnabled = false;
                stuff.Opacity = .5;
            }
            currentTotalTimes = PBTotalTimes;
            isSplitAGoldSplit = new bool[16];
            ScrollOffset = -9;
            currentEndTime = TimeSpan.Zero;
            PBTotalTimes_afterRunInLabel = new TimeSpan[16];
            Previous_Segment_Label_Number.Content = "";
            UpdateLabels();
                
        }

        void ResetVariables()
        {
            waitingForFirstSplit = false;
            SegmentStopwatch.Reset();
            MainStopwatch.Reset();
            currentSplitProgress = 0;
            currentTrackIndex = 0;
            currentTrackOrder = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            Splittimes = new TimeSpan[16];
            isSplitAGoldSplit = new bool[16];
            if (MainRefreshTimer != null) MainRefreshTimer.Dispose();
            TimerLabel.Content = ("00:00");
            PBTotalTimes = new TimeSpan[16];
            currentTotalTimes = new TimeSpan[16];            
            totalCurrentRunTime = TimeSpan.Zero;
            totalCurrentPBTime = TimeSpan.Zero;
        }

        #endregion

        #region LoadandSave

        void LoadInXMLFile(string givenFilePath)
        {
            if (System.IO.File.Exists(givenFilePath))
            {
                saveFile = XDocument.Load(givenFilePath);
                saveFileElement = saveFile.Root;               
            }
            else
            {
                saveFileElement = new XElement("SaveFile", new XElement("Times", new XAttribute("TotalRunTime", TimeSpan.Zero)), new XElement("Settings"), new XElement("Miscellaneous"));
                saveFile = new XDocument(saveFileElement);
                for (int i = 0; i <= 15; i++)
                {
                    saveFileElement.Element("Times").Add(new XElement("Track" + i.ToString(), new XAttribute("PBTime", TimeSpan.Zero), new XAttribute("GoldTime", TimeSpan.Zero)));
                }
            }

        }

        void LoadSplits()
        {
            try
            {
                PBEndTime = XmlConvert.ToTimeSpan(saveFileElement.Element("Times").Attribute("TotalRunTime").Value);
            }
            catch (NullReferenceException)
            {
                PBEndTime = TimeSpan.Zero;
            }
            isPBMissingSplits = false;
            isPBMissingGoldSplits = false;
            for (int i = 0; i <= 15; i++)
            {
                PBSplits[i] = TimeSpan.Zero;
                GoldSplits[i] = TimeSpan.Zero;
                try
                {
                    PBSplits[i] = XmlConvert.ToTimeSpan(saveFileElement.Element("Times").Element("Track" + i.ToString()).Attribute("PBTime").Value);
                    GoldSplits[i] = XmlConvert.ToTimeSpan(saveFileElement.Element("Times").Element("Track" + i.ToString()).Attribute("GoldTime").Value);
                }
                catch (NullReferenceException)
                {

                }
                if (GoldSplits[i] == TimeSpan.Zero) isPBMissingGoldSplits = true;
                if (PBSplits[i] == TimeSpan.Zero) isPBMissingSplits = true;
            }
        }

        void LoadSettings()
        {
            if (saveFileElement.Element("Settings").Element("Hotkeys") != null)
            {
                if (saveFileElement.Element("Settings").Element("Hotkeys").Element("Split").Attribute("Primary") != null && saveFileElement.Element("Settings").Element("Hotkeys").Element("Split").Attribute("Primary").Value != null)
                {
                    splitKeyCode = (Keys)XmlConvert.ToInt32(saveFileElement.Element("Settings").Element("Hotkeys").Element("Split").Attribute("Primary").Value);
                }
                else splitKeyCode = Keys.NumPad1;
                if (saveFileElement.Element("Settings").Element("Hotkeys").Element("Reset").Attribute("Primary") != null && saveFileElement.Element("Settings").Element("Hotkeys").Element("Reset").Attribute("Primary").Value != null)
                {
                    resetKeyCode = (Keys)XmlConvert.ToInt32(saveFileElement.Element("Settings").Element("Hotkeys").Element("Reset").Attribute("Primary").Value);
                }
                else resetKeyCode = Keys.NumPad3;
                if (saveFileElement.Element("Settings").Element("Hotkeys").Element("SkipSplit").Attribute("Primary") != null && saveFileElement.Element("Settings").Element("Hotkeys").Element("SkipSplit").Attribute("Primary").Value != null)
                {
                    skipSplitKeyCode = (Keys)XmlConvert.ToInt32(saveFileElement.Element("Settings").Element("Hotkeys").Element("SkipSplit").Attribute("Primary").Value);
                }
                else skipSplitKeyCode = Keys.NumPad1;
                if (saveFileElement.Element("Settings").Element("Hotkeys").Element("UndoSelection").Attribute("Primary") != null && saveFileElement.Element("Settings").Element("Hotkeys").Element("UndoSelection").Attribute("Primary").Value != null)
                {
                    undoSelectionKeyCode = (Keys)XmlConvert.ToInt32(saveFileElement.Element("Settings").Element("Hotkeys").Element("UndoSelection").Attribute("Primary").Value);
                }
                else undoSelectionKeyCode = Keys.NumPad8;
            }
            else
            {
                saveFileElement.Element("Settings").Add(new XElement("Hotkeys",
                    new XElement("Split", new XAttribute("Primary", (int)Keys.NumPad1)),
                    new XElement("Reset", new XAttribute("Primary", (int)Keys.NumPad3)),
                    new XElement("SkipSplit", new XAttribute("Primary", (int)Keys.NumPad2)),
                    new XElement("UndoSelection", new XAttribute("Primary", (int)Keys.NumPad8))));
                splitKeyCode = Keys.NumPad1;
                resetKeyCode = Keys.NumPad3;
                skipSplitKeyCode = Keys.NumPad1;
                undoSelectionKeyCode = Keys.NumPad8;
            }
            if (saveFileElement.Element("Settings").Element("useGlobalHotkeys") != null)
            {
                if (saveFileElement.Element("Settings").Element("useGlobalHotkeys").Attribute("Enabled") != null && saveFileElement.Element("Settings").Element("useGlobalHotkeys").Attribute("Enabled").Value != null)
                {
                    useGlobalHotkeys = XmlConvert.ToBoolean(saveFileElement.Element("Settings").Element("useGlobalHotkeys").Attribute("Enabled").Value);
                }
                else useGlobalHotkeys = false;
            }
            else
            {
                saveFileElement.Element("Settings").Add(new XElement("useGlobalHotkeys", new XAttribute("Enabled", false)));
                useGlobalHotkeys = false;
            }
        }

        void LoadMiscellaneous()
        {
            if(saveFileElement.Element("Miscellaneous").Element("RunCounter") != null)
            {
                startedRunCount = XmlConvert.ToInt32(saveFileElement.Element("Miscellaneous").Element("RunCounter").Attribute("startedRunCount").Value);
                completedRunCount = XmlConvert.ToInt32(saveFileElement.Element("Miscellaneous").Element("RunCounter").Attribute("completedRunCount").Value);
            }
            else
            {
                saveFileElement.Element("Miscellaneous").Add(new XElement("RunCounter", new XAttribute("startedRunCount", 0), new XAttribute("completedRunCount", 0)));
            }
        }

        void UpdateRecords()
        {
            if(saveFileElement.Element("Times") != null)
            {
                saveFileElement.Element("Times").SetAttributeValue("TotalRunTime", PBEndTime);
                for (int i = 0; i <= 15; i++)
                {
                    if(saveFileElement.Element("Times").Element("Track"+i.ToString()) != null)
                    {
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("PBTime", PBSplits[i]);
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("GoldTime", GoldSplits[i]);
                    }
                    else
                    {
                        saveFileElement.Element("Times").Add(new XElement("Track" + i.ToString()));
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("PBTime", PBSplits[i]);
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("GoldTime", GoldSplits[i]);
                    }
                }
            }
            else
            {
                saveFileElement.Add(new XElement("Times"));
                for (int i = 0; i <= 15; i++)
                {
                    saveFileElement.Element("Times").Add(new XElement("Track" + i.ToString(), new XAttribute("PBTime", PBSplits[i]), new XAttribute("GoldTime", UnsavedGoldSplits[i])));
                }
            }
            isPBMissingSplits = false;
            isPBMissingGoldSplits = false;
            for (int i = 0; i < 16; i++)
            {
                if (GoldSplits[i] == TimeSpan.Zero) isPBMissingGoldSplits = true;
                if (PBSplits[i] == TimeSpan.Zero) isPBMissingSplits = true;
            }
        }

        void UpdateGolds()
        {
            for (int i = 0; i <= 15; i++)
            {
                if (isSplitAGoldSplit[i])
                {
                    GoldSplits[i] = UnsavedGoldSplits[i];
                    unsavedChangesFlag = true;
                }
                isSplitAGoldSplit[i] = false;
                UnsavedGoldSplits[i] = TimeSpan.Zero;
            }
        }


        void UpdateMiscellaneous()
        {
            if(XmlConvert.ToInt32(saveFileElement.Element("Miscellaneous").Element("RunCounter").Attribute("startedRunCount").Value) != startedRunCount || XmlConvert.ToInt32(saveFileElement.Element("Miscellaneous").Element("RunCounter").Attribute("completedRunCount").Value) != completedRunCount)
            {
                saveFileElement.Element("Miscellaneous").Element("RunCounter").Attribute("startedRunCount").SetValue(startedRunCount);
                saveFileElement.Element("Miscellaneous").Element("RunCounter").Attribute("completedRunCount").SetValue(completedRunCount);
                unsavedChangesFlag = true;
            }
        }

        void UpdateSettings()
        {
            saveFileElement.Element("Settings").Element("Hotkeys").Element("Split").SetAttributeValue("Primary", (int)splitKeyCode);
            saveFileElement.Element("Settings").Element("Hotkeys").Element("Reset").SetAttributeValue("Primary", (int)resetKeyCode);
            saveFileElement.Element("Settings").Element("Hotkeys").Element("SkipSplit").SetAttributeValue("Primary", (int)skipSplitKeyCode);
            saveFileElement.Element("Settings").Element("Hotkeys").Element("UndoSelection").SetAttributeValue("Primary", (int)undoSelectionKeyCode);

            saveFileElement.Element("Settings").Element("useGlobalHotkeys").SetAttributeValue("Enabled", useGlobalHotkeys);
        }

        void SaveTheSaveFile()
        {
            saveFile = new XDocument(saveFileElement);
            saveFile.Save(defaultSaveFileName);
            unsavedChangesFlag = false;
        }
        #endregion
    
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
                            SplitLabelArray[i, 0].Content = "RR";
                            break;
                        default:
                            SplitLabelArray[i, 0].Content = "Error";
                            break;
                    }

                    //Gold Delta
                    if (GoldSplits[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] != TimeSpan.Zero)
                    {
                        if (isSplitAGoldSplit[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                        {
                            SplitLabelArray[i, 1].Content = (GoldSplits[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - Splittimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\-\ ss\.f");
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Gold;
                        }
                        else
                        {
                            SplitLabelArray[i, 1].Content = (Splittimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - GoldSplits[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\+\ ss\.f");
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Red;
                        }
                    }
                    else
                    {
                        SplitLabelArray[i, 1].Content = "-";
                        SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                    }

                    //Update PB Difference Label
                    if (MainStopwatch.IsRunning)
                    {
                        if (!isPBMissingSplits)
                        {
                            if (PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > TimeSpan.Zero)
                            {
                                if (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                                {
                                    SplitLabelArray[i, 2].Content = (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\+\ ss\.f");
                                    if (PBSplits[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > Splittimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                                    {
                                        SplitLabelArray[i, 2].Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 165));
                                    }
                                    else
                                    {
                                        SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Red;
                                    }
                                    if (isSplitAGoldSplit[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]) SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Gold;
                                }
                                else if (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] == PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                                {
                                    SplitLabelArray[i, 2].Content = "-";
                                    SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                                }
                                else
                                {
                                    SplitLabelArray[i, 2].Content = (PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\-\ ss\.f");
                                    SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Green;
                                    if (isSplitAGoldSplit[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]) SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Gold;
                                }
                            }
                            else
                            {
                                SplitLabelArray[i, 2].Content = "-";
                                SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                            }
                        }
                        else
                        {
                            SplitLabelArray[i, 2].Content = "-";
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                        }
                    }
                    else
                    {
                        if (!isPBMissingSplits)
                        {
                            if (PBTotalTimes_afterRunInLabel[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > TimeSpan.Zero)
                            {
                                if (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > PBTotalTimes_afterRunInLabel[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                                {
                                    SplitLabelArray[i, 2].Content = (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - PBTotalTimes_afterRunInLabel[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\+\ ss\.f");
                                    if (PBSplits_afterRunInLabel[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > Splittimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                                    {
                                        SplitLabelArray[i, 2].Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 165));
                                    }
                                    else
                                    {
                                        SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Red;
                                    }
                                    if (isSplitAGoldSplit[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]) SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Gold;
                                }
                                else if (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] == PBTotalTimes_afterRunInLabel[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                                {
                                    SplitLabelArray[i, 2].Content = "-";
                                    SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                                }
                                else
                                {
                                    SplitLabelArray[i, 2].Content = (PBTotalTimes_afterRunInLabel[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\-\ ss\.f");
                                    SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Green;
                                    if (isSplitAGoldSplit[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]) SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Gold;
                                }
                            }
                            else
                            {
                                SplitLabelArray[i, 2].Content = "-";
                                SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                            }
                        }
                        else
                        {
                            SplitLabelArray[i, 2].Content = "-";
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                        }
                    }



                    //Update Split Time Label
                    if(currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] != TimeSpan.Zero)
                    {
                        SplitLabelArray[i, 3].Content = currentTotalTimes[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]].ToString(@"mm\:ss\.f");
                    }
                    else
                    {
                        SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                        SplitLabelArray[i, 1].Content = "-";
                        SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                        SplitLabelArray[i, 2].Content = "-";
                        SplitLabelArray[i, 3].Content = "-";
                    }

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
                                SplitLabelArray[i, 0].Content = "RR";
                                break;
                            default:
                                SplitLabelArray[i, 0].Content = "Error";
                                break;
                        }

                        //Gold Delta
                        if (GoldSplits[currentTrackOrder[i]] != TimeSpan.Zero)
                        {
                            if (isSplitAGoldSplit[currentTrackOrder[i]])
                            {
                                SplitLabelArray[i, 1].Content = (GoldSplits[currentTrackOrder[i]] - Splittimes[currentTrackOrder[i]]).ToString(@"\-\ ss\.f");
                                SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Gold;
                            }
                            else
                            {
                                SplitLabelArray[i, 1].Content = (Splittimes[currentTrackOrder[i]] - GoldSplits[currentTrackOrder[i]]).ToString(@"\+\ ss\.f");
                                SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Red;
                            }
                        }
                        else
                        {
                            SplitLabelArray[i, 1].Content = "-";
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                        }

                        //Update PB Difference Label
                        if (PBTotalTimes[currentTrackOrder[i]] > TimeSpan.Zero)
                        {
                            if (currentTotalTimes[currentTrackOrder[i]] > PBTotalTimes[currentTrackOrder[i]])
                            {
                                SplitLabelArray[i, 2].Content = (currentTotalTimes[currentTrackOrder[i]] - PBTotalTimes[currentTrackOrder[i]]).ToString(@"\+\ ss\.f");
                                if (PBSplits[currentTrackOrder[i]] > Splittimes[currentTrackOrder[i]])
                                {
                                    SplitLabelArray[i, 2].Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 165));
                                }
                                else
                                {
                                    SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Red;
                                }
                                if (isSplitAGoldSplit[currentTrackOrder[i]]) SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Gold;
                            }
                            else if (currentTotalTimes[currentTrackOrder[i]] == PBTotalTimes[currentTrackOrder[i]])
                            {
                                SplitLabelArray[i, 2].Content = "-";
                                SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                            }
                            else
                            {
                                SplitLabelArray[i, 2].Content = (PBTotalTimes[currentTrackOrder[i]] - currentTotalTimes[currentTrackOrder[i]]).ToString(@"\-\ ss\.f");
                                SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Green;
                                if (isSplitAGoldSplit[currentTrackOrder[i]]) SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Gold;
                            }
                        }
                        else
                        {
                            SplitLabelArray[i, 2].Content = "-";
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                        }


                        //Update Split Time Label
                        if(currentTotalTimes[currentTrackOrder[i]] != TimeSpan.Zero)
                        {
                            SplitLabelArray[i, 3].Content = currentTotalTimes[currentTrackOrder[i]].ToString(@"mm\:ss\.f");
                        }
                        else
                        {
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                            SplitLabelArray[i, 1].Content = "-";
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                            SplitLabelArray[i, 2].Content = "-";
                            SplitLabelArray[i, 3].Content = "-";
                        }
                    }
                    else
                    {
                        SplitLabelArray[i, 0].Foreground = System.Windows.Media.Brushes.White;
                        SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                        SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                        SplitLabelArray[i, 3].Foreground = System.Windows.Media.Brushes.White;
                        SplitLabelArray[i, 0].Content = "-";
                        SplitLabelArray[i, 1].Content = "-";
                        SplitLabelArray[i, 2].Content = "-";
                        SplitLabelArray[i, 3].Content = "-";
                    }
                }

                if(TimerLabel.Content.ToString() == "00:00") SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
            }

            if (MainStopwatch.IsRunning)
            {
                if (currentTrackOrder[currentSplitProgress] == -1)
                {
                    if(currentTrackIndex != -1)
                    {
                        Possible_Time_Save_Label_Number.Content = (PBSplits[currentTrackIndex] - GoldSplits[currentTrackIndex]).ToString(@"\-\ ss\.f");
                    }
                    else Possible_Time_Save_Label_Number.Content = "-";
                }
                else Possible_Time_Save_Label_Number.Content = "-";
            }
            else Possible_Time_Save_Label_Number.Content = "-";
            UpdateSumOfBest();
            Run_Counter_Label_Number.Content = startedRunCount + "/" + completedRunCount;
        }

        void UpdateSumOfBest()
        {
            if (!isPBMissingGoldSplits)
            {
                TimeSpan tempCalcTimeSpan = new TimeSpan();
                for (int i = 0; i < 16; i++)
                {
                    if (isSplitAGoldSplit[i]) tempCalcTimeSpan += UnsavedGoldSplits[i];
                    else tempCalcTimeSpan += GoldSplits[i];
                }
                SoB_Value.Content = tempCalcTimeSpan.ToString(@"mm\:ss");
            }
            else SoB_Value.Content = "-";

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
            ScrollOffset = 0;
            UpdateLabels();
        }

        #endregion

        #region TrackImagesEvents

        private void leTimer_Tick(object sender, EventArgs e)
        {
            foreach (Image stuff in trackSelectionImages)
            {
                stuff.IsEnabled = true;
            }
            System.Windows.Threading.DispatcherTimer timer = (System.Windows.Threading.DispatcherTimer)sender;
            Action action = (Action)timer.Tag;
            isPendingTrackSelection = true;
            action.Invoke();
            timer.Stop();
            waitingForFirstSplit = false;
        }


        private void GeneralTrackLogo_MouseDown(int givenIndex)
        {
            if (isPendingTrackSelection)
            {
                currentTrackIndex = givenIndex;
                trackSelectionImages[givenIndex].IsEnabled = false;
                trackSelectionImages[givenIndex].Opacity = .2;
                isPendingTrackSelection = false;
                switch (currentTrackIndex)
                {
                    case 0:
                        ProgramInfoLabel.Content = "Press splitkey to split on Luigi Circuit.";
                        break;
                    case 1:
                        ProgramInfoLabel.Content = "Press splitkey to split on Peach Beach.";
                        break;
                    case 2:
                        ProgramInfoLabel.Content = "Press splitkey to split on Baby Park.";
                        break;
                    case 3:
                        ProgramInfoLabel.Content = "Press splitkey to split on Dry Dry Desert.";
                        break;
                    case 4:
                        ProgramInfoLabel.Content = "Press splitkey to split on Mushroom Bridge.";
                        break;
                    case 5:
                        ProgramInfoLabel.Content = "Press splitkey to split on Mario Circuit.";
                        break;
                    case 6:
                        ProgramInfoLabel.Content = "Press splitkey to split on Daisy Cruiser.";
                        break;
                    case 7:
                        ProgramInfoLabel.Content = "Press splitkey to split on Waluigi Stadium.";
                        break;
                    case 8:
                        ProgramInfoLabel.Content = "Press splitkey to split on Sherbet Land.";
                        break;
                    case 9:
                        ProgramInfoLabel.Content = "Press splitkey to split on Mushroom City.";
                        break;
                    case 10:
                        ProgramInfoLabel.Content = "Press splitkey to split on Yoshi Circuit.";
                        break;
                    case 11:
                        ProgramInfoLabel.Content = "Press splitkey to split on DK Mountain.";
                        break;
                    case 12:
                        ProgramInfoLabel.Content = "Press splitkey to split on Wario Colosseum.";
                        break;
                    case 13:
                        ProgramInfoLabel.Content = "Press splitkey to split on Dino Dino Jungle.";
                        break;
                    case 14:
                        ProgramInfoLabel.Content = "Press splitkey to split on Bowser's Castle.";
                        break;
                    case 15:
                        ProgramInfoLabel.Content = "Press splitkey to split on Rainbow Road.";
                        break;
                }
                UpdateLabels();
            }
        }

        private void GeneralTrackLogo_MouseEnter(int givenIndex)
        {
            if (isPendingTrackSelection) if(!waitingForFirstSplit) if (trackSelectionImages[givenIndex].IsEnabled) trackSelectionImages[givenIndex].Opacity = 1;
        }

        private void GeneralTrackLogo_MouseLeave(int givenIndex)
        {
            if (isPendingTrackSelection) if(!waitingForFirstSplit) if (trackSelectionImages[givenIndex].IsEnabled) trackSelectionImages[givenIndex].Opacity = .5;
        }



        private void TrackLogo1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(0);
        }
        
        private void TrackLogo1_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(0);
        }
        
        private void TrackLogo1_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(0);
        }
        private void TrackLogo2_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(1);
        }

        private void TrackLogo2_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(1);
        }

        private void TrackLogo2_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(1);
        }
        private void TrackLogo3_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(2);
        }

        private void TrackLogo3_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(2);
        }

        private void TrackLogo3_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(2);
        }
        private void TrackLogo4_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(3);
        }

        private void TrackLogo4_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(3);
        }

        private void TrackLogo4_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(3);
        }
        private void TrackLogo5_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(4);
        }

        private void TrackLogo5_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(4);
        }

        private void TrackLogo5_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(4);
        }
        private void TrackLogo6_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(5);
        }

        private void TrackLogo6_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(5);
        }

        private void TrackLogo6_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(5);
        }
        private void TrackLogo7_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(6);
        }

        private void TrackLogo7_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(6);
        }

        private void TrackLogo7_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(6);
        }
        private void TrackLogo8_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(7);
        }

        private void TrackLogo8_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(7);
        }

        private void TrackLogo8_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(7);
        }
        private void TrackLogo9_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(8);
        }

        private void TrackLogo9_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(8);
        }

        private void TrackLogo9_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(8);
        }
        private void TrackLogo10_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(9);
        }

        private void TrackLogo10_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(9);
        }

        private void TrackLogo10_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(9);
        }
        private void TrackLogo11_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(10);
        }

        private void TrackLogo11_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(10);
        }

        private void TrackLogo11_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(10);
        }
        private void TrackLogo12_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(11);
        }

        private void TrackLogo12_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(11);
        }

        private void TrackLogo12_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(11);
        }



        private void TrackLogo13_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(12);
        }

        private void TrackLogo13_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(12);
        }

        private void TrackLogo13_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(12);
        }
        private void TrackLogo14_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(13);
        }

        private void TrackLogo14_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(13);
        }

        private void TrackLogo14_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(13);
        }
        private void TrackLogo15_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(14);
        }

        private void TrackLogo15_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(14);
        }

        private void TrackLogo15_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(14);
        }
        private void TrackLogo16_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GeneralTrackLogo_MouseDown(15);
        }

        private void TrackLogo16_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseEnter(15);
        }

        private void TrackLogo16_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GeneralTrackLogo_MouseLeave(15);
        }
    
        #endregion

        TimeSpan SumUpTimeArray(TimeSpan[] givenArray)
        {
            TimeSpan Result = new TimeSpan();
            foreach (TimeSpan i in givenArray)
            {
                Result += i;
            }
            return Result;
        }

    }
}
