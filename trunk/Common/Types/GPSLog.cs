using System;
using System.Collections.Generic;
using System.IO;
using Netline.BalloonLogger.SignatureLib;


namespace AXToolbox.Common
{
    public enum SignatureStatus { NotSigned, Genuine, Counterfeit }
    public class GPSLog
    {
        private string originalFilePath;
        private SignatureStatus signature;
        private string loggerSerialNumber;
        private string loggerModel;
        private int pilotNumber;
        private int pilotQnh;
        private DateTime date;
        private string datum;
        private List<GPSFix> track = new List<GPSFix>();
        private List<Marker> markers = new List<Marker>();
        private List<GoalDeclaration> goalDeclarations = new List<GoalDeclaration>();

        public string OriginalFilePath
        {
            get { return originalFilePath; }
            set { originalFilePath = value; }
        }
        public SignatureStatus Signature
        {
            get { return signature; }
            set { signature = value; }
        }
        public string LoggerSerialNumber
        {
            get { return loggerSerialNumber; }
            set { loggerSerialNumber = value; }
        }
        public string LoggerModel
        {
            get { return loggerModel; }
            set { loggerModel = value; }
        }
        public int PilotNumber
        {
            get { return pilotNumber; }
            set { pilotNumber = value; }
        }
        public int PilotQnh
        {
            get { return pilotQnh; }
            set { pilotQnh = value; }
        }
        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }
        public string Datum
        {
            get { return datum; }
            set { datum = value; }
        }
        public List<GPSFix> Track
        {
            get { return track; }
            set { track = value; }
        }
        public List<Marker> Markers
        {
            get { return markers; }
            set { markers = value; }
        }
        public List<GoalDeclaration> GoalDeclarations
        {
            get { return goalDeclarations; }
            set { goalDeclarations = value; }
        }

        public GPSLog(string filePath)
        {
            originalFilePath = filePath;
        }
    }
}

