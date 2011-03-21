using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace PDFTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
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

        private void buttonPdf_Click(object sender, RoutedEventArgs e)
        {
            var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "iTextsharp.pdf");

            Print("Writing " + fileName);
            try
            {
                var pdftest = new PdfTest(fileName);

                var proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = fileName;
                proc.Start();
            }
            catch (Exception ex)
            {
                Print(ex.Message);
            }
        }
    }
}
