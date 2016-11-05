using System;
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

namespace BananaSplit
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        Key TempSplitKey, TempResetKey, TempSkipSplitKey, TempUndoSelKey;

        public SettingsWindow()
        {
            InitializeComponent();

            TB_Split.Text = MainWindow.SplitKeyCode.ToString();
            TB_Reset.Text = MainWindow.ResetKeyCode.ToString();
            TB_SkipSplit.Text = MainWindow.SkipSplitKeyCode.ToString();
            TB_UndoSel.Text = MainWindow.UndoSelectionKeyCode.ToString();

            CheckBox_GlobalHotkeys.IsChecked = MainWindow.UseGlobalHotkeys;
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

            MainWindow.UseGlobalHotkeys = CheckBox_GlobalHotkeys.IsChecked.Value;

            Close();
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
