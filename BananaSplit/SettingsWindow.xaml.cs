﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml;
using System.Media;

namespace BananaSplit
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        Key TempSplitKey, TempResetKey, TempSkipSplitKey, TempUndoSelKey;
        bool TempChromaMode, TempGloabelHotkeys;
        


        public SettingsWindow()
        {
            InitializeComponent();

            TB_Split.Text = MainWindow.SplitKeyCode.ToString();
            TB_Reset.Text = MainWindow.ResetKeyCode.ToString();
            TB_SkipSplit.Text = MainWindow.SkipSplitKeyCode.ToString();
            TB_UndoSel.Text = MainWindow.UndoSelectionKeyCode.ToString();
            TB_SplitDelay.Text = MainWindow.SplitDelay.ToString();

            CheckBox_BestPosTime.IsChecked = MainWindow.UseBestPosTime;
            CheckBox_GlobalHotkeys.IsChecked = MainWindow.UseGlobalHotkeys;
            CheckBox_ChromaKeyMode.IsChecked = MainWindow.IsInChromaMode;
            CheckBox_UnTintMainTimer.IsChecked = MainWindow.IsInTintedMode;

            TempGloabelHotkeys = MainWindow.UseGlobalHotkeys;
            TempChromaMode = MainWindow.IsInChromaMode;
        }

        private void Button_OK_MouseDown(object sender, MouseButtonEventArgs e)
        {
            /*
            Console.WriteLine(TempSplitKey);
            Console.WriteLine(TempResetKey);
            Console.WriteLine(TempSkipSplitKey);
            Console.WriteLine(TempUndoSelKey);
            */
            if(TempSplitKey != default(Key)) MainWindow.SplitKeyCode = (Keys)KeyInterop.VirtualKeyFromKey(TempSplitKey);
            if (TempResetKey != default(Key)) MainWindow.ResetKeyCode = (Keys)KeyInterop.VirtualKeyFromKey(TempResetKey);
            if (TempSkipSplitKey != default(Key)) MainWindow.SkipSplitKeyCode = (Keys)KeyInterop.VirtualKeyFromKey(TempSkipSplitKey);
            if (TempUndoSelKey != default(Key)) MainWindow.UndoSelectionKeyCode = (Keys)KeyInterop.VirtualKeyFromKey(TempUndoSelKey);

            MainWindow.SplitDelay = Convert.ToInt32(TB_SplitDelay.Text.ToString());

            MainWindow.UseBestPosTime = CheckBox_BestPosTime.IsChecked.Value;
            MainWindow.UseGlobalHotkeys = CheckBox_GlobalHotkeys.IsChecked.Value;
            MainWindow.IsInChromaMode = CheckBox_ChromaKeyMode.IsChecked.Value;
            MainWindow.IsInTintedMode = CheckBox_UnTintMainTimer.IsChecked.Value;

            if (CheckBox_GlobalHotkeys.IsChecked.Value && !TempGloabelHotkeys) System.Windows.MessageBox.Show("In order for the Global Hotkeys to work you have to restart the program.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);


             Close();
        }

        private void TheNewTruePreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs  e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void Button_Cancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void TB_Split_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key != Key.Escape)
            {
                TB_Split.Text = e.Key.ToString();
                TempSplitKey = e.Key;
                Keyboard.ClearFocus();
            }
        }

        private void TB_Split_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TB_Split.Text = "Set Hotkey...";
        }

        private void TB_Reset_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                TB_Reset.Text = e.Key.ToString();
                TempResetKey = e.Key;
                Keyboard.ClearFocus();
            }
        }

        private void TB_Reset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TB_Reset.Text = "Set Hotkey...";
        }

        private void TB_SkipSplit_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                TB_SkipSplit.Text = e.Key.ToString();
                TempSkipSplitKey = e.Key;
                Keyboard.ClearFocus();
            }
        }

        private void TB_SkipSplit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TB_SkipSplit.Text = "Set Hotkey...";
        }

        private void TB_UndoSel_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                TB_UndoSel.Text = e.Key.ToString();
                TempUndoSelKey = e.Key;
                Keyboard.ClearFocus();
            }
        }

        private void TB_UndoSel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TB_UndoSel.Text = "Set Hotkey...";
        }

    }
}
