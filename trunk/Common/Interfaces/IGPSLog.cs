using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AXToolbox.Common
{
    public enum SignatureStatus { NotSigned, Genuine, Counterfeit }

    public interface IGPSLog
    {
        SignatureStatus Signature { get; }
        string LoggerSerialNumber { get; }
        string LoggerModel { get; }
        int PilotId { get; }
        int PilotQnh { get; }
        DateTime Date { get; }
        //TODO: Remove Datum, it's always WGS84
        string Datum { get; }
        List<GPSFix> Track { get; }
        List<LoggerMarker> Markers { get; }
        List<LoggerGoalDeclaration> GoalDeclarations { get; }
    }
}
