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
            var menuItem = sender as MenuItem;
            var task = menuItem.Tag as Task;
            BufferCollection.Remove(task);
        }
        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (BufferCollection.Count == 0)
                BufferCollection.Add(new Task() { Number = 1 });
            else
                BufferCollection.Add(new Task() { Number = BufferCollection.Last().Number + 1 });
        }
        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (BufferCollection.Count > 0 && saveCollection.Count == 0)
            {
                //First task added
                //throw new NotImplementedException();
            }

            Save();
        }
    }
}
