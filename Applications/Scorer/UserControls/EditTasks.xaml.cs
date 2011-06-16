using System;
using System.Collections.ObjectModel;
using System.Linq;

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

        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Tasks.Count == 0)
                Tasks.Add(new Task() { Number = 1 });
            else 
                Tasks.Add(new Task() { Number = Tasks.Last().Number + 1 });
        }
        private void deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Tasks.Count > 0)
                Tasks.RemoveAt(Tasks.Count - 1);
        }
        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var db = Database.Instance;

            if (Tasks.Count > 0 && db.Tasks.Count == 0)
            {
                //First task added
                throw new NotImplementedException();
            }

            Tasks.Sort(t => t.Number).CopyTo(Database.Instance.Tasks);

            Database.Instance.IsDirty = true;
        }
    }
}
