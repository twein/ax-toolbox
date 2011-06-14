using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
//using System.Linq;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Scorer
{
    public partial class EditCompetitions
    {
        public ObservableCollection<Competition> Competitions { get; set; }

        public EditCompetitions()
        {
            InitializeComponent();
            DataContext = this;

            Competitions = new ObservableCollection<Competition>();
            Database.Instance.Competitions.CopyTo(Competitions);
        }

        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Competitions.Sort(c => c.Id).CopyTo(Database.Instance.Competitions);
        }
    }
}
