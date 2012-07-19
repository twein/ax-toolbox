using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    class ScriptingPenalty : ScriptingObject
    {
        internal static ScriptingPenalty Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingPenalty(engine, definition);
        }

        protected ScriptingPenalty(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        {
            Infringements = new List<Penalty>();
        }

        protected ScriptingArea area;
        protected double maxSpeed = 0;
        protected double sensitivity = 0;
        protected string description = "";

        public List<Penalty> Infringements { get; protected set; }


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            if (Task == null)
                throw new ArgumentException(Definition.ObjectName + ": no previous task defined");

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown penalty type '" + Definition.ObjectType + "'");

                case "BPZ":
                    //BPZ: blue PZ
                    //BPZ(<area>,<description>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    area = ResolveOrDie<ScriptingArea>(0);
                    description = ParseOrDie<string>(1, Parsers.ParseString);
                    break;

                case "RPZ":
                    //BPZ: blue PZ
                    //BPZ(<area>,<description>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    area = ResolveOrDie<ScriptingArea>(0);
                    description = ParseOrDie<string>(1, Parsers.ParseString);
                    break;

                case "VSMAX":
                    //VSMAX: maximum vertical speed
                    //VSMAX(<verticalSpeed>,<sensitivity>)
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    maxSpeed = ParseOrDie<double>(0, Parsers.ParseDouble) * Physics.FEET2METERS / 60;
                    sensitivity = ParseOrDie<double>(1, Parsers.ParseDouble);
                    break;

            }
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (Definition.DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + Definition.DisplayMode + "'");

                case "NONE":
                case "":
                case "DEFAULT":
                    if (Definition.DisplayParameters.Length != 1 || Definition.DisplayParameters[0] != "")
                        throw new ArgumentException("Syntax error");
                    break;
            }
        }

        public override void Reset()
        {
            base.Reset();
            Infringements.Clear();
        }
        public override void Process()
        {
            base.Process();

            // parse and resolve pilot dependent values
            // the static values are already defined
            // syntax is already checked
            //TODO: apply globally instead of a per task basis
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown penalty type '" + Definition.ObjectType + "'");

                case "BPZ":
                    {
                        var sortedTasks = from obj in Engine.Heap.Values
                                          where obj is ScriptingTask
                                          orderby ((ScriptingTask)obj).TaskOrder
                                          select obj as ScriptingTask;

                        var firstPoint = Engine.Report.TakeOffPoint;
                        var done = false;
                        foreach (var task in sortedTasks)
                        {
                            if (done)
                                break;

                            var lastPoint = task.Result.LastUsedPoint;
                            if (lastPoint == null)
                            {
                                lastPoint = Engine.Report.LandingPoint;
                                done = true;
                            }

                            var currentTrack =
                                new Track(Engine.Report.FlightTrack)
                                .Filter(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time);
                            var penaltyPoints = area.BpzPenalty(currentTrack);
                            if (penaltyPoints > 0)
                            {
                                var infringement = new Penalty("R7.3.6 " + description, PenaltyType.CompetitionPoints, penaltyPoints);
                                infringement.InfringingTrack = area.FilterTrack(currentTrack);
                                Infringements.Add(infringement);
                                task.Penalties.Add(infringement);
                            }

                            firstPoint = lastPoint;
                        }
                    }
                    break;

                case "RPZ":
                    {
                        var sortedTasks = from obj in Engine.Heap.Values
                                          where obj is ScriptingTask
                                          orderby ((ScriptingTask)obj).TaskOrder
                                          select obj as ScriptingTask;

                        var firstPoint = Engine.Report.TakeOffPoint;
                        var done = false;
                        foreach (var task in sortedTasks)
                        {
                            if (done)
                                break;

                            var lastPoint = task.Result.LastUsedPoint;
                            if (lastPoint == null)
                            {
                                lastPoint = Engine.Report.LandingPoint;
                                done = true;
                            }

                            var currentTrack =
                                new Track(Engine.Report.FlightTrack)
                                .Filter(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time);
                            var penaltyPoints = area.RpzPenalty(currentTrack);
                            if (penaltyPoints > 0)
                            {
                                var infringement = new Penalty("R7.3.4 " + description, PenaltyType.CompetitionPoints, penaltyPoints);
                                infringement.InfringingTrack = area.FilterTrack(currentTrack);
                                Infringements.Add(infringement);
                                task.Penalties.Add(infringement);
                            }

                            firstPoint = lastPoint;
                        }
                    }
                    break;
                case "VSMAX":
                    //TODO: implement VSMAX
                    {
                        var sortedTasks = from obj in Engine.Heap.Values
                                          where obj is ScriptingTask
                                          orderby ((ScriptingTask)obj).TaskOrder
                                          select obj as ScriptingTask;

                        var firstPoint = Engine.Report.TakeOffPoint;
                        var done = false;
                        foreach (var task in sortedTasks)
                        {
                            if (done)
                                break;

                            var lastPoint = task.Result.LastUsedPoint;
                            if (lastPoint == null)
                            {
                                lastPoint = Engine.Report.LandingPoint;
                                done = true;
                            }

                            var infringingTrack = new Track(Engine.Report.FlightTrack)
                                .Filter(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time)
                                .FilterPairs((p1, p2) => Math.Abs(Physics.VerticalVelocity(p1, p2)) > maxSpeed)
                                .FilterSegments((p1, p2) => (p2.Time - p1.Time).TotalSeconds > 15);


                            foreach (var str in infringingTrack.ToStringList())
                            {
                                task.AddNote("Max ascent/descent rate exceeded " + str, true);
                                //task.AddNote(
                                //    string.Format("Max ascent/descent rate exceeded from {0} to {1}: {2:0} ft/min for {3} sec",
                                //    first.ToString(AXPointInfo.Time).TrimEnd(),
                                //    last.ToString(AXPointInfo.Time).TrimEnd(),
                                //    Physics.VerticalVelocity(first, last) * Physics.METERS2FEET * 60,
                                //    (last.Time - first.Time).TotalSeconds), true);
                            }

                            firstPoint = lastPoint;
                        }
                    }
                    break;
            }

            if (Infringements.Count == 0)
                AddNote("not infringed");
        }
        public override void Display()
        {
            if (Definition.DisplayMode != "NONE")
            {
                foreach (var inf in Infringements)
                {
                    if (inf.InfringingTrack.Length > 0)
                    {
                        var path = inf.InfringingTrack.ToWindowsPointArray();
                        var first = path[0][0];
                        var last = path[path.Length - 1][path[path.Length - 1].Length - 1];
                        switch (Definition.ObjectType)
                        {
                            case "RPZ":
                                Engine.MapViewer.AddOverlay(new TrackOverlay(path, 5) { Color = Colors.Red, Layer = (uint)OverlayLayers.Penalties });
                                Engine.MapViewer.AddOverlay(new DistanceOverlay(first, last, inf.ToString()) { Layer = (uint)OverlayLayers.Penalties });
                                break;

                            case "BPZ":
                                Engine.MapViewer.AddOverlay(new TrackOverlay(path, 5) { Color = Colors.Blue, Layer = (uint)OverlayLayers.Penalties });
                                Engine.MapViewer.AddOverlay(new DistanceOverlay(first, last, inf.ToString()) { Layer = (uint)OverlayLayers.Penalties });
                                break;

                            case "VSMAX":
                                Engine.MapViewer.AddOverlay(new TrackOverlay(path, 5) { Color = Colors.Green, Layer = (uint)OverlayLayers.Penalties });
                                Engine.MapViewer.AddOverlay(new DistanceOverlay(first, last, inf.ToString()) { Layer = (uint)OverlayLayers.Penalties });
                                break;
                        }
                    }
                }
            }
        }
    }
}
