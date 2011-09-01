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
            DataGridCollection.Remove(competition);
        }
        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DataGridCollection.Add(new Competition());
        }
    }
}
