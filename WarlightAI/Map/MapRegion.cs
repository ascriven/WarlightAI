using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarlightAI.MapNamespace
{
    class MapRegion
    {
        public int ID { get; set; }
        public int SuperRegionID { get; set; }
        public List<int> NeighborsList { get; set; }
        public bool HasWastelands { get; set; }
        public int Armies { get; set; }
        public string Team { get; set; }
        public bool IsVisible { get; set; }
        public bool WasEverVisible { get; set; }
        public int ArmiesCommittedInRegion { get; set; }

        public int ArmiesUncommittedInRegion
        {
            get
            {
                return Armies - 1 - ArmiesCommittedInRegion;
            }
        }

        public MapRegion()
        {
            NeighborsList = new List<int>();
            HasWastelands = false;
            Armies = 2;
            Team = "neutral";
            IsVisible = false;
            WasEverVisible = false;
            ArmiesCommittedInRegion = 0;
        }
    }
}
