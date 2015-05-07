﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarlightAI.BotNamespace
{
    class AttackPlan
    {
        public int RegionID { get; set; }
        public int RegionToAttackID { get; set; }
        public int Armies { get; set; }

        public string BuildCommand()
        {
            string command = RegionID.ToString()+" "+RegionToAttackID.ToString()+" "+Armies.ToString();
            return command;
        }
    }
}
