using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using AXToolbox.PdfHelpers;
using iTextSharp.text;

namespace Scorer
{
    [Serializable]
    public sealed class Event : BindableObject
    {
        #region "singleton"
        public static readonly Event Instance = new Event();
        static Event() { }
        #endregion

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                isNew = false;
                RaisePropertyChanged("Name");
                RaisePropertyChanged("SaveVisibility");
            }
        }
        private string shortName;
        public string ShortName
        {
            get { return shortName; }
            set
            {
                shortName = value;
                isNew = false;
                RaisePropertyChanged("ShortName");
                RaisePropertyChanged("SaveVisibility");
            }
        }
        private string locationDates;
        public string LocationDates
        {
            get { return locationDates; }
            set
            {
                locationDates = value;
                isNew = false;
                RaisePropertyChanged("LocationDates");
                RaisePropertyChanged("SaveVisibility");
            }
        }
        private string director;
        public string Director
        {
            get { return director; }
            set
            {
                director = value;
                isNew = false;
                RaisePropertyChanged("Director");
                RaisePropertyChanged("SaveVisibility");
            }
        }

        private ObservableCollection<Competition> competitions;
        public ObservableCollection<Competition> Competitions
        {
            get { return competitions; }
            set
            {
                competitions = value;
                RaisePropertyChanged("Competitions");
            }
        }
        private ObservableCollection<Pilot> pilots;
        public ObservableCollection<Pilot> Pilots
        {
            get { return pilots; }
            set
            {
                pilots = value;
                RaisePropertyChanged("Pilots");
            }
        }
        private ObservableCollection<Task> tasks;
        public ObservableCollection<Task> Tasks
        {
            get { return tasks; }
            set
            {
                tasks = value;
                RaisePropertyChanged("Tasks");
            }
        }

        [NonSerialized]
        private string filePath;
        [XmlIgnore]
        public string FilePath
        {
            get { return filePath; }
            private set
            {
                filePath = value;
                RaisePropertyChanged("FilePath");
            }
        }

        [NonSerialized]
        private bool isNew = true;

        private Event()
        {
            name = "new event";
            shortName = "short name";
            locationDates = "location and dates";
            director = "event director";

            competitions = new ObservableCollection<Competition>();
            pilots = new ObservableCollection<Pilot>();
            tasks = new ObservableCollection<Task>();

            pilots.CollectionChanged += Pilots_CollectionChanged;
            tasks.CollectionChanged += Tasks_CollectionChanged;
        }

        public override void AfterPropertyChanged(string propertyName)
        {
            if (propertyName != "IsDirty")
                Event.Instance.IsDirty = true;
        }
        void Pilots_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(Tasks.Count == 0, "Can not modify pilot list if there are tasks defined");

            Event.Instance.IsDirty = true;

            if (Competitions.Count > 0)
            {
                //update the pilot list for each competition
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var c in Competitions)
                            c.Pilots.Clear();
                        break;

                    case NotifyCollectionChangedAction.Add:
                        foreach (var c in Competitions)
                            foreach (Pilot p in e.NewItems)
                                c.Pilots.Add(p);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var c in Competitions)
                            foreach (Pilot p in e.OldItems)
                                c.Pilots.Remove(p);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
        void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(Pilots.Count > 0, "Can not modify task list if there are no pilots defined");

            Event.Instance.IsDirty = true;

            if (Competitions.Count > 0)
            {
                //update the task list for each competition
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var c in Competitions)
                            c.Tasks.Clear();
                        break;

                    case NotifyCollectionChangedAction.Add:
                        foreach (var c in Competitions)
                            foreach (Task p in e.NewItems)
                                c.Tasks.Add(p);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var c in Competitions)
                            foreach (Task p in e.OldItems)
                                c.Tasks.Remove(p);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            pilots.CollectionChanged += Pilots_CollectionChanged;
            tasks.CollectionChanged += Tasks_CollectionChanged;
        }
        public void Save(string fileName, SerializationFormat serializationFormat = SerializationFormat.DataContract)
        {
            FilePath = fileName;
            ObjectSerializer<Event>.Save(this, fileName, serializationFormat);

            IsDirty = false;
            isNew = false;
        }
        public void Load(string fileName, SerializationFormat serializationFormat = SerializationFormat.DataContract)
        {
            var evt = ObjectSerializer<Event>.Load(fileName, serializationFormat);

            Name = evt.name;
            ShortName = evt.ShortName;
            LocationDates = evt.LocationDates;
            Director = evt.Director;
            Competitions = evt.Competitions;
            Pilots = evt.Pilots;
            Tasks = evt.Tasks;

            FilePath = fileName;

            IsDirty = false;
            isNew = false;
        }

        public void PilotListToPdf(string folder, bool openAfterCreation = false)
        {
            var fileName = Path.Combine(folder, ShortName + " pilot list.pdf");
            Pilot.ListToPdf(fileName, "Pilot list", Pilots);

            if (openAfterCreation)
                PdfHelper.OpenPdf(fileName);
        }
        public void WorkListToPdf(string folder, bool openAfterCreation = false)
        {
            var fileName = Path.Combine(folder, ShortName + " work list.pdf");
            Pilot.WorkListToPdf(fileName, "Work list", Pilots);

            if (openAfterCreation)
                PdfHelper.OpenPdf(fileName);
        }

        public string GetProgramInfo()
        {
            var assembly = GetType().Assembly;
            var aName = assembly.GetName();
            var aTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var aCopyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(aTitle.Length > 0 && aCopyright.Length > 0, "Assembly information incomplete");

            return string.Format("{0} {2}",
                ((AssemblyTitleAttribute)aTitle[0]).Title,
                aName.Version,
                ((AssemblyCopyrightAttribute)aCopyright[0]).Copyright);
            //return string.Format("{0} v{1} {2}",
            //    ((AssemblyTitleAttribute)aTitle[0]).Title,
            //    aName.Version,
            //    ((AssemblyCopyrightAttribute)aCopyright[0]).Copyright);
        }
        public PdfConfig GetDefaultPdfConfig()
        {
            return new PdfConfig()
            {
                PageLayout = PageSize.A4.Rotate(),
                MarginTop = 1.5f * PdfHelper.cm2pt,
                MarginBottom = 1.5f * PdfHelper.cm2pt,

                FooterRight = Event.Instance.GetProgramInfo()
            };
        }

        public Visibility SaveVisibility
        {
            get
            {
                if (!isNew)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public void ImportFile(string fileName)
        {
            var records = from line in File.ReadAllLines(fileName)
                          where !string.IsNullOrEmpty(line.Trim()) && !line.Trim().StartsWith("#")
                          select line.Split(new char[] { '\t', ';' });

            foreach (var record in records)
            {
                switch (record[0].ToLower())
                {
                    case "event":
                        ImportEvent(record);
                        break;
                    case "competition":
                        ImportCompetition(record);
                        break;
                    case "pilot":
                        ImportPilot(record);
                        break;
                    case "task":
                        ImportTask(record);
                        break;
                    case "result":
                        ImportResult(record);
                        break;
                }
            }
        }
        private void ImportEvent(string[] record)
        {
            Name = record[1].Trim();
            ShortName = record[2].Trim();
            LocationDates = record[3].Trim();
            Director = record[4].Trim();
        }
        private void ImportCompetition(string[] record)
        {
            var shortName = record[2].Trim();

            Competition competition;
            if (Competitions.Any(c => c.ShortName == shortName))
            {
                competition = Competitions.First(c => c.ShortName == shortName);
            }
            else
            {
                if (Tasks.Count() > 0 || Pilots.Count() > 0)
                    throw new InvalidOperationException("Competition import is not allowed when there are pilots or tasks defined");

                competition = new Competition() { ShortName = shortName };
                Competitions.Add(competition);
            }

            competition.Name = record[1].Trim();
        }
        private void ImportPilot(string[] record)
        {
            if (Competitions.Count() == 0)
                throw new InvalidOperationException("Pilot import is not allowed when no competitions are defined");

            var number = int.Parse(record[1]);

            Pilot pilot;
            if (Pilots.Any(p => p.Number == number))
            {
                pilot = Pilots.First(p => p.Number == number);
            }
            else
            {
                if (Tasks.Count() > 0)
                    throw new InvalidOperationException("Cannot add pilots when there are tasks defined");

                pilot = new Pilot() { Number = number };
                Pilots.Add(pilot);
            }

            pilot.Name = record[2].Trim();
            pilot.Country = (record.Length > 3) ? record[3].Trim() : "";
            pilot.Balloon = (record.Length > 4) ? record[4].Trim() : "";
        }
        private void ImportTask(string[] record)
        {
            if (Competitions.Count() == 0 || Pilots.Count() == 0)
                throw new InvalidOperationException("Task import is not allowed when no competitions or pilots are defined");

            var taskNumber = int.Parse(record[1]);
            Task task;
            if (!Tasks.Any(t => t.Number == taskNumber))
            {
                task = new Task() { Number = taskNumber };
                Tasks.Add(task);
            }
            else
            {
                task = Tasks.First(t => t.Number == taskNumber);
            }

            var type = record[2].ToUpper().Trim();
            if (!Task.Types.Any(t => t.ShortName == type))
                throw new InvalidOperationException("Inexistent task type " + type);
            var typeNumber = Task.Types.First(t => t.ShortName == type).Number;
            task.TypeNumber = typeNumber;

            if (record.Count() > 3)
            {
                var date = (new DateConverter()).ConvertBack(record[3], typeof(DateTime), null, null);
                if (date is DateTime)
                {
                    task.Date = (DateTime)date;
                }
                else
                    task.Date = DateTime.Now;
            }
        }
        private void ImportResult(string[] record)
        {
            var auto = record[1].ToUpper().Trim() == "AUTO";

            var taskNumber = int.Parse(record[2]);
            if (!Tasks.Any(t => t.Number == taskNumber))
                throw new InvalidOperationException("Inexistent task " + taskNumber.ToString());
            var pilotNumber = int.Parse(record[3]);

            if (!Pilots.Any(p => p.Number == pilotNumber))
                throw new InvalidOperationException("Inexistent pilot " + pilotNumber.ToString());

            var task = Tasks.First(t => t.Number == taskNumber);
            var pilotResult = task.PilotResults.First(pr => pr.Pilot.Number == pilotNumber);
            var result = auto ? pilotResult.AutoResultInfo : pilotResult.ManualResultInfo;

            var measure = ResultInfo.ParseMeasure(record[4]);
            var measurePenalty = ResultInfo.ParseMeasure(record[5]);
            var taskScorePenalty = int.Parse(record[6]);
            var competitionScorePenalty = int.Parse(record[7]);

            if (result.Measure != measure || result.MeasurePenalty != measurePenalty
                || result.TaskScorePenalty != taskScorePenalty || result.CompetitionScorePenalty != competitionScorePenalty)
            {
                result.Measure = measure;
                result.MeasurePenalty = measurePenalty;
                result.TaskScorePenalty = taskScorePenalty;
                result.CompetitionScorePenalty = competitionScorePenalty;

                task.Phases |= CompletedPhases.Dirty | (auto ? CompletedPhases.AutoResults : CompletedPhases.ManualResults);
            }
            result.InfringedRules = record[8].Trim();
        }
    }
}
