using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
