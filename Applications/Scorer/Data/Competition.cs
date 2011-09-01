using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using AXToolbox.Common;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Scorer
{
    [Serializable]
    public class Competition : BindableObject
    {
        protected string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged("Name");
            }
        }
        protected string shortName;
        public string ShortName
        {
            get { return shortName; }
            set
            {
                shortName = value;
                RaisePropertyChanged("ShortName");
                RaisePropertyChanged("Status");
            }
        }

        public ObservableCollection<Pilot> Pilots { get; set; }
        public ObservableCollection<Task> Tasks { get; set; }
        public List<TaskScore> TaskScores { get; set; }

        public string Status
        {
            get { return string.Format("{0}: {1} pilots, {2} tasks", ShortName, Pilots.Count, Tasks.Count); }
        }

        public Competition()
        {
            Pilots = new ObservableCollection<Pilot>();
            Tasks = new ObservableCollection<Task>();
            TaskScores = new List<TaskScore>();

            Pilots.CollectionChanged += Pilots_CollectionChanged;
            Tasks.CollectionChanged += Tasks_CollectionChanged;

            name = "new competition";
            shortName = "short name";
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Pilots.CollectionChanged += Pilots_CollectionChanged;
            Tasks.CollectionChanged += Tasks_CollectionChanged;
        }

        void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(Event.Instance.Tasks.Count == 0, "Can not modify pilot list if there are tasks defined");
            RaisePropertyChanged("Pilots");
            RaisePropertyChanged("Status");
        }
        void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(Event.Instance.Pilots.Count > 0, "Can not modify task list if there are no pilots defined");

            //update the task scores list
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    TaskScores.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                    foreach (Task t in e.NewItems)
                        TaskScores.Add(new TaskScore(this, t));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Task t in e.OldItems)
                    {
                        var old_ts = TaskScores.First(ts => ts.Task == t);
                        TaskScores.Remove(old_ts);
                    }

                    break;

                default:
                    throw new NotImplementedException();
            }

            RaisePropertyChanged("Tasks");
            RaisePropertyChanged("Status");
        }

        public void ResetPilots()
        {
            Pilots.Clear();
            foreach (var p in Event.Instance.Pilots)
                Pilots.Add(p);

            RaisePropertyChanged("Pilots");
            RaisePropertyChanged("Status");
        }
        public void ResetTasks()
        {
            Tasks.Clear();
            foreach (var t in Event.Instance.Tasks)
                Tasks.Add(t);

            RaisePropertyChanged("Tasks");
            RaisePropertyChanged("Status");
        }

        public override void AfterPropertyChanged(string propertyName)
        {
            Event.Instance.IsDirty = true;
        }

        public override string ToString()
        {
            return Name;
        }

        public void PilotListToPdf(bool openAfterCreation = false)
        {
            var fileName = Path.Combine(Event.Instance.DraftsFolder, ShortName + " pilot list.pdf");
            Pilot.ListToPdf(fileName, "Pilot list", Pilots);

            if (openAfterCreation)
                PdfHelper.OpenPdf(fileName);
        }
        public void TotalScoreToPdf(bool openAfterCreation = false)
        {
            const int maxTasksPerSheet = 10;

            var fileName = Path.Combine(Event.Instance.DraftsFolder, ShortName + " total score.pdf");
            var config = Event.Instance.GetDefaultPdfConfig();

            var helper = new PdfHelper(fileName, config);
            var document = helper.Document;
            var title = "Total score";

            //tables
            var validTaskScores = (from ts in TaskScores
                                   where !ts.Task.IsCancelled && (ts.Task.Phases & CompletedPhases.Computed) > 0
                                   orderby ts.Task.Number
                                   select ts).ToArray();

            var nTasks = validTaskScores.Count();
            var nTables = (int)Math.Floor((decimal)(nTasks - 1) / maxTasksPerSheet) + 1;
            var nTasksTable = (int)Math.Ceiling((decimal)nTasks / nTables);
            var tables = new PdfPTable[nTables];

            //Create tables with headers
            for (var iTable = 0; iTable < nTables; iTable++)
            {
                var headers = new List<string>() { "Rank", "Pilot", "TOTAL", "Average" };
                var relWidths = new List<float>() { 2, 8, 2, 2 };
                for (var iTask = iTable * nTasksTable; iTask < (int)Math.Min(nTasks, (iTable + 1) * nTasksTable); iTask++)
                {
                    headers.Add("T" + validTaskScores[iTask].UltraShortDescriptionStatus);
                    relWidths.Add(2);
                }
             
                tables[iTable] = helper.NewTable(headers.ToArray(), relWidths.ToArray(), title);
            }

            //insert scores
            var pilotTotalScores = new List<PilotTotalScore>();
            foreach (var p in Pilots)
                pilotTotalScores.Add(new PilotTotalScore(this, p));

            var rank = 1;
            foreach (var pts in pilotTotalScores.OrderByDescending(s => s.Total).ThenByDescending(s => s.Average).ThenBy(s => s.Pilot.Number))
            {
                for (var iTable = 0; iTable < nTables; iTable++)
                {
                    tables[iTable].AddCell(helper.NewRCell(rank.ToString()));
                    tables[iTable].AddCell(helper.NewLCell(pts.Pilot.Info));
                    tables[iTable].AddCell(new PdfPCell(new Paragraph(pts.Total.ToString(), config.BoldFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    tables[iTable].AddCell(helper.NewRCell(pts.Average.ToString()));
                }

                var iTask = 0;
                foreach (var taskScore in pts.TaskScores)
                {
                    var iTable = (int)Math.Floor((decimal)iTask / nTasksTable);
                    tables[iTable].AddCell(helper.NewRCell(taskScore.ToString()));
                    iTask++;
                }

                //TODO: fix complete ties in total and average scores
                rank++;
            }

            //checksums
            for (var iTable = 0; iTable < nTables; iTable++)
            {
                tables[iTable].AddCell(helper.NewLCell(""));
                tables[iTable].AddCell(new PdfPCell(new Paragraph("Checksum", config.ItalicFont)) { HorizontalAlignment = Element.ALIGN_LEFT });
                tables[iTable].AddCell(helper.NewLCell(""));
                tables[iTable].AddCell(helper.NewLCell(""));
            }

            {
                var iTask = 0;
                foreach (var taskScore in validTaskScores)
                {
                    var iTable = (int)Math.Floor((decimal)iTask / nTasksTable);
                    tables[iTable].AddCell(new PdfPCell(new Paragraph(taskScore.CheckSum, config.ItalicFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    iTask++;
                }
            }

            foreach (var t in tables)
            {
                //title
                document.Add(new Paragraph(Name, config.TitleFont));
                //subtitle
                document.Add(new Paragraph(title, config.SubtitleFont) { SpacingAfter = config.SubtitleFont.Size });
                //table
                document.Add(t);

                document.NewPage();
            }

            document.Close();

            if (openAfterCreation)
                PdfHelper.OpenPdf(fileName);
        }
        public void TaskScoresTo1Pdf(string folder, bool openAfterCreation = false)
        {
            var fileName = Path.Combine(folder, ShortName + " task scores.pdf");
            var config = Event.Instance.GetDefaultPdfConfig();

            var helper = new PdfHelper(fileName, config);
            var document = helper.Document;

            var validTScores = from ts in TaskScores
                               where !ts.Task.IsCancelled && (ts.Task.Phases & CompletedPhases.Computed) > 0
                               orderby ts.Task.Number
                               select ts;
            var isFirstTask = true;
            foreach (var ts in validTScores)
            {
                if (!isFirstTask)
                    document.NewPage();

                ts.ScoresToTable(helper);
                isFirstTask = false;
            }

            document.Close();

            if (openAfterCreation)
                PdfHelper.OpenPdf(fileName);
        }
        public void TaskScoresToNPdf()
        {
            var validTScores = from ts in TaskScores
                               where !ts.Task.IsCancelled && (ts.Task.Phases & CompletedPhases.Computed) > 0
                               orderby ts.Task.Number
                               select ts;
            foreach (var ts in validTScores)
                ts.ScoresToPdf(false);
        }
    }
}
