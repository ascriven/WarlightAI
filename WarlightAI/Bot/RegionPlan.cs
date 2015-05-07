using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarlightAI.MapNamespace;

namespace WarlightAI.BotNamespace
{
    class RegionPlan
    {
        public MapRegion myRegion { get; set; }
        public decimal Score { get; set; }
        public int ArmiesToPlace { get; set; }
        public int ArmiesToAttackWith { get; set; }
        public int AttackFromRegionID { get; set; }
        public int EnemyArmiesToDefendAgainst { get; set; }

        public RegionPlan()
        {
            Score = 0;
            ArmiesToPlace = 0;
            ArmiesToAttackWith = 0;
            AttackFromRegionID = -1;
        }
    }
}
