using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarlightAI.GameStateNamespace;

namespace WarlightAI.MapNamespace
{
    class Map
    {
        private const decimal CWastelandWeight = 10;
        private const decimal CArmyRegionRatioWeight = 20;
        private const decimal CRemainingRegionsWeight = 4;
        private const decimal CAlreadyCapturedWeight = 15;

        public List<MapSuperRegion> MapSuperRegions { get; set; }
        public List<MapRegion> MapRegions { get; set; }
        public GameState myGameState;
        public List<MapSuperRegion> MapSuperRegionsPreferenceOrder { get; set; }

        public Map()
        {
            MapSuperRegions = new List<MapSuperRegion>();
            MapRegions=new List<MapRegion>();
        }
        //setup_map
        public void HandleSetupMap(string input)
        {
            string[] map = input.Split(' ');

            switch (map[1])
            {
                case "super_regions":
                    BuildMapSuperRegions(map);
                    break;
                case "regions":
                    BuildMapRegions(map);
                    break;
                case "neighbors":
                    BuildMapNeighbors(map);
                    break;
                case "wastelands":
                    BuildMapWastelands(map);
                    break;
                case "opponent_starting_regions":
                    BuildOpponentStartingRegions(map);
                    break;
            }
        }
        private void BuildMapSuperRegions(string[] input)
        {
            MapSuperRegion tempSuperRegion;
            for (int i = 2; i < input.Length; i+=2)
            {
                tempSuperRegion = new MapSuperRegion();
                tempSuperRegion.ID = Convert.ToInt32(input[i]);
                tempSuperRegion.ArmyBonus = Convert.ToInt32(input[i + 1]);
                MapSuperRegions.Add(tempSuperRegion);
            }
        }
        private void BuildMapRegions(string[] input)
        {
            MapRegion tempRegion;
            for (int i = 2; i < input.Length; i += 2)
            {
                tempRegion = new MapRegion();
                tempRegion.ID = Convert.ToInt32(input[i]);
                tempRegion.SuperRegionID = Convert.ToInt32(input[i + 1]);
                MapRegions.Add(tempRegion);
                MapSuperRegions[tempRegion.SuperRegionID - 1].Regions.Add(tempRegion.ID);
            }
        }
        private void RankSuperRegionByArmyRegionsRatio()//deprecated because silly
        {
            decimal score = MapSuperRegions.Count;
            List<MapSuperRegion> superRegionsList = new List<MapSuperRegion>();

            for (int i = 0; i < MapSuperRegions.Count; i++)
            {
                superRegionsList.Add(MapSuperRegions[i]);
            }

            superRegionsList.Sort((x, y) => string.Compare(x.ArmyRegionsRatio.ToString("0.0000000"), y.ArmyRegionsRatio.ToString("0.0000000")));


            for (int i = 0; i < superRegionsList.Count; i++)
            {
                superRegionsList[i].ArmyRegionsRatioScore = score - i;
            }
        }
        private void BuildMapNeighbors(string[] input)
        {
            int mainRegion;
            string[] neighborRegions;
            for (int i = 2; i < input.Length; i += 2)
            {
                mainRegion = Convert.ToInt32(input[i]);
                neighborRegions = input[i + 1].Split(',');
                for (int x = 0; x < neighborRegions.Length; x++)
                {
                    MapRegions[mainRegion - 1].NeighborsList.Add(Convert.ToInt32(neighborRegions[x]));
                    MapRegions[Convert.ToInt32(neighborRegions[x]) - 1].NeighborsList.Add(mainRegion);
                }
            }
        }
        private void BuildMapWastelands(string[] input)
        {
            int region;
            for (int i = 2; i < input.Length; i++)
            {
                region = Convert.ToInt32(input[i]);
                MapRegions[region - 1].HasWastelands = true;
                MapRegions[region - 1].Armies = 6;
                MapSuperRegions[MapRegions[region - 1].SuperRegionID - 1].WastelandCount += 1;
            }
        }
        private void BuildOpponentStartingRegions(string[] input)
        {
            for (int i = 2; i < input.Length; i++)
            {
                MapRegions[Convert.ToInt32(input[i]) - 1].Team = myGameState.BotOpponent;
            }
        }
        //update_map
        public void HandleUpdateMap(string input)
        {
            string[] map = input.Split(' ');
            int regionID=-1;

            try
            {
                SetAllPlayerRegionsEnemy();
                SetAllRegionsNotVisible();
                for (int i = 1; i < map.Length; i += 3)
                {
                    regionID = Convert.ToInt32(map[i]);
                    MapRegions[regionID - 1].IsVisible = true;
                    MapRegions[regionID - 1].WasEverVisible = true;
                    MapRegions[regionID - 1].Team = map[i + 1];
                    MapRegions[regionID - 1].Armies = Convert.ToInt32(map[i + 2]);
                }
                UpdateSuperRegions();
            }
            catch (Exception e)
            {
                Exception x=new Exception("RegionID: " + regionID.ToString()+" RegionsCount: "+MapRegions.Count.ToString(),e);
                throw x;
            }
        }
        private void SetAllPlayerRegionsEnemy()//this is because if you lose a province you can't see the game does not tell you your opponent has taken it, this way bot will not try to place units
        {
            for (int i = 0; i < MapRegions.Count; i++)
            {
                if (MapRegions[i].Team == myGameState.BotPlayer)
                {
                    MapRegions[i].Team = myGameState.BotOpponent;
                }
            }
        }
        private void SetAllRegionsNotVisible()
        {
            for (int i = 0; i < MapRegions.Count; i++)
            {
                MapRegions[i].IsVisible = false;
            }
        }
        public void UpdateSuperRegions()
        {
            CheckSuperRegionsControlled();
            RankSuperRegions();
        }
        private void CheckSuperRegionsControlled()
        {
            bool AllRegionsVisible;
            bool AllVisibleRegionsEnemyOwned;
            bool AllRegionsPlayerOwned;
            for (int i = 0; i < MapSuperRegions.Count; i++)
            {
                AllRegionsVisible = true;
                AllVisibleRegionsEnemyOwned = true;
                AllRegionsPlayerOwned = true;
                for (int iR = 0; iR < MapSuperRegions[i].Regions.Count; iR++)
                {
                    if (MapRegions[MapSuperRegions[i].Regions[iR] - 1].Team != myGameState.BotPlayer)
                    {
                        AllRegionsPlayerOwned = false;
                    }
                    if (!MapRegions[MapSuperRegions[i].Regions[iR] - 1].IsVisible)
                    {
                        AllRegionsVisible = false;
                    }
                    else
                    {
                        if (MapRegions[MapSuperRegions[i].Regions[iR] - 1].Team != myGameState.BotOpponent)
                        {
                            AllVisibleRegionsEnemyOwned = false;
                        }
                    }
                }

                MapSuperRegions[i].EnemyControlledDefinitely = false;
                if (AllRegionsVisible && AllVisibleRegionsEnemyOwned)
                {
                    MapSuperRegions[i].EnemyControlledDefinitely = true;
                }

                MapSuperRegions[i].EnemyControlledPotentially = false;
                if (!AllRegionsVisible && AllVisibleRegionsEnemyOwned)
                {
                    MapSuperRegions[i].EnemyControlledPotentially = true;
                }

                MapSuperRegions[i].PlayerControlled = false;
                if (AllRegionsPlayerOwned)
                {
                    MapSuperRegions[i].PlayerControlled = true;
                }
            }
        }
        private void RankSuperRegions()
        {
            CalculateRegionsRemainingUntilCapture();
            MapSuperRegionsPreferenceOrder = new List<MapSuperRegion>();

            for (int i = 0; i < MapSuperRegions.Count; i++)
            {
                MapSuperRegions[i].PreferenceScore = 0;
                MapSuperRegions[i].PreferenceScore += (decimal)MapSuperRegions[i].WastelandCount * (-CWastelandWeight);
                MapSuperRegions[i].PreferenceScore += MapSuperRegions[i].ArmyRegionsRatio * CArmyRegionRatioWeight;
                MapSuperRegions[i].PreferenceScore += (10 - MapSuperRegions[i].RegionsRemainingUntilCapture) * CRemainingRegionsWeight;
                //if (MapSuperRegions[i].RegionsRemainingUntilCapture == 0)
                //{
                //    MapSuperRegions[i].PreferenceScore += -CAlreadyCapturedWeight;
                //}
                MapSuperRegionsPreferenceOrder.Add(MapSuperRegions[i]);
            }

            MapSuperRegionsPreferenceOrder.Sort((x,y)=>string.Compare(y.PreferenceScore.ToString("000"),x.PreferenceScore.ToString("000")));
        }
        private void CalculateRegionsRemainingUntilCapture()
        {
            int ownedCount = 0;
            for (int i = 0; i < MapSuperRegions.Count; i++)
            {
                ownedCount = 0;
                for (int iR = 0; iR < MapSuperRegions[i].Regions.Count; iR++)
                {
                    if(MapRegions[MapSuperRegions[i].Regions[iR]-1].Team==myGameState.BotPlayer)
                    {
                        ownedCount += 1;
                    }
                }
                MapSuperRegions[i].RegionsRemainingUntilCapture = MapSuperRegions[i].Regions.Count - ownedCount;
            }
        }
        //helper
        public bool RegionHasNeighborWithEnemyOrNeutralAndInTargetSuperRegion(int regionID, int superRegionTargetID)
        {
            bool check = false;
            int neighborIndex = -1;

            for (int i = 0; i < MapRegions[regionID - 1].NeighborsList.Count; i++)
            {
                neighborIndex = MapRegions[regionID - 1].NeighborsList[i] - 1;
                if (MapRegions[neighborIndex].Team != myGameState.BotPlayer&&MapSuperRegions[superRegionTargetID-1].ContainsRegionID(neighborIndex+1))
                {
                    check = true;
                    break;
                }
            }

            return check;
        }
        public bool RegionHasNeighborWithPlayer(int regionID)
        {
            bool check = false;
            int neighborIndex = -1;

            for (int i = 0; i < MapRegions[regionID - 1].NeighborsList.Count; i++)
            {
                neighborIndex = MapRegions[regionID - 1].NeighborsList[i] - 1;
                if (MapRegions[neighborIndex].Team == myGameState.BotPlayer)
                {
                    check = true;
                    break;
                }
            }

            return check;
        }
        public int GetPlayerOwnedNeighborRegionWithMostArmiesNonCommitted(int regionID)
        {
            int id = -1;
            int neighborIndex = -1;
            int mostArmies = -1;

            for (int i = 0; i < MapRegions[regionID - 1].NeighborsList.Count; i++)
            {
                neighborIndex = MapRegions[regionID - 1].NeighborsList[i] - 1;
                if (MapRegions[neighborIndex].Team == myGameState.BotPlayer)
                {
                    if (MapRegions[neighborIndex].Armies - MapRegions[neighborIndex].ArmiesCommittedInRegion > mostArmies)
                    {
                        id = MapRegions[regionID - 1].NeighborsList[i];
                        mostArmies = MapRegions[neighborIndex].Armies - MapRegions[neighborIndex].ArmiesCommittedInRegion;
                    }
                }
            }

            return id;
        }
        public bool RegionHasNeighborWithEnemyOrNeutral(int regionID)
        {
            bool check = false;
            int neighborIndex=-1;

            for (int i = 0; i < MapRegions[regionID - 1].NeighborsList.Count; i++)
            {
                neighborIndex=MapRegions[regionID - 1].NeighborsList[i]-1;
                if (MapRegions[neighborIndex].Team != myGameState.BotPlayer)
                {
                    check = true;
                    break;
                }
            }

            return check;
        }
        public int EnemyNeighborCount(int regionID)
        {
            int count = 0;
            int neighborIndex = -1;

            for (int i = 0; i < MapRegions[regionID - 1].NeighborsList.Count; i++)
            {
                neighborIndex = MapRegions[regionID - 1].NeighborsList[i] - 1;
                if (MapRegions[neighborIndex].Team == myGameState.BotOpponent)
                {
                    count += 1;
                }
            }
            return count;
        }
        public int NeutralNeighborCount(int regionID)
        {
            int count = 0;
            int neighborIndex = -1;

            for (int i = 0; i < MapRegions[regionID - 1].NeighborsList.Count; i++)
            {
                neighborIndex = MapRegions[regionID - 1].NeighborsList[i] - 1;
                if (MapRegions[neighborIndex].Team == "neutral")
                {
                    count += 1;
                }
            }
            return count;
        }
        public int GetRegionSuperRegionsID(int regionID)
        {
            int superRegionsID=-1;

            for (int i = 0; i < MapSuperRegions.Count; i++)
            {
                for (int x = 0; x < MapSuperRegions[i].Regions.Count; x++)
                {
                    if (regionID == MapSuperRegions[i].Regions[x])
                    {
                        superRegionsID = MapSuperRegions[i].ID;
                    }
                }
            }

            return superRegionsID;
        }
    }
}
