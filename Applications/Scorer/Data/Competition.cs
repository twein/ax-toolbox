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

        public void PilotListToPdf(string folder, bool openAfterCreation = false)
        {
            var fileName = Path.Combine(folder, ShortName + " pilot list.pdf");
            Pilot.ListToPdf(fileName, "Pilot list", Pilots);

            if (openAfterCreation)
                PdfHelper.OpenPdf(fileName);
        }
        public void TotalScoreToPdf(string folder, bool openAfterCreation = false)
        {
            var fileName = Path.Combine(folder, ShortName + " total score.pdf");
            var config = Event.Instance.GetDefaultPdfConfig();

            var helper = new PdfHelper(fileName, config);
            var document = helper.Document;
            var title = "Total score";

            //title
            document.Add(new Paragraph(Name, config.TitleFont));
            //subtitle
            document.Add(new Paragraph(title, config.SubtitleFont) { SpacingAfter = config.SubtitleFont.Size });

            //table
            var validTaskScores = from ts in TaskScores
                             where (ts.Task.Phases & (CompletedPhases.Computed | CompletedPhases.Dirty)) == CompletedPhases.Computed
                             && !ts.Task.IsCancelled
                             orderby ts.Task.Number
                             select ts;

            var headers = new List<string>() { "Rank", "Pilot", "TOTAL", "Average" };
            var relWidths = new List<float>() { 2, 8, 2, 2 };
            foreach (var ts in validTaskScores)
            {
                headers.Add("T" + ts.UltraShortDescriptionStatus);
                relWidths.Add(2);
            }
            var table = helper.NewTable(headers.ToArray(), relWidths.ToArray(), title);

            //scores
            var pilotTotalScores = new List<PilotTotalScore>();
            foreach (var p in Pilots)
                pilotTotalScores.Add(new PilotTotalScore(this, p));

            var rank = 1;
            foreach (var pts in pilotTotalScores.OrderByDescending(s => s.Total).ThenByDescending(s => s.Average).ThenBy(s => s.Pilot.Number))
            {
                table.AddCell(helper.NewRCell(rank.ToString()));
                table.AddCell(helper.NewLCell(pts.Pilot.Info));
                table.AddCell(new PdfPCell(new Paragraph(pts.Total.ToString(), config.BoldFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(helper.NewRCell(pts.Average.ToString()));

                foreach (var taskScore in pts.TaskScores)
                    table.AddCell(helper.NewRCell(taskScore.ToString()));

                //TODO: fix complete ties in total and average scores
                rank++;
            }

            //checksums
            table.AddCell(helper.NewLCell(""));
            table.AddCell(new PdfPCell(new Paragraph("Checksum", config.ItalicFont)) { HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(helper.NewLCell(""));
            table.AddCell(helper.NewLCell(""));
            foreach (var taskScore in validTaskScores)
                table.AddCell(new PdfPCell(new Paragraph(taskScore.CheckSum, config.ItalicFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });

            document.Add(table);

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
                               where (ts.Task.Phases & (CompletedPhases.Computed | CompletedPhases.Dirty)) == CompletedPhases.Computed && !ts.Task.IsCancelled
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
        public void TaskScoresToNPdf(string folder)
        {
            var validTScores = from ts in TaskScores
                               where (ts.Task.Phases & (CompletedPhases.Computed | CompletedPhases.Dirty)) == CompletedPhases.Computed && !ts.Task.IsCancelled
                               orderby ts.Task.Number
                               select ts;
            foreach (var ts in validTScores)
                ts.ScoresToPdf(folder, false);
        }
    }
}
