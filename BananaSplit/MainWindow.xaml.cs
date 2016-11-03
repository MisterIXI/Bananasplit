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

        bool ifPBHasHappenend;

        #region SettingsVariables

        bool UseGlobalHotkeys = false; //todo: later set this to true

        #endregion

        #region XMLVariables

        XDocument saveFile;
        XElement saveFileElement;
        string defaultSaveFileName = "C:\\temp\\BananaSplit_SaveFile.xml";

        #endregion

        #region SplitterVariables
        //Splitter Variables
        int currentTrackIndex, currentSplitProgress = 0;

        int[] currentTrackOrder = new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};
        TimeSpan[] Splittimes = new TimeSpan[16], PBSplits = new TimeSpan[16], UnsavedGoldSplits = new TimeSpan[16], GoldSplits = new TimeSpan[16], currentTotalTimes = new TimeSpan[16], PBTotalTimes = new TimeSpan[16], PBTimesInRun = new TimeSpan[16];
        TimeSpan lastSplit, totalCurrentRunTime, totalCurrentPBTime, PBEndTime, currentEndTime;
        bool[] isSplitAGoldSplit = new bool[16];
        bool isPendingTrackSelection, isPBMissingSplits, isPBMissingGoldSplits, isCurrentRunMissingSplits;

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

        #region InterfaceVariables
        //Interface Variables
        int ScrollOffset = 0;
        Image[] trackSelectionImages; 
        System.Windows.Controls.Label[,] SplitLabelArray;
        bool unsavedChangesFlag;

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

            if (UseGlobalHotkeys)
            {
                RegisterAllHotkeys();
            }

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
            UpdateLabels();
            Previous_Segment_Label_Number.Content = "-";
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

        #region KeyFunctions
        void RegisterAllHotkeys()
        {
            Hook.RegisterHotKey(Keys.Space);
            Hook.RegisterHotKey(Keys.A);
            Hook.RegisterHotKey(Keys.S);
            Hook.RegisterHotKey(Keys.D);
        }

        void SplitKey()
         {
            if (MainStopwatch.IsRunning) Split();
            else if (MainStopwatch.Elapsed == TimeSpan.Zero) StartSplitting();
        }

        void ResetKey()
        {
            
            if (CheckIfRecords())
            {
                switch (System.Windows.MessageBox.Show("Some of your splits have been updated. Do you want to save them?", "Unsaved Times", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    case MessageBoxResult.Yes:
                        UpdateRecords();
                        Reset();
                        break;
                    case MessageBoxResult.No:
                        Reset();
                        break;
                    case MessageBoxResult.Cancel:
                        break;
                }

            }
            else Reset();

        }

        void UndoSplitKey()
        {
            //todo
        }

        void SkipSplitKey()
        {
            //todo
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

        #endregion



        void Hook_OnKeyPress(object sender, LiveSplit.Model.Input.KeyOrButton e)
        {

            //Console.WriteLine("pressing triggered");
          //  if (UseGlobalHotkeys)
           // {
                Action action = () =>
                {
                    if (Keys.Space == e.Key)
                    {
                        SplitKey();
    


                    }
                    if (Keys.A == e.Key)
                    {
                        saveFileElement = new XElement("SaveFile", new XElement("Times"), new XElement("Settings"), new XElement("Miscellaneous"));
                        for (int i = 0; i <= 15; i++)
                        {
                            saveFileElement.Element("Times").Add(new XElement("Track" + i.ToString(), new XAttribute("PBTime", Splittimes[i]), new XAttribute("GoldTime", UnsavedGoldSplits[i])));
                        }
                    }

                    if (Keys.S == e.Key)
                    {

                    }
                    if (Keys.D == e.Key)
                    {
                        Console.WriteLine(saveFileElement);
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
           // }
        }

        #region WindowEvents

        private void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!UseGlobalHotkeys)
            {
                if (e.Key == System.Windows.Input.Key.NumPad1) SplitKey();
                if (e.Key == System.Windows.Input.Key.NumPad3) ResetKey();
                if (e.Key == System.Windows.Input.Key.NumPad2) SkipSplitKey();
                if (e.Key == System.Windows.Input.Key.NumPad8) UndoSplitKey();

                if (e.Key == System.Windows.Input.Key.Space) SplitKey();
                if (e.Key == System.Windows.Input.Key.R) ResetKey();

                if(e.Key == System.Windows.Input.Key.S && System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                {
                    if (CheckIfRecords())
                    {
                        switch (System.Windows.MessageBox.Show("Some of your splits have been updated. Do you want to save them?", "Unsaved Times", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                        {
                            case MessageBoxResult.Yes:
                                UpdateRecords();
                                break;
                            case MessageBoxResult.No:
                                Reset();
                                break;
                            case MessageBoxResult.Cancel:
                                return;
                        }

                    }
                }
            }


                if (e.Key == System.Windows.Input.Key.M && e.Key == System.Windows.Input.Key.K)
            {
                Console.Beep(800, 400);
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

        void on_Window_Close(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CheckIfRecords())
            {
                if (!MainStopwatch.IsRunning)
                {
                    if ((string)TimerLabel.Content != "00:00")
                    {
                        switch (System.Windows.MessageBox.Show("Some of your splits have been updated. Do you want to save them?", "Unsaved Times", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                        {
                            case MessageBoxResult.Yes:
                                UpdateRecords();
                                break;
                            case MessageBoxResult.No:
                                break;
                            case MessageBoxResult.Cancel:
                                e.Cancel = true;
                                return;
                        }
                    }
                }
            }
            if (unsavedChangesFlag)
                switch (System.Windows.MessageBox.Show("Your splits have been updated but not yet saved. Do you want to save your spltis now?", "Save Splits?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    case MessageBoxResult.Yes:
                        SaveRecords();
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
            currentTrackIndex = 0;
            currentSplitProgress = 0;
            lastSplit = TimeSpan.Zero;
            TrackLogo1.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
            totalCurrentRunTime = TimeSpan.Zero;
            totalCurrentPBTime = TimeSpan.Zero;
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
                    Previous_Segment_Label_Number.Content = " - ";
                }

                totalCurrentRunTime += Splittimes[currentTrackIndex];
                totalCurrentPBTime += PBSplits[currentTrackIndex];

                if(PBTotalTimes[currentTrackIndex] == TimeSpan.Zero)
                {
                    PBTotalTimes[currentTrackIndex] = TempTimeSpan;
                }



                ScrollOffset = 0;
                currentSplitProgress++;
                isPendingTrackSelection = true;
                currentTrackIndex = -1;
                //if(!MainStopwatch.IsRunning)if (CheckIfRecords()) OnPB();
                UpdateLabels();

            }
        }

        void SkipSplit()
        {
            //todo
        }

        void UndoSplit()
        {
            //todo
        }

        void OnPB()
        {
            PBEndTime = MainStopwatch.Elapsed;

        }

        bool CheckIfRecords()
        {
            bool tempCheckingBool = false;

            if (PBEndTime > currentEndTime && currentEndTime > TimeSpan.Zero)//todo exchange with wholetime
            {
                tempCheckingBool = true;
            }
            else if (SumUpTimeArray(PBSplits) == TimeSpan.Zero | PBEndTime == TimeSpan.Zero)
            {
                tempCheckingBool = true;
            }

            if (tempCheckingBool) return true;
            else return false;
        }

        void UpdateRecords()
        {
            if (PBEndTime > currentEndTime && currentEndTime > TimeSpan.Zero)//todo exchange with wholetime
            {
                PBSplits = Splittimes;
                PBTotalTimes = currentTotalTimes;
                PBEndTime = currentEndTime;
                unsavedChangesFlag = true;
            }
            else if (SumUpTimeArray(PBSplits) == TimeSpan.Zero)
            {
                PBSplits = Splittimes;
                unsavedChangesFlag = true;
            }
            for (int i = 0; i <= 15; i++)
            {
                if (isSplitAGoldSplit[i])
                {
                    GoldSplits[i] = Splittimes[i];
                    unsavedChangesFlag = true;
                }
            }
        }

        void Reset()
        {

            MainStopwatch.Stop();
            MainStopwatch.Reset();
            if (MainRefreshTimer != null) MainRefreshTimer.Dispose();
            TimerLabel.Content = ("00:00");
            SegmentLabel.Content = ("00:00");
            foreach (Image stuff in trackSelectionImages) stuff.Visibility = System.Windows.Visibility.Visible;
            isPendingTrackSelection = false;            
            currentSplitProgress = 16;
            currentTrackOrder = new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            Splittimes = PBSplits;
            currentTotalTimes = PBTotalTimes;
            isSplitAGoldSplit = new bool[16];
            ScrollOffset = -8;
            currentEndTime = TimeSpan.Zero;
            UpdateLabels();
        }

        void ResetVariables()
        {
            SegmentStopwatch.Reset();
            MainStopwatch.Reset();
            currentSplitProgress = 0;
            currentTrackIndex = 0;
            currentTrackOrder = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            Splittimes = new TimeSpan[16];
            isSplitAGoldSplit = new bool[16];
            if (MainRefreshTimer != null) MainRefreshTimer.Dispose();
            TimerLabel.Content = ("00:00");
            SegmentLabel.Content = ("00:00");
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
                    saveFileElement.Element("Times").Add(new XElement("Track" + i.ToString(), new XAttribute("PBTime", TimeSpan.Zero), new XAttribute("GoldTime", TimeSpan.Zero), new XAttribute("TimesInRun", TimeSpan.Zero)));
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
                PBTotalTimes[i] = TimeSpan.Zero;
                try
                {
                    PBSplits[i] = XmlConvert.ToTimeSpan(saveFileElement.Element("Times").Element("Track" + i.ToString()).Attribute("PBTime").Value);
                    GoldSplits[i] = XmlConvert.ToTimeSpan(saveFileElement.Element("Times").Element("Track" + i.ToString()).Attribute("GoldTime").Value);
                    PBTotalTimes[i] = XmlConvert.ToTimeSpan(saveFileElement.Element("Times").Element("Track" + i.ToString()).Attribute("TimesInRun").Value);
                }
                catch (NullReferenceException)
                {
                    if (PBSplits[i] == TimeSpan.Zero) isPBMissingSplits = true;
                    if (GoldSplits[i] == TimeSpan.Zero) isPBMissingGoldSplits = true;
                }  
            }
        }



        void SaveRecords()
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
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("TimesInRun", PBTotalTimes[i]);
                    }
                    else
                    {
                        saveFileElement.Element("Times").Add(new XElement("Track" + i.ToString()));
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("PBTime", PBSplits[i]);
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("GoldTime", GoldSplits[i]);
                        saveFileElement.Element("Times").Element("Track" + i.ToString()).SetAttributeValue("TimesInRun", PBTotalTimes[i]);
                    }
                }
            }
            else
            {
                saveFileElement.Add(new XElement("Times"));
                for (int i = 0; i <= 15; i++)
                {
                    saveFileElement.Element("Times").Add(new XElement("Track" + i.ToString(), new XAttribute("PBTime", Splittimes[i]), new XAttribute("GoldTime", UnsavedGoldSplits[i]), new XAttribute("TimesInRun", PBTotalTimes[i])));
                }

            }
            saveFile = new XDocument(saveFileElement);
            saveFile.Save(defaultSaveFileName);
            unsavedChangesFlag = false;
        }

        void LoadMiscellaneous()
        {
            //todo
        }

        void SaveMiscellaneous()
        {
            //todo
        }

        void saveSettings()
        {
            if (saveFileElement.Element("Times") != null)
            {

            }
            else
            {
                saveFileElement = new XElement("SaveFile", new XElement("Settings"));
            }
            //todo
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
                            SplitLabelArray[i, 0].Content = "RRR";
                            break;
                        default:
                            SplitLabelArray[i, 0].Content = "Error";
                            break;
                    }

                    //Gold Delta
                    //Todo: fix yo shit bruh: calculations
                    if (GoldSplits[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] != TimeSpan.Zero)
                    {
                        if (Splittimes[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] >= GoldSplits[currentSplitProgress -1 -6 + i + ScrollOffset])
                        {
                            SplitLabelArray[i, 1].Content = (Splittimes[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]] - GoldSplits[currentSplitProgress -1 -6 + i + ScrollOffset]).ToString(@"\+\ ss\.f");
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                        }
                        else
                        {
                            SplitLabelArray[i, 1].Content = (GoldSplits[currentSplitProgress -1 -6 + i + ScrollOffset] - Splittimes[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]]).ToString(@"\-\ ss\.f");
                            SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Gold;
                        }
                    }
                    else
                    {
                        SplitLabelArray[i, 1].Content = "-";
                        SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                    }

                    //Update PB Difference Label
                    if (PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > TimeSpan.Zero)
                    {
                        if (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] > PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                        {
                            SplitLabelArray[i, 2].Content = (currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\+\ ss\.f");
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Red;
                        }
                        else if(currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] == PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]])
                        {
                            SplitLabelArray[i, 2].Content = "-";
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                        }
                        else
                        {
                            SplitLabelArray[i, 2].Content = (PBTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]] - currentTotalTimes[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]).ToString(@"\-\ ss\.f");
                            SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Green;
                            if(isSplitAGoldSplit[currentTrackOrder[currentSplitProgress - 1 - 6 + i + ScrollOffset]]) SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Gold;
                        }
                    }
                    else
                    {
                        SplitLabelArray[i, 2].Content = "-";
                        SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.White;
                    }

                    //Update Split Time Label
                    SplitLabelArray[i, 3].Content = currentTotalTimes[currentTrackOrder[currentSplitProgress -1 -6 + i + ScrollOffset]].ToString(@"mm\:ss\.f");

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
                            if (Splittimes[currentTrackOrder[i]] > GoldSplits[currentTrackOrder[i]])
                            {
                                SplitLabelArray[i, 1].Content = (Splittimes[currentTrackOrder[i]] - GoldSplits[currentTrackOrder[i]]).ToString(@"\+\ ss\.f");
                                SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                            }
                            else if(Splittimes[currentTrackOrder[i]] == GoldSplits[currentTrackOrder[i]])
                            {
                                SplitLabelArray[i, 1].Content = "-";
                                SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.White;
                            }
                            else
                            {
                                SplitLabelArray[i, 1].Content = (GoldSplits[currentTrackOrder[i]] - Splittimes[currentTrackOrder[i]]).ToString(@"\-\ ss\.f");
                                SplitLabelArray[i, 1].Foreground = System.Windows.Media.Brushes.Gold;                               
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
                                SplitLabelArray[i, 2].Foreground = System.Windows.Media.Brushes.Red;
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
                        SplitLabelArray[i, 3].Content = currentTotalTimes[currentTrackOrder[i]].ToString(@"mm\:ss\.f");
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

            if (MainStopwatch.IsRunning)
            {
                if (currentTrackOrder[currentSplitProgress] == -1)
                {
                    if(currentTrackIndex != -1)
                    {
                        Possible_Time_Save_Label_Number.Content = (PBSplits[currentTrackIndex] - GoldSplits[currentTrackIndex]).ToString(@"\-\ ss\.ff");
                    }
                    else Possible_Time_Save_Label_Number.Content = "-";
                }
                else Possible_Time_Save_Label_Number.Content = "-";
            }
            else Possible_Time_Save_Label_Number.Content = "-";


            if(!isPBMissingSplits) SoB_Value.Content = SumUpTimeArray(GoldSplits).ToString(@"mm\:ss");



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
        private void TrackLogo1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentTrackIndex = 0;
            TrackLogo1.Visibility = Visibility.Hidden;
            isPendingTrackSelection = false;
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
            UpdateLabels();
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
