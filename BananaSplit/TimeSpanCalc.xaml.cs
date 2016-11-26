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
    /// Interaction logic for TimeSpanCalc.xaml
    /// </summary>
    public partial class TimeSpanCalc : Window
    {
        public TimeSpanCalc()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TB_OutPut.Text = (TimeSpan.Parse("00:0" + TB_TimeToPutThere.Text) - TimeSpan.Parse("00:0" + TB_TimeToSubtract.Text)).ToString(@"mm\:ss\.fff");
            }
            catch
            {
                TB_OutPut.Text = "Wrong Input";
            }
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
    }
}
