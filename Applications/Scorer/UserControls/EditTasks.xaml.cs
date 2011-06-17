using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Scorer
{
    public partial class EditTasks : EditCollection<Task>
    {

        public EditTasks(ObservableCollection<Task> tasks, EditOptions editOptions)
            : base(tasks, editOptions)
        {
            InitializeComponent();
        }

        private void menuRemove_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            DataGridCollection.Remove(task);
        }
        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataGridCollection.Count == 0)
                DataGridCollection.Add(new Task() { Number = 1 });
            else
                DataGridCollection.Add(new Task() { Number = DataGridCollection.Last().Number + 1 });
        }
    }
}
