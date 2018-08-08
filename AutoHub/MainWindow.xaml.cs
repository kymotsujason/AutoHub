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

namespace AutoHub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_Sweeper7(object sender, RoutedEventArgs e)
        {
            Sweeper7 sweep7Win = new Sweeper7();
            sweep7Win.Show();
        }

        private void Button_Click_Sweeper10(object sender, RoutedEventArgs e)
        {
            Sweeper10 sweep10Win = new Sweeper10();
            sweep10Win.Show();
        }
    }
}
