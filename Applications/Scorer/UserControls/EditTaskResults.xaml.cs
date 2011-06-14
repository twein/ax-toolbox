using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;

namespace Scorer
{
    /// <summary>
    /// Interaction logic for EditTaskScores.xaml
    /// </summary>
    public partial class EditTaskResults : UserControl
    {
        public List<EditPilotScore> Score { get; set; }

        public EditTaskResults(Task task)
        {
            InitializeComponent();

            //http://www.i-programmer.info/programming/wpf-workings/620-using-the-wpf-net-40-datagrid-.html
            Score = new List<EditPilotScore>();
            Thread.Sleep(100);
            var rnd = new Random();
            foreach (var p in Database.Instance.Pilots)
            {
                var pscore = new EditPilotScore()
                {
                    TaskNumber = task.Number,
                    PilotNumber = p.Number,
                    PilotName = p.Name,
                    ManualMeasure = new Result(AXToolbox.Scripting.ResultType.No_Result),
                    //ManualMeasure = (decimal)(100 * rnd.NextDouble()),
                    ManualMeasurePenalty = rnd.NextDouble() < .05 ? (decimal)(100 * rnd.NextDouble()) : 0,
                    ManualTaskScorePenalty = rnd.NextDouble() < .05 ? (int)(100 * rnd.NextDouble()) : 0,
                    ManualCompetitionScorePenalty = rnd.NextDouble() < .05 ? (int)(100 * rnd.NextDouble()) : 0,
                    ManualInfringedRules = "some infringed rule"
                };
                Score.Add(pscore);
            }
            dgMain.DataContext = Score;

        }

        public class EditPilotScore : PilotResult
        {
            public string PilotName { get; set; }
        }

        private void buttonSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
