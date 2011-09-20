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

                            double penaltyPoints = 0;
                            AXPoint last = null;
                            var pointsInside = new List<AXPoint>();
                            foreach (var p in Engine.Report.FlightTrack.Where(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time))
                            {
                                if (area.Contains(p))
                                {
                                    pointsInside.Add(p);
                                    if (last != null)
                                    {
                                        var deltaT = (p.Time - last.Time).TotalSeconds;
                                        penaltyPoints += area.ScaledBPZInfringement(p) * deltaT;
                                    }
                                    last = p;
                                }
                            }
                            if (penaltyPoints > 0)
                            {
                                penaltyPoints = Math.Min(1000, 10 * Math.Ceiling(penaltyPoints / 10)); //Rule 7.5
                                var infringement = new Penalty("R7.3.6 " + description, PenaltyType.CompetitionPoints, (int)penaltyPoints);
                                infringement.UsedPoints.AddRange(pointsInside);
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

                            double penalty = 0;
                            AXPoint first = null;
                            AXPoint last = null;
                            var pointsInside = new List<AXPoint>();
                            //TODO: replace by .First(p=>area.Contains(p)) and .Last(...)
                            foreach (var p in Engine.Report.FlightTrack.Where(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time))
                            {
                                if (area.Contains(p))
                                {
                                    pointsInside.Add(p);
                                    if (first == null)
                                        first = p;
                                    last = p;
                                }
                            }
                            if (first != null)
                            {
                                var vertInfringement = 1 - (first.Altitude + last.Altitude) / (2 * area.UpperLimit);
                                var horzInfringement = Physics.Distance2D(first, last) / area.MaxHorizontalInfringement;
                                penalty = 500 * (vertInfringement + horzInfringement) / 2; //COH7.5

                                penalty = 10 * Math.Ceiling((penalty / 10));
                                var infringement = new Penalty("R7.3.4 " + description, PenaltyType.CompetitionPoints, (int)penalty);
                                infringement.UsedPoints.AddRange(pointsInside);
                                Infringements.Add(infringement);
                                task.Penalties.Add(infringement);
                            }

                            firstPoint = lastPoint;
                        }
                        /*
                         * new 2011 draft
                        double penalty = 0;
                        AXPoint last = null;
                        foreach (var p in Engine.Report.FlightTrack)
                        {
                            if (area.Contains(p))
                            {
                                if (last != null && !p.StartSubtrack)
                                {
                                    var deltaT = (p.Time - last.Time).TotalSeconds;
                                    penalty += area.ScaledRPZInfringement(p) * deltaT;
                                }
                                last = p;
                            }
                        }
                        penalty = Math.Min(1000, 10 * Math.Ceiling(penalty / 10)); //Rule 7.5
                        Penalty = new Penalty("R7.3.4 RPZ", PenaltyType.CompetitionPoints, (int)penalty);
                        */
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

                            AXPoint first = null;
                            AXPoint last = null;
                            foreach (var p in Engine.Report.FlightTrack.Where(p => p.Time >= firstPoint.Time && p.Time <= lastPoint.Time))
                            {
                                if (last == null)
                                {
                                    //do nothing
                                }
                                else if (Math.Abs(Physics.VerticalVelocity(last, p)) > maxSpeed)
                                {
                                    if (first == null)
                                        first = p;
                                }
                                else if (first != null)
                                {
                                    if ((last.Time - first.Time).TotalSeconds >= 15)
                                    {
                                        task.AddNote(
                                            string.Format("Max ascent/descent rate exceeded from {0} to {1}: {2:0} ft/min for {3} sec",
                                            first.ToString(AXPointInfo.Time).TrimEnd(),
                                            last.ToString(AXPointInfo.Time).TrimEnd(),
                                            Physics.VerticalVelocity(first, last) * Physics.METERS2FEET * 60,
                                            (last.Time - first.Time).TotalSeconds), true);
                                    }
                                    first = null;
                                }

                                last = p;
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
            if (DisplayMode != "NONE")
            {
                foreach (var inf in Infringements)
                {
                    if (inf.UsedPoints.Count > 0)
                    {
                        var path = new Point[inf.UsedPoints.Count];
                        Parallel.For(0, inf.UsedPoints.Count, i =>
                        {
                            path[i] = inf.UsedPoints[i].ToWindowsPoint();
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
