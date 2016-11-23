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
using System.Windows.Shapes;

namespace BananaSplit
{
    /// <summary>
    /// Interaction logic for SplitEditor.xaml
    /// </summary>
    public partial class SplitEditor : Window
    {

        TextBox[] TimeFields = new TextBox[16];

        public SplitEditor()
        {
            InitializeComponent();
            TimeFields = new TextBox[] { TB_LC, TB_PB, TB_BP, TB_DDD, TB_MB, TB_MAC, TB_DC, TB_WS, TB_SL, TB_MUC, TB_YC, TB_DKM, TB_WC, TB_DDJ, TB_BC, TB_RR };
            for (int i = 0; i < 16; i++)
            {
                TimeFields[i].Text = MainWindow.PBSplits[i].ToString(@"m\:ss\.fff");
            }
            TB_Total.Text =  MainWindow.PBEndTime.ToString(@"mm\:ss\.fff");
        }

        private void BlockWrongInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9.:]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void Btn_OK_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                for (int i = 0; i < 16; i++)
                {
                    MainWindow.PBSplits[i] = TimeSpan.Parse("00:0"+TimeFields[i].Text);
                }
                MainWindow.PBEndTime = TimeSpan.Parse("00:"+TB_Total.Text);
                Close();
            }
            catch
            {
                MessageBox.Show("Something is wrong in the times...");
            }

        }

        private void Btn_Cancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("apiwfhapwfih");
            Close();
        }
    }
}
