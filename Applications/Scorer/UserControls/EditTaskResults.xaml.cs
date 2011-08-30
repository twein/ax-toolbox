using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Scorer
{
    public partial class EditTaskResults : EditCollection<ResultInfo>
    {
        protected Task task;

        public EditTaskResults(Task task, ObservableCollection<ResultInfo> results, EditOptions editOptions)
            : base(results, editOptions, true)
        {
            InitializeComponent();

#if DEBUG
            buttonRandom.Visibility = System.Windows.Visibility.Visible;
#endif

            this.task = task;
        }

        private void buttonRandom_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var rnd = new Random();
            foreach (var r in DataGridCollection)
            {
                r.InfringedRules = "";

                int prob;
                prob = rnd.Next(100);
                if (prob < 5)
                    r.Measure = ResultInfo.ParseMeasure("NF");
                else if (prob < 15)
                    r.Measure = ResultInfo.ParseMeasure("NR");
                else
                {
                    r.Measure = 50 * (decimal)rnd.NextDouble();

                    if (rnd.Next(100) < 5)
                    {
                        r.MeasurePenalty = 50 * (decimal)rnd.NextDouble();
                        r.InfringedRules += "Some rule ";
                    }
                }

                if (rnd.Next(100) < 5)
                {
                    r.TaskScorePenalty = rnd.Next(50) * 10;
                    r.InfringedRules += "Some rule ";
                }

                if (rnd.Next(100) < 5)
                {
                    r.CompetitionScorePenalty = rnd.Next(50) * 10;
                    r.InfringedRules += "Some rule ";
                }
            }
        }

        private void buttonSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //TODO: Hide the save button if validation fails
            SaveCollection[0].Task.Phases |= CompletedPhases.ManualResults;
            Save();

            //compute the scores
            task.ComputeScores();
        }
    }
}
