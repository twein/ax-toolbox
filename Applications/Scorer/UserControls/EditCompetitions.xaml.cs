using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Scorer
{
    public partial class EditCompetitions :EditCollection<Competition>
    {
        public EditCompetitions(ObservableCollection<Competition> competitions, EditOptions editOptions)
            : base(competitions, editOptions)
        {
            InitializeComponent();
        }

        private void menuRemove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            BufferCollection.Remove(competition);
        }
        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BufferCollection.Add(new Competition());
        }
        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }
    }
}
