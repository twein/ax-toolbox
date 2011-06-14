using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
//using System.Linq;
using Microsoft.Win32;

namespace Scorer
{
    public partial class EditPilots
    {
        public ObservableCollection<Pilot> Pilots { get; set; }

        public EditPilots()
        {
            InitializeComponent();
            DataContext = this;

            Pilots = new ObservableCollection<Pilot>();
            Database.Instance.Pilots.CopyTo(Pilots);
            //Pilots.CollectionChanged += new NotifyCollectionChangedEventHandler(Pilots_CollectionChanged);
        }

        void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
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
            Pilots.Sort(p => p.Number).CopyTo(Database.Instance.Pilots);
            Database.Instance.IsDirty = true;
        }

        private void ImportPilots(string filePath)
        {
            var pilotList = File.ReadAllLines(filePath);
            int i = 0;
            try
            {
                Pilots.Clear();
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

                        Pilots.Add(new Pilot() { Number = number, Name = name, Country = country, Balloon = balloon });
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
