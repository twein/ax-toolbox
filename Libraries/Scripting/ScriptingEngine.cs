﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AXToolbox.Scripting
{
    [Flags]
    public enum OverlayLayers : uint
    {
        Grid = 0x1,
        Track = 0x2,
        Pointer = 0x4,
        Areas = 0x8,
        TakeOff_And_Landing = 0x10,
        Static_Points = 0x20,
        Markers = 0x40,
        Pilot_Points = 0x80,
        Results = 0x100,
        Penalties = 0x200,

        All = 0xFFFFFFFF
    }
    public enum TrackTypes { OriginalTrack, CleanTrack, FligthTrack }

    public sealed class ScriptingEngine : BindableObject
    {
        public ScriptingEngine(MapViewerControl mapViewer)
        {
            MapViewer = mapViewer;
            Settings = new FlightSettings();
            Heap = new Dictionary<string, ScriptingObject>();
        }

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

        internal Track FlightValidTrack { get; set; }
        internal Track TaskValidTrack { get; set; }
        /// <summary>returns the last used point: last used marker drop or take off</summary>
        internal AXPoint LastUsedPoint
        {
            get
            {
                AXPoint lastPoint = null;

                var usedPoints = (from t in Heap.Values
                                  where t is ScriptingTask
                                    && ((ScriptingTask)t).Result != null
                                    && ((ScriptingTask)t).Result.LastUsedPoint != null
                                  select ((ScriptingTask)t).Result.LastUsedPoint).ToList();
                usedPoints.Add(Report.TakeOffPoint);

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
        public AXPoint[] VisibleTrack
        {
            get
            {
                AXPoint[] track;
                if (Report == null)
                {
                    track = new AXPoint[0];
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
                        yield return string.Format("Task {0:00} {1}: {2}", t.Number, t.Definition.ObjectType, t.Result.ToString());
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
                        yield return string.Format("Task {0:00} {1}: {2}", t.Number, t.Definition.ObjectType, p.ToString());
            }
        }
        public IEnumerable<Note> Log
        {
            get
            {
                return GetLog(true);
            }
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

                    var obj = ScriptingObject.Parse(this, line);

                    if (obj != null)
                    {
                        //place on heap
                        Heap.Add(obj.Definition.ObjectName, obj);
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
                var message = "Incomplete script: DATETIME, UTCOFFSET, DATUM, UTMZONE and MAP are required.";
                Trace.WriteLine("Exception: " + message, "ENGINE");
                throw new ArgumentException(message);
            }

            RaisePropertyChanged("Settings");
            RaisePropertyChanged("ShortDescription");
            RaisePropertyChanged("Detail");

            Display(true);
        }
        public void LoadFlightReport(string debriefer, string loggerFile, bool noDisplay = false)
        {
            Trace.WriteLine("Loading " + loggerFile, "ENGINE");
            Reset();

            Report = FlightReport.Load(debriefer, loggerFile, Settings);

            if (Report.OriginalTrack.Length > 0)
            {

                if (!noDisplay)
                    Display();

                RaisePropertyChanged("Log");
                RaisePropertyChanged("Report");
                RaisePropertyChanged("Results");
                RaisePropertyChanged("Penalties");
            }
            else
            {
                Report = null;
                throw new InvalidOperationException("No valid points in track");
            }
        }
        public void Reset()
        {
            TrackPointer = null;
            foreach (var obj in Heap.Values)
                obj.Reset();
            Report = null;

            RaisePropertyChanged("Report");
        }
        public void Process(bool noDisplay = false)
        {
            if (Report.PilotId == 0)
                throw new InvalidOperationException("The pilot id can not be zero"); //should never happen. checked before

            Trace.WriteLine("Processing " + Report.ToString(), "ENGINE");

            FlightValidTrack = new Track(Report.FlightTrack);

            //reset all objects
            foreach (var obj in Heap.Values)
                obj.Reset();

            //process all objects
            foreach (var obj in Heap.Values)
                obj.Process();

            if (!noDisplay)
                Display();

            RaisePropertyChanged("Log");
            RaisePropertyChanged("Results");
            RaisePropertyChanged("Penalties");
        }
        public void BatchProcess(string debriefer, string fileName, string rootFolder)
        {
            Trace.WriteLine("Batch processing " + rootFolder, "ENGINE");

            var reportsFolder = Path.Combine(rootFolder, "Flight reports");
            if (!Directory.Exists(reportsFolder))
                Directory.CreateDirectory(reportsFolder);

            var resultsFolder = Path.Combine(rootFolder, "Results");
            if (!Directory.Exists(resultsFolder))
                Directory.CreateDirectory(resultsFolder);

            LoadFlightReport(debriefer, fileName, true);
            Process(true);

            ExportResults(resultsFolder);
            try
            {
                SavePdfReport(reportsFolder);
            }
            catch { }
            SavePdfLog(reportsFolder);
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
                    var path = new Track(VisibleTrack).ToWindowsPointArray();
                    MapViewer.AddOverlay(new TrackOverlay(path, 2) { Layer = (uint)OverlayLayers.Track });

                    var position = path[0][0];
                    if (TrackPointer != null)
                        position = TrackPointer.Position;
                    TrackPointer = new CrosshairsOverlay(position) { Layer = (uint)OverlayLayers.Pointer };
                    MapViewer.AddOverlay(TrackPointer);

                    MapViewer.AddOverlay(new WaypointOverlay(Report.TakeOffPoint.ToWindowsPoint(), "Take off") { Layer = (uint)OverlayLayers.TakeOff_And_Landing });
                    MapViewer.AddOverlay(new WaypointOverlay(Report.LandingPoint.ToWindowsPoint(), "Landing") { Layer = (uint)OverlayLayers.TakeOff_And_Landing });

                    foreach (var m in Report.Markers)
                        MapViewer.AddOverlay(new MarkerOverlay(m.ToWindowsPoint(), "Marker " + m.Name) { Layer = (uint)OverlayLayers.Markers });
                }
                if (KeepPointerCentered)
                    MapViewer.PanTo(TrackPointer.Position);
            }));
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
                    SavePdfReport(reportsFolder, true);
                    SavePdfLog(reportsFolder);
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
        public void SavePdfReport(string folder, bool shouldOpenPdf = false)
        {
            var pdfFileName = Path.Combine(folder, Report.ToShortString() + ".pdf");
            var helper = NewEmptyPdfFlightReport(pdfFileName);

            var table = helper.NewTable(null, new float[] { 1, 4 }, null);
            table.AddCell(new PdfPCell(new Paragraph("Take off and landing:", helper.Config.BoldFont)));
            var c = new PdfPCell() { PaddingBottom = 5 };
            c.AddElement(new Paragraph("Take off " + Report.TakeOffPoint.ToString(AXPointInfo.CustomReport), helper.Config.FixedWidthFont));
            if (!string.IsNullOrEmpty(Report.TakeOffPoint.Remarks))
                c.AddElement(new Paragraph(Report.TakeOffPoint.Remarks, helper.Config.FixedWidthFont));
            c.AddElement(new Paragraph("Landing " + Report.LandingPoint.ToString(AXPointInfo.CustomReport), helper.Config.FixedWidthFont));
            if (!string.IsNullOrEmpty(Report.LandingPoint.Remarks))
                c.AddElement(new Paragraph(Report.LandingPoint.Remarks, helper.Config.FixedWidthFont));
            table.AddCell(c);
            helper.Document.Add(table);


            var taskQuery = from t in Heap.Values
                            where t is ScriptingTask
                            select t as ScriptingTask;
            foreach (var task in taskQuery)
                task.ToPdfReport(helper);


            helper.Document.Add(new Paragraph("Notes", helper.Config.SubtitleFont));
            foreach (var line in Report.Notes)
                helper.Document.Add(new Paragraph(line, helper.Config.FixedWidthFont));


            helper.Document.Close();

            if (shouldOpenPdf)
                helper.OpenPdf();
        }
        public void SavePdfLog(string folder, bool shouldOpenPdf = false)
        {
            var pdfFileName = Path.Combine(folder, Report.ToShortString() + "_log.pdf");
            var helper = NewEmptyPdfFlightReport(pdfFileName);

            helper.Document.Add(new Paragraph("Measurement process log", helper.Config.SubtitleFont));
            foreach (var line in Report.Notes)
                helper.Document.Add(new Paragraph(line, helper.Config.FixedWidthFont));
            foreach (var line in GetLog(false).Select(n => n.ToLongString()))
                helper.Document.Add(new Paragraph(line, helper.Config.FixedWidthFont));

            helper.Document.Close();

            if (shouldOpenPdf)
                helper.OpenPdf();
        }

        private PdfHelper NewEmptyPdfFlightReport(string pdfFileName)
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

            var config = new PdfConfig(PdfConfig.Application.FlightAnalyzer)
            {
                HeaderLeft = Settings.Title,
                HeaderRight = Settings.Subtitle,
                FooterRight = programInfo
            };

            var helper = new PdfHelper(pdfFileName, config);

            helper.Document.Add(new Paragraph("Automatic Flight Report", config.TitleFont));

            var table = helper.NewTable(null, new float[] { 1, 1 });
            table.WidthPercentage = 50;
            table.HorizontalAlignment = Element.ALIGN_LEFT;
            table.AddCell(helper.NewLCell("Flight Date:"));
            table.AddCell(helper.NewLCell(Settings.Date.GetDateAmPm()));
            table.AddCell(helper.NewLCell("Pilot number:"));
            table.AddCell(helper.NewLCell(Report.PilotId.ToString()));
            table.AddCell(helper.NewLCell("Logger serial number:"));
            table.AddCell(helper.NewLCell(Report.LoggerSerialNumber));
            table.AddCell(helper.NewLCell("Debriefers:"));
            table.AddCell(helper.NewLCell(Report.Debriefers.Aggregate((acc, item) => acc + ", " + item)));
            helper.Document.Add(table);

            return helper;
        }

        internal IEnumerable<Note> GetLog(bool importantOnly)
        {
            return from obj in Heap.Values
                   from note in obj.Notes
                   where note.IsImportant || !importantOnly
                   orderby note.TimeStamp
                   select note;

            //var lines = new List<string>();
            //foreach (var obj in Heap.Values)
            //{
            //    foreach (var note in obj.Notes.Where(n => importantOnly ? n.IsImportant : true))
            //        lines.Add(obj.Definition.ObjectClass + " " + obj.Definition.ObjectName + ": " + note.Text);
            //}

            //return lines.OrderBy(l => l);
        }
    }
}
