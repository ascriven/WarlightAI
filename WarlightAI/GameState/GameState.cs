using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarlightAI.MapNamespace;

namespace WarlightAI.GameStateNamespace
{
    class GameState
    {
        public int TimeBank { get; set; }
        public int TimePerMove { get; set; }
        public int RoundsCurrent { get; set; }
        public int RoundsMax { get; set; }
        public List<int> StartingRegions { get; set; }
        public string BotPlayer { get; set; }
        public string BotOpponent { get; set; }
        public int ReserveArmies { get; set; }
        public Map myMap;
        public int BotOpponentEstimatedArmiesToPlace { get; set; }

        public GameState()
        {
            StartingRegions = new List<int>();
        }
        public void HandleSettings(string input)
        {
            string[] setting = input.Split(' ');

            switch (setting[1])
            {
                case "timebank":
                    TimeBank = Convert.ToInt32(setting[2]);
                    break;
                case "time_per_move":
                    TimePerMove = Convert.ToInt32(setting[2]);
                    break;
                case "max_rounds":
                    RoundsMax = Convert.ToInt32(setting[2]);
                    break;
                case "your_bot":
                    BotPlayer = setting[2];
                    break;
                case "opponent_bot":
                    BotOpponent = setting[2];
                    break;
                case "starting_regions":
                    BuildStartingRegions(setting);
                    break;
                case "starting_armies":
                    ReserveArmies = Convert.ToInt32(setting[2]);
                    break;
            }
        }
        private void BuildStartingRegions(string[] input)
        {
            for (int i = 2; i < input.Length; i++)
            {
                StartingRegions.Add(Convert.ToInt32(input[i]));
            }
        }
    }
}
