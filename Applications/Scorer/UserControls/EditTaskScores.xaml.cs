using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;

namespace Scorer
{
    /// <summary>
    /// Interaction logic for EditTaskScores.xaml
    /// </summary>
    public partial class EditTaskScores : UserControl
    {
        public List<EditPilotScore> Score { get; set; }

        public EditTaskScores()
        {
            InitializeComponent();

            //http://www.i-programmer.info/programming/wpf-workings/620-using-the-wpf-net-40-datagrid-.html
            Score = new List<EditPilotScore>();
            Thread.Sleep(100);
            var rnd = new Random();
            for (var i = 0; i < 30; i++)
            {
                var pscore = new EditPilotScore()
                {
                    PilotNumber = i + 1,
                    PilotName = string.Format("Pilot number {0:##0}", i + 1),
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
    }
}
