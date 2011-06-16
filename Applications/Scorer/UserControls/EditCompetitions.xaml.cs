using System.Collections.ObjectModel;

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

        private void addButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Competitions.Add(new Competition());
        }

        private void deleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Competitions.Count > 0)
                Competitions.RemoveAt(Competitions.Count - 1);
        }

        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Competitions.CopyTo(Database.Instance.Competitions);
            Database.Instance.IsDirty = true;
        }
    }
}
