using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Scorer
{
    public partial class EditTasks
    {
        public ObservableCollection<Task> EditBuffer { get; set; }
        public ObservableCollection<Task> Tasks { get; set; }

        public EditTasks(ObservableCollection<Task> tasks)
        {
            InitializeComponent();
            DataContext = this;

            EditBuffer = new ObservableCollection<Task>();
            tasks.CopyTo(EditBuffer);
            Tasks = tasks;
        }

        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EditBuffer.Count == 0)
                EditBuffer.Add(new Task() { Number = 1 });
            else
                EditBuffer.Add(new Task() { Number = EditBuffer.Last().Number + 1 });
        }
        private void deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EditBuffer.Count > 0)
                EditBuffer.RemoveAt(EditBuffer.Count - 1);
        }
        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EditBuffer.Count > 0 && Tasks.Count == 0)
            {
                //First task added
                //throw new NotImplementedException();
            }

            EditBuffer.Sort(t => t.Number).CopyTo(Tasks);
            Database.Instance.IsDirty = true;
        }
    }
}
