using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public abstract class ScriptingObject
    {
        protected Dictionary<string, ScriptingObject> heap = Singleton<Dictionary<string, ScriptingObject>>.Instance;
        protected string name;
        protected string type;
        protected string[] parameters;
        protected FlightSettings settings;

        public ScriptingObject(string name, string type, string[] parameters, FlightSettings settings)
        {
            this.name = name;
            this.type = type;
            this.parameters = parameters;
            this.settings = settings;
        }

        public abstract void Resolve(FlightReport report);

        public abstract void Display(MapViewerControl map);
    }
}
