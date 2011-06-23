using System.Collections.ObjectModel;

namespace Scorer
{
    public partial class EditTaskResults : EditCollection<ResultInfo>
    {
        public EditTaskResults(ObservableCollection<ResultInfo> results, EditOptions editOptions)
            : base(results, editOptions, true)
        {
            InitializeComponent();
        }

        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //TODO: Hide the save button if validation fails

            SaveCollection[0].Task.Phases |= CompletedPhases.ManualResults | CompletedPhases.Dirty;
            Save();
        }
    }
}
