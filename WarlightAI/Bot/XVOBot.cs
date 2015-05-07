using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarlightAI.MapNamespace;
using WarlightAI.GameStateNamespace;
using System.IO;

namespace WarlightAI.BotNamespace
{
    class XVOBot
    {
        const int CAttackAdvantageModifier = 2;
        const int CDefenseAdvantageModifier = 2;
        const bool CExceptionsOn = true;
        decimal AttackRatio = (decimal)1.80;//ratio of enemy armies/my armies on attack
        decimal DefenseRatio = (decimal).70; //ratio of enemy armies/my armies on defense

        bool debug = false;
        StreamReader logReader;

        string GameInput;
        string[] GameInputSplit;
        int InstructionCount = 0;
        string Settings;
        string SetupMap;

        List<RegionPlan> DefenseRegionsList;
        List<RegionPlan> OffensiveRegionsList;
        List<PlacementPlan> PlacementPlans;
        List<AttackPlan> AttackPlans;

        GameState myGameState=new GameState();
        Map myMap=new Map();

        
        public XVOBot()
        {
            Settings = "Settings: ";
            SetupMap = "SetupMap: ";

            myGameState = new GameState();
            myMap = new Map();

            myMap.myGameState = myGameState;
            myGameState.myMap = myMap;
        }
        public void Run()
        {
            while (true)
            {
                InstructionCount += 1;
                GameInput = GetInput();
                if (GameInput == null)
                {
                    break;//exit if game is null
                }
                GameInputSplit = GameInput.Split(' ');

                if (GameInputSplit[0] == "settings")
                {
                    Settings += GameInputSplit[1] + " " + GameInputSplit[2] + "| ";
                    myGameState.HandleSettings(GameInput);
                }
                else if (GameInputSplit[0] == "setup_map")
                {
                    SetupMap += GameInputSplit[1] + "| ";
                    myMap.HandleSetupMap(GameInput);
                }
                else if (GameInputSplit[0] == "update_map")
                {
                    myMap.HandleUpdateMap(GameInput);
                }
                else if (GameInputSplit[0] == "pick_starting_region")
                {
                    HandlePickStartingRegions();
                }
                else if (GameInputSplit[0] == "go")
                {
                    HandleGo();
                }
                else if (GameInputSplit[0] == "opponent_moves")
                {
                    HandleOpponentMoves();
                }
                else if (GameInputSplit[0] == "debug")
                {
                    SetupLogReader();
                }
                else if (GameInputSplit[0] == "Round")
                {
                    myGameState.RoundsCurrent = Convert.ToInt32(GameInputSplit[1]);
                }
                else
                {
                    if (!debug)
                    {
                        Console.WriteLine("This Bot Fails, this step: " + GameInput +
                        "; Instruction Count: " + InstructionCount.ToString() + ";"
                        + Settings + ";" + SetupMap);
                    }
                }
            }
        }
        private string GetInput()
        {
            string input = "";
            if (!debug)
            {
                input = Console.ReadLine();
            }
            else
            {
                input = logReader.ReadLine();
            }
            return input;
        }
        private void SetupLogReader()
        {
            debug = true;
            if(GameInputSplit.Length==1)
            {
                logReader = File.OpenText(@"C:\WarlightDump\Dump.txt");
            }
            else if (File.Exists(GameInputSplit[1]))
            {
                logReader = File.OpenText(GameInputSplit[1]);
            }
            else
            {
                Console.WriteLine("FAILED TO OPEN FILE");
            }
        }

        private void HandlePickStartingRegions()
        {
            int myRegionPick=-1;
            myMap.UpdateSuperRegions();
            for (int i = 0; i < myMap.MapSuperRegionsPreferenceOrder.Count; i++)
            {
                for (int iR = 0; iR < myMap.MapSuperRegionsPreferenceOrder[i].Regions.Count; iR++)
                {
                    if (IsRegionPickable(myMap.MapSuperRegionsPreferenceOrder[i].Regions[iR]))
                    {
                        myRegionPick = myMap.MapSuperRegionsPreferenceOrder[i].Regions[iR];
                        break;
                    }
                }
                if (myRegionPick != -1)
                {
                    break;
                }
            }

            Console.WriteLine(myRegionPick.ToString());
        }
        private bool IsRegionPickable(int regionID)
        {
            bool check = false;
            for (int x = 2; x < GameInputSplit.Length; x++)
            {
                if (regionID == Convert.ToInt32(GameInputSplit[x]))
                {
                    check = true;
                }
            }
            return check;
        }
        private void HandleGo()
        {
            switch (GameInputSplit[1])
            {
                case "place_armies":
                    HandlePlaceArmies();
                    break;
                case "attack/transfer":
                    HandleAttackTransfer();
                    break;
            }
        }
        private void HandlePlaceArmies()
        {
            DeviseDefensivePlans();
            DeviseOffensivePlans();
            GeneratePlacementPlans();
            GenerateAttackPlans();
            UpdateMapAfterPlacementDecided();
            Console.WriteLine(BuildPlacementCommand());
        }
        private void DeviseDefensivePlans()
        {
            CreateDefenseRegionsList();
            DetermineDefensivePlacementPlans();
        }
        private void CreateDefenseRegionsList()
        {
            DefenseRegionsList = new List<RegionPlan>();
            RegionPlan tempPlan;
            decimal armyRatio;

            foreach (MapRegion region in myMap.MapRegions)
            {
                tempPlan = new RegionPlan();
                tempPlan.myRegion = region;
                foreach (MapSuperRegion superRegion in myMap.MapSuperRegions)
                {
                    if (superRegion.ContainsRegionID(region.ID))
                    {
                        if (superRegion.PlayerControlled)
                        {
                            tempPlan.Score = 3;
                        }
                        else if (superRegion.EnemyControlledDefinitely)
                        {
                            tempPlan.Score = 2;
                        }
                        else if (superRegion.EnemyControlledPotentially&&myGameState.BotOpponentEstimatedArmiesToPlace>5)
                        {
                            tempPlan.Score = 1;
                        }
                    }
                }

                tempPlan.EnemyArmiesToDefendAgainst = 0;
                if (tempPlan.Score != 0)
                {
                    foreach (int neighborID in region.NeighborsList)
                    {
                        if (myMap.MapRegions[neighborID - 1].Team == myGameState.BotOpponent)
                        {
                            tempPlan.EnemyArmiesToDefendAgainst += myMap.MapRegions[neighborID - 1].Armies - 1;
                        }
                    }
                    armyRatio = ((decimal)tempPlan.EnemyArmiesToDefendAgainst + (decimal)myGameState.BotOpponentEstimatedArmiesToPlace) / (decimal)region.Armies;
                    if (armyRatio < DefenseRatio)
                    {
                        tempPlan.Score += 1 - armyRatio;
                        DefenseRegionsList.Add(tempPlan);
                    }
                }
            }

            DefenseRegionsList.Sort((x, y) => string.Compare(y.Score.ToString("00.0000000"), x.Score.ToString("00.0000000")));
        }
        private void DetermineDefensivePlacementPlans()
        {
            int ArmiesRemainingToPlace = myGameState.ReserveArmies;
            decimal armyRatio;
            foreach (RegionPlan plan in DefenseRegionsList)
            {
                for (int i = 1; i < ArmiesRemainingToPlace; i++)
                {
                    armyRatio = (decimal)plan.EnemyArmiesToDefendAgainst + (decimal)myGameState.BotOpponentEstimatedArmiesToPlace / (decimal)plan.myRegion.Armies;
                    if (armyRatio >= DefenseRatio)
                    {
                        ArmiesRemainingToPlace -= i;
                        plan.ArmiesToPlace = i;
                        break;
                    }
                }
            }
            myGameState.ReserveArmies = ArmiesRemainingToPlace;
        }
        private void DeviseOffensivePlans()
        {
            CreateAttackRegionsList();
            DetermineAttackPlans();
        }
        private void CreateAttackRegionsList()
        {
            OffensiveRegionsList = new List<RegionPlan>();
            RegionPlan tempPlan;
            decimal superRegionScore = myMap.MapSuperRegions.Count;

            foreach(MapSuperRegion superRegion in myMap.MapSuperRegionsPreferenceOrder)
            {
                foreach(MapRegion region in myMap.MapRegions)
                {
                    for (int i=0;i<superRegion.Regions.Count;i++)
                    {
                        if(superRegion.Regions[i]==region.ID)
                        {
                            if(region.Team!=myGameState.BotPlayer)
                            {
                                if(myMap.RegionHasNeighborWithPlayer(region.ID))
                                {
                                    tempPlan = new RegionPlan();
                                    tempPlan.myRegion = region;
                                    tempPlan.Score = superRegionScore;
                                    for (int x = 0; x < myMap.MapSuperRegionsPreferenceOrder.Count; x++)
                                    {
                                        for (int y = 0; y < region.NeighborsList.Count; y++)
                                        {
                                            if (myMap.MapSuperRegionsPreferenceOrder[x].ID == myMap.GetRegionSuperRegionsID(region.NeighborsList[y]))
                                            {
                                                tempPlan.Score += (decimal)1 / (decimal)Math.Pow(10, 1 + x);
                                            }
                                        }
                                    }
                                    if (region.Team == myGameState.BotOpponent)
                                    {
                                        tempPlan.Score = tempPlan.Score * (decimal)1.10;
                                    }
                                    OffensiveRegionsList.Add(tempPlan);
                                }
                            }
                            break;
                        }
                    }
                }
                superRegionScore += -1;
            }
            OffensiveRegionsList.Sort((x, y) => string.Compare(y.Score.ToString("00.0000000"), x.Score.ToString("00.0000000")));
        }
        private void DetermineAttackPlans()
        {
            decimal attackRatio;
            int neighborID=-1;
            int ArmiesToPlace = myGameState.ReserveArmies;

            ResetArmiesCommitted();

            foreach (RegionPlan plan in OffensiveRegionsList)
            {
                neighborID = myMap.GetPlayerOwnedNeighborRegionWithMostArmiesNonCommitted(plan.myRegion.ID);
                if (myMap.MapRegions[neighborID-1].Team==myGameState.BotPlayer)
                {
                    if(plan.myRegion.Team==myGameState.BotOpponent)
                    {
                        attackRatio = (decimal)myMap.MapRegions[neighborID - 1].ArmiesUncommittedInRegion / ((decimal)plan.myRegion.Armies + (decimal)myGameState.BotOpponentEstimatedArmiesToPlace);
                    }
                    else
                    {
                        attackRatio = (decimal)myMap.MapRegions[neighborID - 1].ArmiesUncommittedInRegion / (decimal)plan.myRegion.Armies;
                    }
                    if (attackRatio >= AttackRatio)
                    {
                        for (int i = 1; i < myMap.MapRegions[neighborID-1].ArmiesUncommittedInRegion; i++)
                        {
                            attackRatio = (decimal)i / (decimal)plan.myRegion.Armies;
                            if (attackRatio > AttackRatio)
                            {
                                plan.AttackFromRegionID=neighborID;
                                plan.ArmiesToAttackWith = i;
                                myMap.MapRegions[neighborID-1].ArmiesCommittedInRegion += i;
                                break;
                            }
                        }
                    }
                    else if(ArmiesToPlace!=0)
                    {
                        for (int i = 0; i <= ArmiesToPlace;i++)
                        {
                            if (plan.myRegion.Team == myGameState.BotOpponent)
                            {
                                attackRatio = (decimal)(myMap.MapRegions[neighborID - 1].ArmiesUncommittedInRegion + i) / ((decimal)plan.myRegion.Armies + (decimal)myGameState.BotOpponentEstimatedArmiesToPlace);
                            }
                            else
                            {
                                attackRatio = (decimal)(myMap.MapRegions[neighborID - 1].ArmiesUncommittedInRegion + i) / (decimal)plan.myRegion.Armies;
                            }
                            if (attackRatio >= AttackRatio)
                            {
                                plan.AttackFromRegionID = neighborID;
                                plan.ArmiesToPlace += i;
                                plan.ArmiesToAttackWith = myMap.MapRegions[neighborID-1].ArmiesUncommittedInRegion + i;
                                myMap.MapRegions[neighborID-1].ArmiesCommittedInRegion += myMap.MapRegions[neighborID-1].ArmiesUncommittedInRegion;
                                ArmiesToPlace -= i;
                                break;
                            }
                            if (i == ArmiesToPlace)
                            {
                                plan.AttackFromRegionID = neighborID;
                                plan.ArmiesToPlace = i;
                                myMap.MapRegions[neighborID - 1].ArmiesCommittedInRegion += myMap.MapRegions[neighborID - 1].ArmiesUncommittedInRegion;
                                ArmiesToPlace -= i;
                            }
                        }
                    }
                }
            }

            ReinforceAttackPlansWithLeftovers();
        }    
        private void ResetArmiesCommitted()
        {
            foreach (MapRegion region in myMap.MapRegions)
            {
                region.ArmiesCommittedInRegion = 0;
            }
        }
        private void ReinforceAttackPlansWithLeftovers()
        {
            bool singleAttackFromRegion;
            foreach (RegionPlan plan in OffensiveRegionsList)
            {
                if (plan.ArmiesToAttackWith != 0)
                {
                    singleAttackFromRegion = true;
                    foreach (RegionPlan comparePlan in OffensiveRegionsList)
                    {
                        if (plan.myRegion.ID != comparePlan.myRegion.ID)
                        {
                            if (plan.AttackFromRegionID == comparePlan.AttackFromRegionID)
                            {
                                singleAttackFromRegion = false;
                            }
                        }
                    }
                    if (singleAttackFromRegion)
                    {
                        plan.ArmiesToAttackWith = (myMap.MapRegions[plan.AttackFromRegionID-1].Armies - 1) + plan.ArmiesToPlace;
                    }
                }
            }
        }
        private void GeneratePlacementPlans()
        {
            PlacementPlans = new List<PlacementPlan>();
            PlacementPlan tempPlacementPlan;
            //defense placement
            for (int i = 0; i < DefenseRegionsList.Count; i++)
            {
                if (DefenseRegionsList[i].ArmiesToPlace > 0)
                {
                    tempPlacementPlan = new PlacementPlan();
                    tempPlacementPlan.RegionID = DefenseRegionsList[i].myRegion.ID;
                    tempPlacementPlan.Armies = DefenseRegionsList[i].ArmiesToPlace;
                    PlacementPlans.Add(tempPlacementPlan);
                }
            }
            //offense placement
            for (int i = 0; i < OffensiveRegionsList.Count; i++)
            {
                if (OffensiveRegionsList[i].ArmiesToPlace > 0)
                {
                    tempPlacementPlan = new PlacementPlan();
                    tempPlacementPlan.RegionID = OffensiveRegionsList[i].AttackFromRegionID;
                    tempPlacementPlan.Armies = OffensiveRegionsList[i].ArmiesToPlace;
                    PlacementPlans.Add(tempPlacementPlan);
                }
            }
            //consolidate
            //TODO
        }
        private void GenerateAttackPlans()
        {
            AttackPlans = new List<AttackPlan>();
            GenerateOffensiveAttackPlans();
            GenerateTransferAttackPlans();
        }
        private void GenerateOffensiveAttackPlans()
        {
            AttackPlan tempAttackPlan;
            //attack!
            for (int i = 0; i < OffensiveRegionsList.Count; i++)
            {
                if (OffensiveRegionsList[i].ArmiesToAttackWith > 0)
                {
                    tempAttackPlan = new AttackPlan();
                    tempAttackPlan.RegionID = OffensiveRegionsList[i].AttackFromRegionID;
                    tempAttackPlan.RegionToAttackID = OffensiveRegionsList[i].myRegion.ID;
                    tempAttackPlan.Armies = OffensiveRegionsList[i].ArmiesToAttackWith;
                    AttackPlans.Add(tempAttackPlan);
                }
            }
        }
        private void GenerateTransferAttackPlans()
        {
            AttackPlan tempAttackPlan;
            bool onAttackPlan;

            for (int i = 0; i < myMap.MapRegions.Count; i++)
            {
                if (myMap.MapRegions[i].Armies > 1)
                {
                    if (!myMap.RegionHasNeighborWithEnemyOrNeutral(myMap.MapRegions[i].ID))
                    {
                        onAttackPlan = false;
                        foreach (RegionPlan plan in OffensiveRegionsList)
                        {
                            if (plan.myRegion.ID == myMap.MapRegions[i].ID)
                            {
                                onAttackPlan = true;
                            }
                        }
                        if (!onAttackPlan)
                        {
                            int tranferID = -1;
                            int level = 0;

                            tempAttackPlan = new AttackPlan();
                            tempAttackPlan.RegionID = myMap.MapRegions[i].ID;
                            while (tranferID == -1)
                            {
                                tranferID = TransferToFront(myMap.MapRegions[i].NeighborsList, level);
                                level += 1;
                            }
                            tempAttackPlan.RegionToAttackID = tranferID;
                            tempAttackPlan.Armies = myMap.MapRegions[i].Armies - 1;
                            AttackPlans.Add(tempAttackPlan);
                        }
                    }
                }
            }
        }
        private void HandlePlaceArmiesDeprecated()
        {
            PlacementPlans = new List<PlacementPlan>();

            int regionIndex = -1;
            int remainingarmies = myGameState.ReserveArmies;
            bool allArmiesPlaced = false;
            PlacementPlan tempPlacementPlan;

            //check for region in highest priority super regions make sure neighbor is in region
            for (int i = 0; i < myMap.MapSuperRegionsPreferenceOrder.Count; i++)
            {
                for (int iR = 0; iR < myMap.MapSuperRegionsPreferenceOrder[i].Regions.Count; iR++)
                {
                    regionIndex = myMap.MapSuperRegionsPreferenceOrder[i].Regions[iR] - 1;
                    if (myMap.MapRegions[regionIndex].Team == myGameState.BotPlayer)
                    {
                        if (myMap.RegionHasNeighborWithEnemyOrNeutralAndInTargetSuperRegion(myMap.MapRegions[regionIndex].ID,myMap.MapSuperRegionsPreferenceOrder[i].ID))
                        {
                            if (!DoesRegionHaveOverwhelmingForce(myMap.MapRegions[regionIndex].ID))
                            {
                                allArmiesPlaced = true;
                                tempPlacementPlan = new PlacementPlan();
                                tempPlacementPlan.RegionID = myMap.MapRegions[regionIndex].ID;
                                tempPlacementPlan.Armies = remainingarmies;
                                PlacementPlans.Add(tempPlacementPlan);
                                break;
                            }
                        }
                    }
                }
                if (allArmiesPlaced)
                {
                    break;
                }
            }
            //same no overwhelming force check
            if (!allArmiesPlaced)
            {
                for (int i = 0; i < myMap.MapSuperRegionsPreferenceOrder.Count; i++)
                {
                    for (int iR = 0; iR < myMap.MapSuperRegionsPreferenceOrder[i].Regions.Count; iR++)
                    {
                        regionIndex = myMap.MapSuperRegionsPreferenceOrder[i].Regions[iR] - 1;
                        if (myMap.MapRegions[regionIndex].Team == myGameState.BotPlayer)
                        {
                            if (myMap.RegionHasNeighborWithEnemyOrNeutralAndInTargetSuperRegion(myMap.MapRegions[regionIndex].ID, myMap.MapSuperRegionsPreferenceOrder[i].ID))
                            {
                                allArmiesPlaced = true;
                                tempPlacementPlan = new PlacementPlan();
                                tempPlacementPlan.RegionID = myMap.MapRegions[regionIndex].ID;
                                tempPlacementPlan.Armies = remainingarmies;
                                PlacementPlans.Add(tempPlacementPlan);
                                break;
                            }
                        }
                    }
                    if (allArmiesPlaced)
                    {
                        break;
                    }
                }
            }
            if (!allArmiesPlaced)
            {
                //check for region in highest priority super regions don't check if neighbor is in region
                for (int i = 0; i < myMap.MapSuperRegionsPreferenceOrder.Count; i++)
                {
                    for (int iR = 0; iR < myMap.MapSuperRegionsPreferenceOrder[i].Regions.Count; iR++)
                    {
                        regionIndex = myMap.MapSuperRegionsPreferenceOrder[i].Regions[iR] - 1;
                        if (myMap.MapRegions[regionIndex].Team == myGameState.BotPlayer)
                        {
                            if (myMap.RegionHasNeighborWithEnemyOrNeutral(myMap.MapRegions[regionIndex].ID))
                            {
                                if (!DoesRegionHaveOverwhelmingForce(myMap.MapRegions[regionIndex].ID))
                                {
                                    allArmiesPlaced = true;
                                    tempPlacementPlan = new PlacementPlan();
                                    tempPlacementPlan.RegionID = myMap.MapRegions[regionIndex].ID;
                                    tempPlacementPlan.Armies = remainingarmies;
                                    PlacementPlans.Add(tempPlacementPlan);
                                    break;
                                }
                            }
                        }
                    }
                    if (allArmiesPlaced)
                    {
                        break;
                    }
                }
            }
            //same no overwhelming force check
            if (!allArmiesPlaced)
            {
                for (int i = 0; i < myMap.MapSuperRegionsPreferenceOrder.Count; i++)
                {
                    for (int iR = 0; iR < myMap.MapSuperRegionsPreferenceOrder[i].Regions.Count; iR++)
                    {
                        regionIndex = myMap.MapSuperRegionsPreferenceOrder[i].Regions[iR] - 1;
                        if (myMap.MapRegions[regionIndex].Team == myGameState.BotPlayer)
                        {
                            if (myMap.RegionHasNeighborWithEnemyOrNeutral(myMap.MapRegions[regionIndex].ID))
                            {
                                allArmiesPlaced = true;
                                tempPlacementPlan = new PlacementPlan();
                                tempPlacementPlan.RegionID = myMap.MapRegions[regionIndex].ID;
                                tempPlacementPlan.Armies = remainingarmies;
                                PlacementPlans.Add(tempPlacementPlan);
                                break;
                            }
                        }
                    }
                    if (allArmiesPlaced)
                    {
                        break;
                    }
                }
            }
            //check for a region with neighbors and no overwhelming force
            //for (int i = 0; i < myMap.MapRegions.Count; i++)
            //{
            //    if (myMap.MapRegions[i].Team == myGameState.BotPlayer)
            //    {
            //        if (myMap.RegionHasNeighborWithEnemyOrNeutral(myMap.MapRegions[i].ID))
            //        {
            //            if (!DoesRegionHaveOverwhelmingForce(myMap.MapRegions[i].ID))
            //            {
            //                allArmiesPlaced = true;
            //                tempPlacementPlan = new PlacementPlan();
            //                tempPlacementPlan.RegionID = myMap.MapRegions[i].ID;
            //                tempPlacementPlan.Armies = remainingarmies;
            //                PlacementPlans.Add(tempPlacementPlan);
            //                break;
            //            }
            //        }
            //    }
            //}
            //check for a region with neighbors
            //if (!allArmiesPlaced)
            //{
            //    for (int i = 0; i < myMap.MapRegions.Count; i++)
            //    {
            //        if (myMap.MapRegions[i].Team == myGameState.BotPlayer)
            //        {
            //            if (myMap.RegionHasNeighborWithEnemyOrNeutral(myMap.MapRegions[i].ID))
            //            {
            //                tempPlacementPlan = new PlacementPlan();
            //                tempPlacementPlan.RegionID = myMap.MapRegions[i].ID;
            //                tempPlacementPlan.Armies = remainingarmies;
            //                PlacementPlans.Add(tempPlacementPlan);
            //                break;
            //            }
            //        }
            //    }
            //}

            UpdateMapAfterPlacementDecided();
            Console.WriteLine(BuildPlacementCommand());
        }
        private bool DoesRegionHaveOverwhelmingForce(int regionID)
        {
            bool check = false;
            int neighborIndex=-1;
            int neighborArmies = 0;

            if (myMap.MapRegions[regionID - 1].Armies > CDefenseAdvantageModifier)
            {
                for (int i = 0; i < myMap.MapRegions[regionID - 1].NeighborsList.Count; i++)
                {
                    neighborIndex = myMap.MapRegions[regionID - 1].NeighborsList[i] - 1;
                    if (myMap.MapRegions[neighborIndex].Team != myGameState.BotPlayer)
                    {
                        neighborArmies += myMap.MapRegions[neighborIndex].Armies;
                    }
                }
            }

            if (myMap.MapRegions[regionID - 1].Armies > neighborArmies + myGameState.BotOpponentEstimatedArmiesToPlace + CDefenseAdvantageModifier)
            {
                check = true;
            }

            return check;
        }
        private void UpdateMapAfterPlacementDecided()
        {
            for (int i = 0; i < PlacementPlans.Count; i++)
            {
                for (int x = 0; x < myMap.MapRegions.Count; x++)
                {
                    if (PlacementPlans[i].RegionID == myMap.MapRegions[x].ID)
                    {
                        myMap.MapRegions[x].Armies += PlacementPlans[i].Armies;
                    }
                }
            }
        }
        private string BuildPlacementCommand()
        {
            string fullCommand = "";
            string seperator = "";
            string returnCommand = "";

            for (int i = 0; i < PlacementPlans.Count; i++)
            {
                if (fullCommand != "")
                {
                    seperator = ", ";
                }
                returnCommand = PlacementPlans[i].BuildCommand();
                if (returnCommand.Length != 0)
                {
                    fullCommand += seperator + myGameState.BotPlayer + " place_armies " + returnCommand;
                }
            }

            return fullCommand;
        }
        private void HandleAttackTransfer()
        {
            Console.WriteLine(BuildAttackTransferCommand());
        }
        private void HandleAttackTransferDeprecated()
        {
            AttackPlans = new List<AttackPlan>();

            for (int i = 0; i < myMap.MapRegions.Count; i++)
            {
                if (myMap.MapRegions[i].Team == myGameState.BotPlayer)
                {
                    if (myMap.MapRegions[i].Armies > 1)
                    {
                        DetermineNeighborToAttack(i);
                    }
                }
            }

            Console.WriteLine(BuildAttackTransferCommand());
        }
        private void DetermineNeighborToAttack(int mapIndex)
        {
            int neighborIndex = -1;
            bool superiorEnemy = false;
            bool hasEnemyNeutralNeighbors = false;
            bool hasAttacked=false;
            bool neighborTargetAlreadyAttacked;
            AttackPlan currentAttackPlan;

            
            //enemy attack logic
            for (int i = 0; i < myMap.MapRegions[mapIndex].NeighborsList.Count;i++)
            {
                neighborIndex = myMap.MapRegions[mapIndex].NeighborsList[i]-1;
                if (myMap.MapRegions[neighborIndex].Team == myGameState.BotOpponent)
                {
                    hasEnemyNeutralNeighbors = true;
                    if (myMap.MapRegions[neighborIndex].Armies < 
                        (myMap.MapRegions[mapIndex].Armies+ myGameState.BotOpponentEstimatedArmiesToPlace - CAttackAdvantageModifier))
                    {
                        neighborTargetAlreadyAttacked = CheckRegionAlreadyBeingAttacked(myMap.MapRegions[neighborIndex].ID);
                        if ((neighborTargetAlreadyAttacked && (myMap.EnemyNeighborCount(myMap.MapRegions[mapIndex].ID) + myMap.NeutralNeighborCount(myMap.MapRegions[mapIndex].ID)) == 1) ||
                            !neighborTargetAlreadyAttacked)
                        {
                            hasAttacked=true;
                            currentAttackPlan = new AttackPlan();
                            currentAttackPlan.RegionID = myMap.MapRegions[mapIndex].ID;
                            currentAttackPlan.RegionToAttackID = myMap.MapRegions[neighborIndex].ID;
                            currentAttackPlan.Armies = myMap.MapRegions[mapIndex].Armies-1;
                            AttackPlans.Add(currentAttackPlan);
                        }
                    }
                    else
                    {
                        superiorEnemy = true;
                        break;
                    }
                }
            }

            //neutral attack logic
            if (!hasAttacked && !superiorEnemy)
            {
                for (int i = 0; i < myMap.MapSuperRegionsPreferenceOrder.Count; i++)
                {
                    for (int iR = 0; iR < myMap.MapSuperRegionsPreferenceOrder[i].Regions.Count; iR++)
                    {
                        for (int x = 0; x < myMap.MapRegions[mapIndex].NeighborsList.Count; x++)
                        {
                            if (myMap.MapRegions[mapIndex].NeighborsList[x] == myMap.MapSuperRegionsPreferenceOrder[i].Regions[iR])
                            {
                                neighborIndex = myMap.MapRegions[mapIndex].NeighborsList[x] - 1;
                                if (myMap.MapRegions[neighborIndex].Team == "neutral")
                                {
                                    hasEnemyNeutralNeighbors = true;
                                    if (myMap.MapRegions[neighborIndex].Armies <
                                        (myMap.MapRegions[mapIndex].Armies - CAttackAdvantageModifier))
                                    {
                                        neighborTargetAlreadyAttacked = CheckRegionAlreadyBeingAttacked(myMap.MapRegions[neighborIndex].ID);
                                        if ((neighborTargetAlreadyAttacked && (myMap.EnemyNeighborCount(myMap.MapRegions[mapIndex].ID) + myMap.NeutralNeighborCount(myMap.MapRegions[mapIndex].ID)) == 1) ||
                                            !neighborTargetAlreadyAttacked)
                                        {
                                            hasAttacked = true;
                                            currentAttackPlan = new AttackPlan();
                                            currentAttackPlan.RegionID = myMap.MapRegions[mapIndex].ID;
                                            currentAttackPlan.RegionToAttackID = myMap.MapRegions[neighborIndex].ID;
                                            currentAttackPlan.Armies = myMap.MapRegions[mapIndex].Armies - 1;
                                            AttackPlans.Add(currentAttackPlan);
                                        }
                                    }
                                    //no longer counts neutral as superior enemy
                                    //else
                                    //{
                                    //    superiorEnemy = true;
                                    //    break;
                                    //}
                                }
                            }
                        }
                        if (hasAttacked)
                        {
                            break;
                        }
                    }
                    if (hasAttacked)
                    {
                        break;
                    }
                }
            }
            //if (!hasAttacked && !superiorEnemy)
            //{
            //    for (int i = 0; i < myMap.MapRegions[mapIndex].NeighborsList.Count; i++)
            //    {
            //        neighborIndex = myMap.MapRegions[mapIndex].NeighborsList[i] - 1;
            //        if (myMap.MapRegions[neighborIndex].Team == "neutral")
            //        {
            //            hasEnemyNeutralNeighbors = true;
            //            if (myMap.MapRegions[neighborIndex].Armies <
            //                (myMap.MapRegions[mapIndex].Armies - CAttackAdvantageModifier))
            //            {
            //                neighborTargetAlreadyAttacked = CheckRegionAlreadyBeingAttacked(myMap.MapRegions[neighborIndex].ID);
            //                if ((neighborTargetAlreadyAttacked && (myMap.EnemyNeighborCount(myMap.MapRegions[mapIndex].ID) + myMap.NeutralNeighborCount(myMap.MapRegions[mapIndex].ID)) == 1) ||
            //                    !neighborTargetAlreadyAttacked)
            //                {
            //                    hasAttacked = true;
            //                    currentAttackPlan = new AttackPlan();
            //                    currentAttackPlan.RegionID = myMap.MapRegions[mapIndex].ID;
            //                    currentAttackPlan.RegionToAttackID = myMap.MapRegions[neighborIndex].ID;
            //                    currentAttackPlan.Armies = myMap.MapRegions[mapIndex].Armies - 1;
            //                    AttackPlans.Add(currentAttackPlan);
            //                }
            //            }
            //            else
            //            {
            //                superiorEnemy = true;
            //                break;
            //            }
            //        }
            //    }
            //}

            int tranferID=-1;
            int level=0;
            //transfer logic
            if (!hasAttacked && !superiorEnemy && !hasEnemyNeutralNeighbors)
            {
                hasAttacked=true;
                currentAttackPlan = new AttackPlan();
                currentAttackPlan.RegionID = myMap.MapRegions[mapIndex].ID;
                while(tranferID==-1)
                {
                    tranferID=TransferToFront(myMap.MapRegions[mapIndex].NeighborsList,level);
                    level+=1;
                }
                currentAttackPlan.RegionToAttackID = tranferID;
                currentAttackPlan.Armies = myMap.MapRegions[mapIndex].Armies-1;
                AttackPlans.Add(currentAttackPlan);
            }
        }
        private int TransferToFront(List<int> neighborRegions, int level)//returns the neighboring region ID that is closest to moving to the front, for transfers
        {
            int selectedNeighborRegion = -1;

            for (int i = 0; i < neighborRegions.Count; i++)
            {
                if (level == 0)
                {
                    if (myMap.MapRegions[neighborRegions[i] - 1].Team != myGameState.BotPlayer)
                    {
                        selectedNeighborRegion = myMap.MapRegions[neighborRegions[i] - 1].ID;
                        break;
                    }
                }
                else
                {
                    selectedNeighborRegion = TransferToFront(myMap.MapRegions[neighborRegions[i] - 1].NeighborsList, level - 1);//recursive OMG FTW!
                    if (selectedNeighborRegion != -1)
                    {
                        selectedNeighborRegion = myMap.MapRegions[neighborRegions[i] - 1].ID;
                        break;
                    }
                }
            }

            return selectedNeighborRegion;
        }
        private string BuildAttackTransferCommand()
        {
            string fullCommand = "";
            string seperator = "";
            string returnCommand = "";
            
            for (int i = 0; i < AttackPlans.Count; i++)
            {
                if (fullCommand.Length != 0)
                {
                    seperator = ", ";
                }
                returnCommand = AttackPlans[i].BuildCommand();
                if (returnCommand.Length != 0)
                {
                    fullCommand += seperator + myGameState.BotPlayer + " attack/transfer " + returnCommand;
                }
            }
            return fullCommand;
        }
        private bool CheckRegionAlreadyBeingAttacked(int regionID)
        {
            bool check = false;

            for (int i = 0; i < AttackPlans.Count; i++)
            {
                if (AttackPlans[i].RegionToAttackID == regionID)
                {
                    check = true;
                    break;
                }
            }

            return check;
        }
        private void HandleOpponentMoves()
        {
            for (int i = 1; i < GameInputSplit.Length; i++)
            {
                myGameState.BotOpponentEstimatedArmiesToPlace = 0;
                if (GameInputSplit[i] == "player1" || GameInputSplit[i] == "player2")
                {
                    i++;
                    if (GameInputSplit[i] == "place_armies")
                    {
                        i++;
                        //region choice
                        i++;
                        myGameState.BotOpponentEstimatedArmiesToPlace += Convert.ToInt32(GameInputSplit[i]);
                    }
                    else//attack/transfer
                    {
                        i++;
                        //from
                        i++;
                        //to
                        i++;
                        //armies
                    }
                }
            }

            if (myGameState.BotOpponentEstimatedArmiesToPlace < 5)
            {
                myGameState.BotOpponentEstimatedArmiesToPlace = 5;
            }
        }
        private void ThrowException(string message)
        {
            if (CExceptionsOn)
            {
                throw new Exception(message);
            }
        }
    }
}
