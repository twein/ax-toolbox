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

        /*
        private void dgMain_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var value = ((TextBox)e.EditingElement).Text.ToUpper();
                switch (e.Column.DisplayIndex)
                {
                    case 2:
                        Result result;
                        decimal tmpResult;
                        if (decimal.TryParse(value, out tmpResult))
                        {
                            result = new Result(tmpResult);
                            e.Cancel = false;
                        }
                        else if (value == "NF")
                        {
                            result = new Result(AXToolbox.Scripting.ResultType.No_Flight);
                            e.Cancel = false;
                        }
                        else if (value == "NR")
                        {
                            result = new Result(AXToolbox.Scripting.ResultType.No_Result);
                            e.Cancel = false;
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                        break;
                }
            }
        }
         */
    }
}
