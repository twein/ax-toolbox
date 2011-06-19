using System.Collections.ObjectModel;

namespace Scorer
{
    public partial class EditTaskResults : EditCollection<PilotResult>
    {
        public EditTaskResults(ObservableCollection<PilotResult> taskResults, EditOptions editOptions)
            : base(taskResults, editOptions, true)
        {
            InitializeComponent();
        }

        private void saveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //TODO: Hide the save button if validation fails
            Save();
        }
    }
}
