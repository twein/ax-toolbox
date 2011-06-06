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
    public partial class MainWindow : Window
    {
        public List<EditPilotScore> Score { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            //http://www.i-programmer.info/programming/wpf-workings/620-using-the-wpf-net-40-datagrid-.html
            Score = new List<EditPilotScore>();
            var rnd = new Random();
            for (var i = 0; i < 90; i++)
            {
                var pscore = new EditPilotScore()
                {
                    PilotNumber = i + 1,
                    PilotName = string.Format("Pilot number {0:##0}", i + 1),
                    ManualMeasure = (decimal)(100 * rnd.NextDouble()),
                    ManualMeasurePenalty = (decimal)(100 * rnd.NextDouble()),
                    ManualTaskScorePenalty = (int)(100 * rnd.NextDouble()),
                    ManualCompetitionScorePenalty = (int)(100 * rnd.NextDouble()),
                    ManualInfringedRules = "some infringed rule"
                };
                Score.Add(pscore);
            }
            dgMain.DataContext = Score;
        }
    }
}
