
using System;
using System.Collections.Generic;
using AXToolbox.Common.Geodesy;
namespace AXToolbox.Common
{
    public interface ILogFile
    {
        DateTime Date { get; }
        bool Am { get; }
        int PilotId { get; }
        SignatureStatus Signature { get; }
        string LoggerSerialNumber { get; }
        string LoggerModel { get; }
        double LoggerQnh { get; }
        List<string> Notes { get; }

        List<Point> Track { get; }
        List<Waypoint> Markers { get; }
        List<Waypoint> DeclaredGoals { get; }
    }
}
