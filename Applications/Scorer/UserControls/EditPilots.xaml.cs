using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Scorer
{
    public partial class EditPilots
    {
        public ObservableCollection<Pilot> EditBuffer { get; set; }
        public ObservableCollection<Pilot> Pilots { get; set; }

        public EditPilots(ObservableCollection<Pilot> pilots)
        {
            InitializeComponent();

            DataContext = this;

            EditBuffer = new ObservableCollection<Pilot>();
            pilots.CopyTo(EditBuffer);
            Pilots = pilots;
        }

        void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EditBuffer.Count == 0)
                EditBuffer.Add(new Pilot() { Number = 1 });
            else
                EditBuffer.Add(new Pilot() { Number = EditBuffer.Max(p => p.Number) + 1 });
        }

        private void deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EditBuffer.Count > 0)
                EditBuffer.RemoveAt(EditBuffer.Count - 1);
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
            EditBuffer.Sort(p => p.Number).CopyTo(Pilots);
            Database.Instance.IsDirty = true;
        }

        private void ImportPilots(string filePath)
        {
            var pilotList = File.ReadAllLines(filePath);
            int i = 0;
            try
            {
                EditBuffer.Clear();
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

                        EditBuffer.Add(new Pilot() { Number = number, Name = name, Country = country, Balloon = balloon });
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
