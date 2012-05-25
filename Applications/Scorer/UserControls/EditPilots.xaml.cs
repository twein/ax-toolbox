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
            var pilot = ((MenuItem)sender).Tag as Pilot;
            DataGridCollection.Remove(pilot);
        }
        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            Pilot newPilot = null;
            if (DataGridCollection.Count == 0)
                newPilot = new Pilot() { Number = 1 };
            else
            {
                DataGridCollection.Sort(p => p.Number);
                newPilot = new Pilot() { Number = DataGridCollection.Max(p => p.Number) + 1 };
            }

            DataGridCollection.Add(newPilot);
        }
    }
}
