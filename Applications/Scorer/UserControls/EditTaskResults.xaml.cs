using System.Collections.ObjectModel;

namespace Scorer
{
    public partial class EditTaskResults : EditCollection<PilotResult>
    {
        public EditTaskResults(ObservableCollection<PilotResult> taskResults, EditOptions editOptions)
            : base(taskResults, editOptions)
        {
            InitializeComponent();
        }
    }
}
