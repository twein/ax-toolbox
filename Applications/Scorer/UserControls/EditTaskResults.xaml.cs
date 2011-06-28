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
        public EditTaskResults(ObservableCollection<ResultInfo> results, EditOptions editOptions)
            : base(results, editOptions, true)
        {
            InitializeComponent();
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

        private void buttonImport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "CSV files (*.csv)|*.csv";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                ImportResults(dlg.FileName);
        }

        private void buttonSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //TODO: Hide the save button if validation fails
            SaveCollection[0].Task.Phases |= CompletedPhases.ManualResults | CompletedPhases.Dirty;
            Save();
        }

        private void ImportResults(string filePath)
        {
            var resultList = File.ReadAllLines(filePath);
            int i = 0;
            try
            {
                foreach (var p in resultList)
                {
                    i++;
                    var resultStr = p.Trim();
                    if (resultStr != "" && resultStr[0] != '#')
                    {
                        var fields = resultStr.Split(new char[] { '\t', ';' }, StringSplitOptions.None);

                        var number = int.Parse(fields[0]);
                        var measure = ResultInfo.ParseMeasure(fields[1]);
                        var measurePenalty = decimal.Parse(fields[2]);
                        var taskPoints = int.Parse(fields[3]);
                        var competitionPoints = int.Parse(fields[4]);
                        var infringedRules = fields[5];

                        try
                        {
                            var result = DataGridCollection.First(r => r.Pilot.Number == number);
                            result.Measure = ResultInfo.ParseMeasure(fields[1]);
                            result.MeasurePenalty = decimal.Parse(fields[2]);
                            result.TaskScorePenalty = int.Parse(fields[3]);
                            result.CompetitionScorePenalty = int.Parse(fields[4]);
                            result.InfringedRules = fields[5];
                        }
                        catch (InvalidOperationException)
                        {
                            //not found
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error in line " + i.ToString() + ":" + Environment.NewLine + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
            }
        }
    }
}
