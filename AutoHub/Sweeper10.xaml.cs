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

namespace AutoHub
{
    /// <summary>
    /// Interaction logic for Sweeper10.xaml
    /// </summary>
    public partial class Sweeper10 : Window
    {
        Minesweeper minesweeper = new Minesweeper();

        public Sweeper10()
        {
            InitializeComponent();
            this.minesweeper.Init10();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            await Start();
        }

        private void ReportProgress(string value)
        {
            statusText.Text = value;
        }

        private async Task Start()
        {
            var progressIndicator = new Progress<string>(ReportProgress);
            await Task.Run(() => minesweeper.Begin(progressIndicator));
            //await Task.Run(() =>
            //{
                //this.minesweeper.Begin(statusText);
            //});
            StartButton.IsEnabled = true;
        }
    }
}
