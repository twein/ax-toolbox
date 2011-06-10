using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
//using System.Linq;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Scorer
{
    public partial class EditTasks
    {
        public ObservableCollection<Task> Tasks { get; set; }

        public EditTasks()
        {
            InitializeComponent();
            DataContext = this;

            Tasks = new ObservableCollection<Task>();
            Database.Instance.Tasks.CopyTo(Tasks);
        }

        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Tasks.Sort(p => p.Number).CopyTo(Database.Instance.Tasks);
        }
    }
}
