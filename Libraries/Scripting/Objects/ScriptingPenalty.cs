using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AXToolbox.MapViewer;
using AXToolbox.Common;
using System.Windows;
using AXToolbox.GpsLoggers;
using System.Windows.Media;
using System.Threading.Tasks;

namespace AXToolbox.Scripting
{
    class ScriptingPenalty : ScriptingObject
    {
        protected ScriptingArea area;
        protected double maxSpeed = 0;
        protected double sensitivity = 0;
        protected string description = "";

        public List<Penalty> Infringements { get; protected set; }

        internal ScriptingPenalty(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        {
            Infringements = new List<Penalty>();
        }


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            if (Task == null)
                throw new ArgumentException(ObjectName + ": no previous task defined");

            //check syntax and resolve static values (well defined at constructor time, not pilot dependent)
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown penalty type '" + ObjectType + "'");

                case "BPZ":
                    //BPZ: blue PZ
                    //BPZ(<area>,<description>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    area = ResolveOrDie<ScriptingArea>(0);
                    description = ParseOrDie<string>(1, ParseString);
                    break;

                case "RPZ":
                    //BPZ: blue PZ
                    //BPZ(<area>,<description>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    area = ResolveOrDie<ScriptingArea>(0);
                    description = ParseOrDie<string>(1, ParseString);
                    break;

                case "VSMAX":
                    //VSMAX: maximum vertical speed
                    //VSMAX(<verticalSpeed>,<sensitivity>)
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    maxSpeed = ParseOrDie<double>(0, ParseDouble) * Physics.FEET2METERS / 60;
                    sensitivity = ParseOrDie<double>(1, ParseDouble);
                    break;

            }
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

                case "NONE":
                case "":
                case "DEFAULT":
                    if (DisplayParameters.Length != 1 || DisplayParameters[0] != "")
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
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown penalty type '" + ObjectType + "'");

                case "BPZ":
                    {
                        var sortedTasks = from obj in Engine.Heap.Values
                                          where obj is ScriptingTask
                                          orderby ((ScriptingTask)obj).TaskOrder
                                          select obj as ScriptingTask;

                        var firstPoint = Engine.Report.LaunchPoint;
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

                            var infringingTrack = new Track(Engine.Report.FlightTrack).Filter(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time);
                            double penaltyPoints = infringingTrack.ReducePairs((p1, p2) =>
                            {
                                return area.ScaledBPZInfringement(p2) * (p2.Time - p1.Time).TotalSeconds;
                            });
                            if (penaltyPoints > 0)
                            {
                                penaltyPoints = Math.Min(1000, 10 * Math.Ceiling(penaltyPoints / 10)); //Rule 7.5
                                var infringement = new Penalty("R7.3.6 " + description, PenaltyType.CompetitionPoints, (int)penaltyPoints);
                                infringement.InfringingTrack = infringingTrack;
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

                        var firstPoint = Engine.Report.LaunchPoint;
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

                            var infringingTrack = new Track(Engine.Report.FlightTrack).Filter(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time);
                            double penaltyPoints = infringingTrack.ReduceSegments((p1, p2) =>
                            {
                                return 1 - (p1.Altitude + p2.Altitude) / (2 * area.UpperLimit) + Physics.Distance2D(p1, p2) / area.MaxHorizontalInfringement;
                            });

                            if (penaltyPoints > 0)
                            {
                                penaltyPoints = 500 * penaltyPoints / 2; //COH7.5
                                penaltyPoints = Math.Min(1000, 10 * Math.Ceiling(penaltyPoints / 10)); //Rule 7.5
                                var infringement = new Penalty("R7.3.4 " + description, PenaltyType.CompetitionPoints, (int)penaltyPoints);
                                infringement.InfringingTrack = infringingTrack;
                                Infringements.Add(infringement);
                                task.Penalties.Add(infringement);
                            }
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

                        var firstPoint = Engine.Report.LaunchPoint;
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

                                        task.AddNote(
                                            string.Format("Max ascent/descent rate exceeded from {0} to {1}: {2:0} ft/min for {3} sec",
                                            first.ToString(AXPointInfo.Time).TrimEnd(),
                                            last.ToString(AXPointInfo.Time).TrimEnd(),
                                            Physics.VerticalVelocity(first, last) * Physics.METERS2FEET * 60,
                                            (last.Time - first.Time).TotalSeconds), true);
                            
                        }
                    }
                    break;
            }

            if (Infringements.Count == 0)
                AddNote("not infringed");
        }
        public override void Display()
        {
            if (DisplayMode != "NONE")
            {
                foreach (var inf in Infringements)
                {
                    if (inf.InfringingTrack.Count > 0)
                    {
                        var path = new Point[inf.InfringingTrack.Count];
                        Parallel.For(0, inf.InfringingTrack.Count, i =>
                        {
                            path[i] = inf.InfringingTrack[i].ToWindowsPoint();
                        });

                        switch (ObjectType)
                        {
                            case "RPZ":
                                Engine.MapViewer.AddOverlay(new TrackOverlay(path, 5) { Color = Brushes.Red, Layer = (uint)OverlayLayers.Penalties });
                                Engine.MapViewer.AddOverlay(new DistanceOverlay(path[0], path[path.Length - 1], inf.ToString()) { Layer = (uint)OverlayLayers.Penalties });
                                break;

                            case "BPZ":
                                Engine.MapViewer.AddOverlay(new TrackOverlay(path, 5) { Color = Brushes.Blue, Layer = (uint)OverlayLayers.Penalties });
                                Engine.MapViewer.AddOverlay(new DistanceOverlay(path[0], path[path.Length - 1], inf.ToString()) { Layer = (uint)OverlayLayers.Penalties });
                                break;
                        }
                    }
                }
            }
        }
    }
}
