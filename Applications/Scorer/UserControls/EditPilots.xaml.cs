using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Scorer
{
    public partial class EditPilots : EditCollection<Pilot>
    {
        public EditPilots(ObservableCollection<Pilot> pilots, EditOptions editOptions)
            : base(pilots, editOptions)
        {
            InitializeComponent();
        }

        private void menuRemove_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var pilot = menuItem.Tag as Pilot;
            BufferCollection.Remove(pilot);
        }
        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (BufferCollection.Count == 0)
                BufferCollection.Add(new Pilot() { Number = 1 });
            else
                BufferCollection.Add(new Pilot() { Number = BufferCollection.Max(p => p.Number) + 1 });
        }
        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "CSV files (*.csv)|*.csv";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                ImportPilots(dlg.FileName);
        }
        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void ImportPilots(string filePath)
        {
            var pilotList = File.ReadAllLines(filePath);
            int i = 0;
            try
            {
                BufferCollection.Clear();
                foreach (var p in pilotList)
                {
                    i++;
                    var pilot = p.Trim();
                    if (pilot != "" && pilot[0] != '#')
                    {
                        var fields = pilot.Split(new char[] { '\t', ';' }, StringSplitOptions.None);
                        var number = int.Parse(fields[0]);
                        var name = fields[1].Trim();
                        var country = (fields.Length > 2) ? fields[2].Trim() : "";
                        var balloon = (fields.Length > 3) ? fields[3].Trim() : "";

                        BufferCollection.Add(new Pilot() { Number = number, Name = name, Country = country, Balloon = balloon });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error in line " + i.ToString() + ":" + Environment.NewLine + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
            }
        }
    }
}
