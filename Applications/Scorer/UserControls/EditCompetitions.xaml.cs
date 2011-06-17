using System.Collections.ObjectModel;

namespace Scorer
{
    public partial class EditCompetitions :EditCollection<Competition>
    {
        public EditCompetitions(ObservableCollection<Competition> competitions, EditOptions editOptions)
            : base(competitions, editOptions)
        {
            InitializeComponent();
        }

        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BufferCollection.Add(new Competition());
        }

        private void deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (BufferCollection.Count > 0)
                BufferCollection.RemoveAt(BufferCollection.Count - 1);
        }

        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }
    }
}
