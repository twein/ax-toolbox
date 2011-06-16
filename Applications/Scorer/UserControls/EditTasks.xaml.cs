using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Scorer
{
    public partial class EditTasks
    {
        public ObservableCollection<Task> EditBuffer { get; private set; }
        public bool ReadOnly
        {
            get { return (Options & EditOptions.CanEdit) > 0; }
        }
        public Visibility DeleteVisibility
        {
            get { return (Options & EditOptions.CanDelete) > 0 ? Visibility.Visible : Visibility.Hidden; }
        }
        public Visibility AddVisibility
        {
            get { return (Options & EditOptions.CanAdd) > 0 ? Visibility.Visible : Visibility.Hidden; }
        }


        private ObservableCollection<Task> Tasks;
        private EditOptions Options;


        public EditTasks(ObservableCollection<Task> tasks, EditOptions editOptions)
        {
            InitializeComponent();
            DataContext = this;

            EditBuffer = new ObservableCollection<Task>();
            tasks.CopyTo(EditBuffer);
            Tasks = tasks;

            Options = editOptions;
        }

        private void menuRemove_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var task = menuItem.Tag as Task;
            EditBuffer.Remove(task);
        }
        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EditBuffer.Count == 0)
                EditBuffer.Add(new Task() { Number = 1 });
            else
                EditBuffer.Add(new Task() { Number = EditBuffer.Last().Number + 1 });
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
