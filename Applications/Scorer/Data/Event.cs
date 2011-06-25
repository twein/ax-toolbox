using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.Common.IO;
using System.Runtime.Serialization;
using System.Reflection;
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
                IsDirty = true;
                name = value;
                RaisePropertyChanged("Name");
            }
        }
        private string shortName;
        public string ShortName
        {
            get { return shortName; }
            set
            {
                shortName = value;
                RaisePropertyChanged("ShortName");
            }
        }
        private string locationDates;
        public string LocationDates
        {
            get { return locationDates; }
            set
            {
                locationDates = value;
                RaisePropertyChanged("LocationDates");
            }
        }
        private string director;
        public string Director
        {
            get { return director; }
            set
            {
                director = value;
                RaisePropertyChanged("Director");
            }
        }

        [NonSerialized]
        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            private set
            {
                filePath = value;
                RaisePropertyChanged("FilePath");
            }
        }

        private ObservableCollection<Competition> competitions;
        public ObservableCollection<Competition> Competitions
        {
            get { return competitions; }
            private set
            {
                competitions = value;
                RaisePropertyChanged("Competitions");
            }
        }
        private ObservableCollection<Pilot> pilots;
        public ObservableCollection<Pilot> Pilots
        {
            get { return pilots; }
            private set
            {
                pilots = value;
                RaisePropertyChanged("Pilots");
            }
        }
        private ObservableCollection<Task> tasks;
        public ObservableCollection<Task> Tasks
        {
            get { return tasks; }
            private set
            {
                tasks = value;
                RaisePropertyChanged("Tasks");
            }
        }

        private Event()
        {
            name = "enter event name";
            shortName = "enter short name (for file names)";
            locationDates = "enter location and dates";
            director = "enter event director name";

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
        }

        public string GetProgramInfo()
        {
            var assembly = GetType().Assembly;
            var aName = assembly.GetName();
            var aTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var aCopyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(aTitle.Length > 0 && aCopyright.Length > 0, "Assembly information incomplete");

            return string.Format("{0} v{1} {2}",
                ((AssemblyTitleAttribute)aTitle[0]).Title,
                aName.Version,
                ((AssemblyCopyrightAttribute)aCopyright[0]).Copyright);
        }
        public PdfConfig GetDefaultPdfConfig()
        {
            return new PdfConfig()
            {
                PageLayout = PageSize.A4.Rotate(),
                MarginTop = 1.5f * PdfHelper.cm2pt,
                MarginBottom = 1.5f * PdfHelper.cm2pt,

                HeaderCenter = LocationDates,
                HeaderRight = "Event director: " + Director,
                FooterLeft = string.Format("Printed on {0}", DateTime.Now),
                FooterRight = Event.Instance.GetProgramInfo()
            };
        }
    }
}
