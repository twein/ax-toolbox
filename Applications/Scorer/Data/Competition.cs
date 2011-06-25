using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using AXToolbox.Common;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

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

            name = "enter competition name";
            shortName = "enter short name (for file names)";
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

            //TODO: recompute task scores

            RaisePropertyChanged("Pilots");
            RaisePropertyChanged("Status");
        }
        public void ResetTasks()
        {
            Tasks.Clear();
            foreach (var t in Event.Instance.Tasks)
                Tasks.Add(t);

            //TODO: recompute task scores

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

        /// <summary>Generate a pdf total scores sheet
        /// </summary>
        /// <param header="fileName">desired pdf file path</param>
        public void TotalScoreToPdf(string pdfFileName)
        {
            var config = Event.Instance.GetDefaultPdfConfig();
            config.HeaderLeft = Name;

            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.Document;
            var title = Name + " total score";

            //title
            document.Add(new Paragraph(Name, config.TitleFont));
            //subtitle
            document.Add(new Paragraph(title, config.SubtitleFont) { SpacingAfter = 10 });


            //table
            var headers = new List<string>() { "Rank", "#", "Name", "TOTAL", "Average" };
            var relWidths = new List<float>() { 2, 2, 6, 3, 3 };
            foreach (var t in Tasks)
            {
                headers.Add("Task " + t.UltraShortDescription);
                relWidths.Add(3);
            }
            var table = helper.NewTable(headers.ToArray(), relWidths.ToArray(), title);

            //scores
            var pilotTotalScores = new List<PilotTotalScore>();
            foreach (var p in Pilots)
                pilotTotalScores.Add(new PilotTotalScore(this, p));

            var rank = 0;
            var i = 1;
            var lastScore = int.MinValue;
            foreach (var pts in pilotTotalScores.OrderByDescending(s => s.Total))
            {
                if (pts.Total != lastScore)
                {
                    lastScore = pts.Total;
                    rank = i;
                }

                table.AddCell(helper.NewRCell(rank.ToString()));
                table.AddCell(helper.NewRCell(pts.Pilot.Number.ToString()));
                table.AddCell(helper.NewLCell(pts.Pilot.Name));
                table.AddCell(new PdfPCell(new Paragraph(pts.Total.ToString(), config.BoldFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(helper.NewRCell(pts.Average.ToString()));

                foreach (var taskScore in pts.TaskScores)
                    table.AddCell(helper.NewRCell(taskScore.ToString()));

                i++;
            }

            //checksums
            table.AddCell(helper.NewLCell(""));
            table.AddCell(helper.NewLCell(""));
            table.AddCell(new PdfPCell(new Paragraph("Checksum", config.ItalicFont)) { HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(helper.NewLCell(""));
            table.AddCell(helper.NewLCell(""));
            foreach (var taskScore in TaskScores)
                table.AddCell(new PdfPCell(new Paragraph(taskScore.CheckSum, config.ItalicFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });

            document.Add(table);

            document.Close();
        }
        /// <summary>Generate a pdf with all task scores
        /// </summary>
        /// <param header="fileName"></param>
        public void TaskScoresTo1Pdf(string pdfFileName)
        {
            var config = Event.Instance.GetDefaultPdfConfig();
            config.HeaderLeft = Name;

            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.Document;

            var isFirstTask = true;
            foreach (var ts in TaskScores)
            {
                if (!isFirstTask)
                    document.NewPage();

                ts.ScoresToTable(helper);
                isFirstTask = false;
            }

            document.Close();
        }
        /// <summary>Generate a pdf for each task score</summary>
        /// <param name="folder">folder where to place the generated pdf files</param>
        public void TaskScoresToNPdf(string folder)
        {
            foreach (var ts in TaskScores)
            {
                var fileName = Path.Combine(folder, ts.GetPdfScoresFileName());
                ts.ScoresToPdf(fileName);
            }
        }
    }
}
