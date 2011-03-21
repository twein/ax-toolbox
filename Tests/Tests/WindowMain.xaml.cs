using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using AXToolbox.Common;

namespace AXToolbox.Tests
{
    public partial class WindowMain : Window
    {
        public WindowMain()
        {
            InitializeComponent();
        }

        public void Print(string line)
        {
            var par = new Paragraph();
            par.Margin = new Thickness(0);
            par.Inlines.Add(line);
            output.Blocks.Add(par);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            output.Blocks.Clear();
        }

        private void buttonMap_Click(object sender, RoutedEventArgs e)
        {
            Print("Opening WindowMapViewer");
            var w = new WindowMapViewer();
            w.ShowDialog();
        }

        private void buttonScripting_Click(object sender, RoutedEventArgs e)
        {
            Print("Opening WindowScripting");
            var w = new WindowScripting();
            w.ShowDialog();
        }
    }
}
