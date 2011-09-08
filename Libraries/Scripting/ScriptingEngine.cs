using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;
using AXToolbox.PdfHelpers;
using iTextSharp.text;

namespace AXToolbox.Scripting
{
    [Flags]
    public enum OverlayLayers : uint
    {
        All = 0xFFFFFFFF,

        Grid = 0x1,
        Areas = 0x2,
        Static_Points = 0x4,
        Track = 0x8,
        Pointer = 0x10,
        Pilot_Points = 0x20,
        Launch_And_Landing = 0x40,
        Reference_Points = 0x80,
        Results = 0x100,
        Penalties = 0x200
    }
    public enum TrackTypes { OriginalTrack, CleanTrack, FligthTrack }

    public sealed class ScriptingEngine : BindableObject
    {
        public string ShortDescription
        {
            get
            {
                var tasks = "";
                foreach (var item in Heap.Values.Where(h => h is ScriptingTask))
                    tasks += ((ScriptingTask)item).ToShortString() + ", ";
                tasks = tasks.Trim(new char[] { ',', ' ' });
                return string.Format("{0}: {1}", Settings.ToString(), tasks);
            }
        }
        public string Detail
        {
            get
            {
                var str = "";
                foreach (var i in Heap.Values)
                    str += i.ToString() + Environment.NewLine;
                return str;
            }
        }
        public FlightSettings Settings { get; private set; }
        internal Dictionary<string, ScriptingObject> Heap { get; private set; }
        public FlightReport Report { get; private set; }
        public MapViewerControl MapViewer { get; private set; }

        internal AXTrackpoint[] AllValidTrackPoints { get; set; }
        internal AXTrackpoint[] TaskValidTrackPoints { get; set; }
        /// <summary>returns the last used point: last used marker drop or launch</summary>
        internal AXPoint LastUsedPoint
        {
            get
            {
                AXPoint lastPoint = null;

                var usedPoints = (from t in Heap.Values
                                  where t is ScriptingTask
                                  where ((ScriptingTask)t).Result != null
                                  where ((ScriptingTask)t).Result.LastUsedPoint != null
                                  select ((ScriptingTask)t).Result.LastUsedPoint).ToList();
                usedPoints.Add(Report.LaunchPoint);

                try
                {
                    //find last used point
                    lastPoint = usedPoints.OrderBy(p => p.Time).Last();
                }
                catch { }

                return lastPoint;
            }
        }
        public TrackTypes VisibleTrackType { get; set; }
        public AXTrackpoint[] VisibleTrack
        {
            get
            {
                AXTrackpoint[] track;
                if (Report == null)
                {
                    track = new AXTrackpoint[0];
                }
                else
                {
                    switch (VisibleTrackType)
                    {
                        case TrackTypes.OriginalTrack:
                            track = Report.OriginalTrack;
                            break;
                        case TrackTypes.CleanTrack:
                            track = Report.CleanTrack;
                            break;
                        case TrackTypes.FligthTrack:
                            track = Report.FlightTrack;
                            break;
                        default:
                            track = null;
                            break;
                    }
                }
                return track;
            }
        }

        public MapOverlay TrackPointer { get; private set; }
        public bool KeepPointerCentered { get; set; }

        public IEnumerable<string> Results
        {
            get
            {
                var taskQuery = from t in Heap.Values
                                where t is ScriptingTask
                                select t as ScriptingTask;

                foreach (var t in taskQuery)
                    if (t.Result != null)
                        yield return string.Format("Task {0:00} {1}: {2}", t.Number, t.ObjectType, t.Result.ToString());
            }
        }
        public IEnumerable<string> Penalties
        {
            get
            {
                var taskQuery = from t in Heap.Values
                                where t is ScriptingTask
                                select t as ScriptingTask;

                foreach (var t in taskQuery)
                    foreach (var p in t.Penalties)
                        yield return string.Format("Task {0:00} {1}: {2}", t.Number, t.ObjectType, p.ToString());
            }
        }
        public IEnumerable<string> Log
        {
            get
            {
                return GetLog(true);
            }
        }

        public IEnumerable<string> GetLog(bool importantOnly)
        {
            var lines = new List<string>();
            foreach (var obj in Heap.Values)
                foreach (var note in obj.Notes.Where(n => importantOnly ? n.IsImportant : true))
                    lines.Add(obj.ObjectName + ": " + note.Text);

            return lines;
        }

        public ScriptingEngine(MapViewerControl mapViewer)
        {
            MapViewer = mapViewer;
            Settings = new FlightSettings();
            Heap = new Dictionary<string, ScriptingObject>();
        }

        public void LoadScript(string scriptFileName)
        {
            Trace.WriteLine("Loading script '" + scriptFileName + "'", "ENGINE");

            Settings = new FlightSettings();
            Heap.Clear();
            Report = null;

            //TODO: initialize all variables

            Directory.SetCurrentDirectory(Path.GetDirectoryName(scriptFileName));

            var lines = File.ReadAllLines(scriptFileName);

            string line;
            int lineNumber = 0;
            try
            {
                for (lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    line = lines[lineNumber];

                    var obj = ScriptingObject.Create(this, line);

                    if (obj != null)
                    {
                        //place on heap
                        Heap.Add(obj.ObjectName, obj);
                    }
                }
            }
            catch (Exception ex)
            {
                var message = "line " + (lineNumber + 1).ToString() + ": " + ex.Message;
                Trace.WriteLine("Exception parsing " + message, "ENGINE");
                throw new ArgumentException(message);
            }

            if (!Settings.AreWellInitialized())
            {
                var message = "The settings are not fully initialized";
                Trace.WriteLine("Exception: " + message, "ENGINE");
                throw new ArgumentException(message);
            }

            RaisePropertyChanged("Settings");
            RaisePropertyChanged("ShortDescription");
            RaisePropertyChanged("Detail");

            Display(true);
        }
        public void LoadFlightReport(string loggerFile)
        {
            Trace.WriteLine("Loading " + loggerFile, "ENGINE");
            Reset();

            Report = FlightReport.Load(loggerFile, Settings);

            Display();

            RaisePropertyChanged("Log");
            RaisePropertyChanged("Report");
            RaisePropertyChanged("Results");
            RaisePropertyChanged("Penalties");
        }

        public void Reset()
        {
            TrackPointer = null;
            foreach (var obj in Heap.Values)
                obj.Reset();
            Report = null;
            RaisePropertyChanged("Report");
        }
        public void Process()
        {
            if (Report.PilotId == 0)
                throw new InvalidOperationException("The pilot id can not be zero");

            Trace.WriteLine("Processing " + Report.ToString(), "ENGINE");

            AllValidTrackPoints = Report.FlightTrack;

            //reset all objects
            foreach (var obj in Heap.Values)
                obj.Reset();

            //process all objects
            foreach (var obj in Heap.Values)
                obj.Process();

            Display();

            RaisePropertyChanged("Log");
            RaisePropertyChanged("Results");
            RaisePropertyChanged("Penalties");
        }
        public void SaveAll(string rootFolder)
        {
            if (Report.PilotId > 0)
            {
                var reportsFolder = Path.Combine(rootFolder, "Flight reports");
                if (!Directory.Exists(reportsFolder))
                    Directory.CreateDirectory(reportsFolder);

                var resultsFolder = Path.Combine(rootFolder, "Results");
                if (!Directory.Exists(resultsFolder))
                    Directory.CreateDirectory(resultsFolder);

                Report.Save(reportsFolder);
                Report.ExportTrackLog(reportsFolder);

                if (Results.Count() > 0)
                {
                    ExportResults(resultsFolder);
                    SavePdfReport(reportsFolder);
                }
            }
            else
                throw new InvalidOperationException("The pilot id can not be zero");
        }

        public void ExportResults(string folder)
        {
            if (Report.PilotId > 0)
            {
                var contents = new List<string>();
                var taskQuery = from t in Heap.Values
                                where t is ScriptingTask
                                select t as ScriptingTask;

                foreach (var t in taskQuery)
                    contents.Add(t.ToCsvString());

                File.WriteAllLines(Path.Combine(folder, Report.ToShortString() + ".csv"), contents);
            }
            else
                throw new InvalidOperationException("The pilot id can not be zero");
        }
        public void SavePdfReport(string folder)
        {
            var assembly = GetType().Assembly;
            var aName = assembly.GetName();
            var aTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var aCopyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(aTitle.Length > 0 && aCopyright.Length > 0, "Assembly information incomplete");

            var programInfo = string.Format("{0} {2}",
                ((AssemblyTitleAttribute)aTitle[0]).Title,
                aName.Version,
                ((AssemblyCopyrightAttribute)aCopyright[0]).Copyright);
            //return string.Format("{0} v{1} {2}",
            //    ((AssemblyTitleAttribute)aTitle[0]).Title,
            //    aName.Version,
            //    ((AssemblyCopyrightAttribute)aCopyright[0]).Copyright);


            var config = new PdfConfig(PdfConfig.Application.FlightAnalyzer)
            {
                FooterRight = programInfo
            };
            var helper = new PdfHelper(Path.Combine(folder, Report.ToShortString() + ".pdf"), config);
            var document = helper.Document;

            config.HeaderLeft = Settings.Title;
            config.HeaderRight = Settings.Subtitle;
            //document.Add(new Paragraph(Settings.Title, config.TitleFont));
            //document.Add(new Paragraph(Settings.Subtitle, config.SubtitleFont));
            document.Add(new Paragraph("Automatic Flight Report", config.TitleFont));

            var table = helper.NewTable(null, new float[] { 1, 1 });
            table.WidthPercentage = 50;
            table.HorizontalAlignment = Element.ALIGN_LEFT;

            table.AddCell(helper.NewLCell("Flight Date:"));
            table.AddCell(helper.NewLCell(Settings.Date.GetDateAmPm()));
            table.AddCell(helper.NewLCell("Pilot number:"));
            table.AddCell(helper.NewLCell(Report.PilotId.ToString()));
            table.AddCell(helper.NewLCell("Logger serial number:"));
            table.AddCell(helper.NewLCell(Report.LoggerSerialNumber));
            table.AddCell(helper.NewLCell("Debriefer:"));
            table.AddCell(helper.NewLCell(Report.Debriefer));

            document.Add(table);

            var taskQuery = from t in Heap.Values
                            where t is ScriptingTask
                            select t as ScriptingTask;
            foreach (var task in taskQuery)
                task.ToPdfReport(helper);

            document.NewPage();
            document.Add(new Paragraph("Measurement process log", config.SubtitleFont));

            foreach (var line in Report.Notes)
                document.Add(new Paragraph(line, config.FixedWidthFont));
            foreach (var line in GetLog(false))
                document.Add(new Paragraph(line, config.FixedWidthFont));

            document.Close();
        }

        public void Display(bool factoryReset = false)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                if (factoryReset)
                    MapViewer.Clear();
                else
                    MapViewer.ClearOverlays();

                foreach (var obj in Heap.Values)
                    obj.Display();

                if (Report != null)
                {
                    var path = new Point[VisibleTrack.Length];
                    Parallel.For(0, VisibleTrack.Length, i =>
                    {
                        path[i] = VisibleTrack[i].ToWindowsPoint();
                    });
                    MapViewer.AddOverlay(new TrackOverlay(path, 2) { Layer = (uint)OverlayLayers.Track });

                    var position = path[0];
                    if (TrackPointer != null)
                        position = TrackPointer.Position;
                    TrackPointer = new CrosshairsOverlay(position) { Layer = (uint)OverlayLayers.Pointer };
                    MapViewer.AddOverlay(TrackPointer);

                    MapViewer.AddOverlay(new WaypointOverlay(Report.LaunchPoint.ToWindowsPoint(), "Launch") { Layer = (uint)OverlayLayers.Launch_And_Landing });
                    MapViewer.AddOverlay(new WaypointOverlay(Report.LandingPoint.ToWindowsPoint(), "Landing") { Layer = (uint)OverlayLayers.Launch_And_Landing });

                    foreach (var m in Report.Markers)
                    {
                        MapViewer.AddOverlay(new MarkerOverlay(m.ToWindowsPoint(), "Marker " + m.Name) { Layer = (uint)OverlayLayers.Pilot_Points });
                    }
                }
                if (KeepPointerCentered)
                    MapViewer.PanTo(TrackPointer.Position);
            }));
        }
    }
}
