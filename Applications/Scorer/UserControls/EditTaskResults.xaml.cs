using System.Collections.ObjectModel;
using System;

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

        private void randomButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var rnd = new Random();
            foreach (var r in DataGridCollection)
                r.Measure = 50 * (decimal)rnd.NextDouble();
        }
    }
}
