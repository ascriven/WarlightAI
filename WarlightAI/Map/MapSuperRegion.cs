using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarlightAI.MapNamespace
{
    class MapSuperRegion
    {
        public int ID { get; set; }
        public int ArmyBonus { get; set; }
        public List<int> Regions { get; set; }
        public string Team { get; set; }

        public decimal PreferenceScore { get; set; }
        public decimal ArmyRegionsRatioScore { get; set; }//deprecated because stupid
        public int WastelandCount { get; set; }
        public int RegionsRemainingUntilCapture { get; set; }
        public decimal ArmyRegionsRatio
        {
            get
            {
                return (decimal)ArmyBonus / (decimal)Regions.Count;
            }
        }

        public bool PlayerControlled { get; set; }
        public bool EnemyControlledDefinitely { get; set; }
        public bool EnemyControlledPotentially { get; set; }


        public MapSuperRegion()
        {
            Regions = new List<int>();
            WastelandCount = 0;
        }

        public bool ContainsRegionID(int regionID)
        {
            bool check = false;

            for (int i = 0; i < Regions.Count; i++)
            {
                if (Regions[i] == regionID)
                {
                    check = true;
                }
            }
            
            return check;
        }
    }
}
