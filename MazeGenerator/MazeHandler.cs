using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Base;

namespace MazeGeneratorMod
{
    internal class MazeHandler
    {
        public static int randomSeed = 0;
        public static bool seedIsRandom = true;
        public static System.Random random = new System.Random();

        public static Int3 mazeSize = new Int3(10, 1, 10);
        public static Vector3 basePosition = new Vector3(-800, -40, 250);
        public static Quaternion baseRotation = Quaternion.identity;

        private static readonly List<MazeCell> mazeCells = new List<MazeCell>();
        private static readonly Dictionary<Int3, MazeCell> mazeCellsByPos = new Dictionary<Int3, MazeCell>();
        private static GameObject mazeBeaconGameObject;
        public static Vector3 finishPosition;

        public static Queue<QueuedBasePlacement> queuedBasePlacements = new Queue<QueuedBasePlacement>();
        //private static float lastBasePlacementTime = Time.time;
        //private static float basePlacementDelay = 0.05f;

        public static Queue<Int3> queuedBaseDestroys = new Queue<Int3>();
        //private static float lastDestroyBaseTime = Time.time;
        //private static float destroyBaseDelay = 0.05f;

        public static Queue<Vector3> queuedCreaturePlacements = new Queue<Vector3>();
        //private static float lastCreaturePlacementTime = Time.time;
        //private static float creaturePlacementDelay = 0.05f;

        private static List<GameObject> posters = new List<GameObject>();

        private static int posterPercentChance = 25;
        private static float posterHorizontalOffset = 2.4614f;
        private static float posterVerticalOffset = 0.5f;

        public class PosterPlacement
        {
            public Vector3 position;
            public Quaternion rotation;

            public PosterPlacement(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }
        }
        public static Queue<PosterPlacement> queuedPosterPlacements = new Queue<PosterPlacement>();

        private static bool rebuildOnNextFrame = false;

        private class MazeCellWall
        {
            public Int3 offset;
            public bool solid = true;

            public MazeCellWall(Int3 offset)
            {
                this.offset = offset;
            }
        }

        private class MazeCell
        {
            public Int3 offset;
            public MazeCellWall
                leftWall,
                rightWall,
                frontWall,
                backWall
            ;
            public bool processed;

            public MazeCell(Int3 offset)
            {
                this.offset = offset;
                leftWall = new MazeCellWall(offset);
                rightWall = new MazeCellWall(offset);
                frontWall = new MazeCellWall(offset);
                backWall = new MazeCellWall(offset);
                processed = false;
            }
        }

        public static void GenerateMaze()
        {
            /*
             * https://en.wikipedia.org/wiki/Maze_generation_algorithm#Iterative_randomized_Prim's_algorithm_(without_stack,_without_sets)
             * 
             * 1. Start with a grid full of walls.
             * 2. Pick a cell, mark it as part of the maze. Add the walls of the cell to the wall list.
             * 3. While there are walls in the list:
             *     1. Pick a random wall from the list. If only one of the cells that the wall divides is visited, then:
             *         1. Make the wall a passage and mark the unvisited cell as part of the maze.
             *         2. Add the neighboring walls of the cell to the wall list.
             *     2. Remove the wall from the list.
             */

            if (seedIsRandom)
            {
                seedIsRandom = false;
                random = new System.Random();
                randomSeed = random.Next(int.MaxValue);
                PictureFrameHandler.UpdateUI();
            }

            random = new System.Random(randomSeed);

            mazeCells.Clear();
            mazeCellsByPos.Clear();

            for (int x = 0; x < mazeSize.x; x++)
            {
                for (int y = 0; y < mazeSize.y; y++)
                {
                    for (int z = 0; z < mazeSize.z; z++)
                    {
                        MazeCell newMazeCell = new MazeCell(
                            new Int3(x, y, z)
                        );

                        mazeCells.Add(newMazeCell);
                        mazeCellsByPos[new Int3(x, y, z)] = newMazeCell;
                    }
                }
            }

            //PrintMaze();

            List<MazeCellWall> remainingWalls = new List<MazeCellWall>();

            MazeCell firstCell = mazeCells.GetRandom(random);
            firstCell.processed = true;

            remainingWalls.Add(firstCell.leftWall);
            remainingWalls.Add(firstCell.rightWall);
            remainingWalls.Add(firstCell.frontWall);
            remainingWalls.Add(firstCell.backWall);

            while (remainingWalls.Count > 0)
            {
                MazeCellWall curMazeCellWall = remainingWalls.GetRandom(random);
                MazeCell curMazeCell = mazeCellsByPos[curMazeCellWall.offset];
                MazeCell otherMazeCell;

                bool
                    isLeftWall = false,
                    isRightWall = false,
                    isFrontWall = false,
                    isBackWall = false
                ;

                if (curMazeCellWall == curMazeCell.leftWall)
                {
                    mazeCellsByPos.TryGetValue(curMazeCellWall.offset + new Int3(-1, 0, 0), out otherMazeCell);
                    isLeftWall = true;
                }
                else if (curMazeCellWall == curMazeCell.rightWall)
                {
                    mazeCellsByPos.TryGetValue(curMazeCellWall.offset + new Int3(1, 0, 0), out otherMazeCell);
                    isRightWall = true;
                }
                else if (curMazeCellWall == curMazeCell.frontWall)
                {
                    mazeCellsByPos.TryGetValue(curMazeCellWall.offset + new Int3(0, 0, -1), out otherMazeCell);
                    isFrontWall = true;
                }
                else if (curMazeCellWall == curMazeCell.backWall)
                {
                    mazeCellsByPos.TryGetValue(curMazeCellWall.offset + new Int3(0, 0, 1), out otherMazeCell);
                    isBackWall = true;
                }
                else
                {
                    Plugin.Logger.LogInfo($"-- GenerateMaze - oops, wall not found! --");
                    remainingWalls.Remove(curMazeCellWall);
                    continue;
                }

                if (otherMazeCell != null)
                {
                    // Check if only one of the two cells divided by this wall is visited
                    if (
                        (otherMazeCell.processed && !curMazeCell.processed)
                        || (!otherMazeCell.processed && curMazeCell.processed)
                    )
                    {
                        curMazeCellWall.solid = false;

                        // Mark the other maze cell's opposing wall as not solid as well
                        if (isLeftWall)
                            otherMazeCell.rightWall.solid = false;
                        else if (isRightWall)
                            otherMazeCell.leftWall.solid = false;
                        else if (isFrontWall)
                            otherMazeCell.backWall.solid = false;
                        else if (isBackWall)
                            otherMazeCell.frontWall.solid = false;

                        if (!curMazeCell.processed)
                        {
                            curMazeCell.processed = true;

                            // Add the current maze cell's wall to the list of walls to be processed
                            //  if it wasn't just marked as not solid
                            if (!isLeftWall)
                                remainingWalls.Add(curMazeCell.leftWall);

                            if (!isRightWall)
                                remainingWalls.Add(curMazeCell.rightWall);

                            if (!isFrontWall)
                                remainingWalls.Add(curMazeCell.frontWall);

                            if (!isBackWall)
                                remainingWalls.Add(curMazeCell.backWall);
                        }
                        else
                        {
                            otherMazeCell.processed = true;

                            // Add the other maze cell's opposing wall to the list of walls to be processed
                            //  if it wasn't just marked as not solid
                            if (!isRightWall)
                                remainingWalls.Add(otherMazeCell.leftWall);

                            if (!isLeftWall)
                                remainingWalls.Add(otherMazeCell.rightWall);

                            if (!isBackWall)
                                remainingWalls.Add(otherMazeCell.frontWall);

                            if (!isFrontWall)
                                remainingWalls.Add(otherMazeCell.backWall);
                        }
                    }
                }

                remainingWalls.Remove(curMazeCellWall);

                //PrintMaze();
            }

            PrintMaze();
        }

        private static void PrintMaze()
        {
            Plugin.Logger.LogInfo($"-- Generated Maze --");
            Plugin.Logger.LogInfo($"-- Size: {mazeSize.x},{mazeSize.z} --");
            Plugin.Logger.LogInfo($"-- Seed: {randomSeed} --");
            string curString = "";
            for (int z = mazeSize.z - 1; z >= 0; z--)
            {
                for (int y = 0; y < mazeSize.y; y++)
                {
                    for (int x = 0; x < mazeSize.x; x++)
                    {
                        Int3 offset = new Int3(x, y, z);
                        MazeCell curCell = mazeCellsByPos[offset];

                        if (!curCell.processed)
                        {
                            curString += " ";
                            continue;
                        }

                        if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "O";
                        }
                        else if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "╵";
                        }
                        else if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "╷";
                        }
                        else if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "│";
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "╶";
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "╰";
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "╭";
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "├";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "╴";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "╯";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "╮";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "┤";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "─";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "┴";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            curString += "┬";
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            curString += "┼";
                        }
                    }
                }

                Plugin.Logger.LogInfo($"-- {curString} --");
                curString = "";
            }
        }

        public static void SpawnMazeStart()
        {
            // Add this base first,
            //  then calculate the offsets for all base pieces relative to it,
            //  then convert those to world positions and place them in a list,
            //  then pass in those positions to each function call

            Mod.PlaceBasePrefab(
                Mod.cachedPrefabs[TechType.BaseRoom],
                basePosition,
                0
            );

            Int3 startHatchCell = new Int3(0, 0, 0);
            Vector3 startHatchPosition = Mod.mazeBase.GridToWorld(startHatchCell);

            // This prevents the hatch from appearing on the diagonal
            Int3 hatchOffset = new Int3(1, 0, 0);

            // This gets the re-calculated grid position of the start and end hatches
            Int3 startHatchCellAfter = Mod.mazeBase.WorldToGrid(startHatchPosition) + hatchOffset;

            // Spawn start room hatch
            Mod.PlaceBasePrefab(
                Mod.cachedPrefabs[TechType.BaseHatch],
                startHatchPosition,
                0,
                new Face(startHatchCellAfter, Direction.South)
            );

            Vector3 playerPosition = Player.main.transform.position;
            Bounds startBounds = Mod.GetStartBounds();

            if (startBounds.Contains(playerPosition))
            {
                Vector3 max = Mod.mazeBase.GridToWorld(Mod.mazeBase.Bounds.maxs + new Int3(0, 1, 0));

                Vector3 newPosition = new Vector3(
                        playerPosition.x,
                        max.y + 2f,
                        playerPosition.z
                    );

                Player.main.SetPosition(newPosition);
            }
        }

        public static void SpawnMazeBasePieces()
        {
            /*
             * Default rotations:
             * BaseCorridorI: │
             * BaseCorridorL: ╰
             * BaseCorridorT: ┴
             * BaseHatch: ?
             */

            List<Vector3> positions = new List<Vector3>();

            Dictionary<Int3, int> solidWallCounts = new Dictionary<Int3, int>();

            Int3 offsetAdjustment = new Int3((-mazeSize.x / 2) + 2, 0, 3) - Mod.mazeBase.cellOffset;

            for (int z = 0; z < mazeSize.z; z++)
            {
                for (int y = 0; y < mazeSize.y; y++)
                {
                    for (int x = 0; x < mazeSize.x; x++)
                    {
                        Int3 offset = new Int3(x, y, z);
                        Vector3 position = Mod.mazeBase.GridToWorld(offset + offsetAdjustment);
                        positions.Add(position);
                    }
                }
            }

            // Set the position for the finish
            finishPosition = Mod.mazeBase.GridToWorld(new Int3(0, 0, mazeSize.z + 3) - Mod.mazeBase.cellOffset);

            int posInc = 0;

            for (int z = 0; z < mazeSize.z; z++)
            {
                for (int y = 0; y < mazeSize.y; y++)
                {
                    for (int x = 0; x < mazeSize.x; x++)
                    {
                        Int3 offset = new Int3(x, y, z);
                        MazeCell curCell = mazeCellsByPos[offset];

                        if (z == 0 && x == mazeSize.x / 2 - 1)
                        {
                            curCell.frontWall.solid = false;
                        }
                        else if (z == mazeSize.z - 1 && x == mazeSize.x / 2 - 1)
                        {
                            curCell.backWall.solid = false;
                        }

                        int solidWallCount = 0;
                        solidWallCount += curCell.leftWall.solid ? 1 : 0;
                        solidWallCount += curCell.rightWall.solid ? 1 : 0;
                        solidWallCount += curCell.frontWall.solid ? 1 : 0;
                        solidWallCount += curCell.backWall.solid ? 1 : 0;

                        solidWallCounts[curCell.offset] = solidWallCount;
                    }
                }
            }


            for (int z = 0; z < mazeSize.z; z++)
            {
                for (int y = 0; y < mazeSize.y; y++)
                {
                    for (int x = 0; x < mazeSize.x; x++)
                    {
                        Int3 offset = new Int3(x, y, z);
                        MazeCell curCell = mazeCellsByPos[offset];

                        // Open up a wall for the starting BaseRoom
                        if (z == 0 && x == mazeSize.x / 2 - 1)
                        {
                            curCell.frontWall.solid = false;
                        }
                        else if (z == mazeSize.z - 1 && x == mazeSize.x / 2 - 1)
                        {
                            curCell.backWall.solid = false;
                        }

                        if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            // nothing
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: (SOLID)");
                        }
                        else if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "╵";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: End");
                        }
                        else if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            //curString += "╷";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: End");
                        }
                        else if (
                            curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "│";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: I 0");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorI],
                                positions[posInc],
                                0
                            ));
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            //curString += "╶";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: End");
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "╰";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: L 0");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorL],
                                positions[posInc],
                                0
                            ));
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            //curString += "╭";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: L 90");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorL],
                                positions[posInc],
                                90
                            ));
                        }
                        else if (
                            curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "├";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: T 90");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorT],
                                positions[posInc],
                                90
                            ));
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            //curString += "╴";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: End");
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "╯";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: L 270");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorL],
                                positions[posInc],
                                270
                            ));
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            //curString += "╮";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: L 180");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorL],
                                positions[posInc],
                                180
                            ));
                        }
                        else if (
                            !curCell.leftWall.solid
                            && curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "┤";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: T 270");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorT],
                                positions[posInc],
                                270
                            ));
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            //curString += "─";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: I 90");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorI],
                                positions[posInc],
                                90
                            ));
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "┴";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: T 0");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorT],
                                positions[posInc],
                                0
                            ));
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && curCell.backWall.solid
                        )
                        {
                            //curString += "┬";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: T 180");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorT],
                                positions[posInc],
                                180
                            ));
                        }
                        else if (
                            !curCell.leftWall.solid
                            && !curCell.rightWall.solid
                            && !curCell.frontWall.solid
                            && !curCell.backWall.solid
                        )
                        {
                            //curString += "┼";
                            //MazeGeneratorPlugin.Logger.LogInfo($"{offset}: X 0");
                            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                                Mod.cachedPrefabs[TechType.BaseCorridorX],
                                positions[posInc],
                                0
                            ));
                        }

                        Int3 leftCell = curCell.offset + new Int3(-1, 0, 0);
                        Int3 rightCell = curCell.offset + new Int3(1, 0, 0);
                        Int3 frontCell = curCell.offset + new Int3(0, 0, -1);
                        Int3 backCell = curCell.offset + new Int3(0, 0, 1);

                        if (CreatureHandler.creaturesEnabled)
                        {
                            if (solidWallCounts[curCell.offset] == 2)
                            {
                                if (
                                    (!curCell.leftWall.solid && solidWallCounts.ContainsKey(leftCell) && solidWallCounts[leftCell] == 3)
                                    || (!curCell.rightWall.solid && solidWallCounts.ContainsKey(rightCell) && solidWallCounts[rightCell] == 3)
                                    || (!curCell.frontWall.solid && solidWallCounts.ContainsKey(frontCell) && solidWallCounts[frontCell] == 3)
                                    || (!curCell.backWall.solid && solidWallCounts.ContainsKey(backCell) && solidWallCounts[backCell] == 3)
                                )
                                {
                                    queuedCreaturePlacements.Enqueue(positions[posInc]);
                                }
                            }
                        }

                        if (random.Next(0, 100) < posterPercentChance)
                        {
                            if (!curCell.leftWall.solid && solidWallCounts.ContainsKey(leftCell) && solidWallCounts[leftCell] == 3)
                            {
                                PosterPlacement posterPlacement = new PosterPlacement(
                                    positions[posInc] + new Vector3(-posterHorizontalOffset, posterVerticalOffset, 0f),
                                    Quaternion.Euler(0f, 90f, 0f)
                                );

                                queuedPosterPlacements.Enqueue(posterPlacement);
                            }
                            else if (!curCell.rightWall.solid && solidWallCounts.ContainsKey(rightCell) && solidWallCounts[rightCell] == 3)
                            {
                                PosterPlacement posterPlacement = new PosterPlacement(
                                    positions[posInc] + new Vector3(posterHorizontalOffset, posterVerticalOffset, 0f),
                                    Quaternion.Euler(0f, -90f, 0f)
                                );

                                queuedPosterPlacements.Enqueue(posterPlacement);
                            }
                            else if (!curCell.frontWall.solid && solidWallCounts.ContainsKey(frontCell) && solidWallCounts[frontCell] == 3)
                            {
                                PosterPlacement posterPlacement = new PosterPlacement(
                                    positions[posInc] + new Vector3(0f, posterVerticalOffset, -posterHorizontalOffset),
                                    Quaternion.Euler(0f, 0f, 0f)
                                );

                                queuedPosterPlacements.Enqueue(posterPlacement);
                            }
                            else if (!curCell.backWall.solid && solidWallCounts.ContainsKey(backCell) && solidWallCounts[backCell] == 3)
                            {
                                PosterPlacement posterPlacement = new PosterPlacement(
                                    positions[posInc] + new Vector3(0f, posterVerticalOffset, posterHorizontalOffset),
                                    Quaternion.Euler(0f, 180f, 0f)
                                );

                                queuedPosterPlacements.Enqueue(posterPlacement);
                            }
                        }

                        posInc++;
                    }
                }
            }

            // Spawn end room
            queuedBasePlacements.Enqueue(new QueuedBasePlacement(
                Mod.cachedPrefabs[TechType.BaseRoom],
                finishPosition,
                0
            ));

            PictureFrameHandler.SpawnFinishWall(finishPosition);
        }

        public static void SpawnMazeBeacon()
        {
            DestroyMazeBeacon();

            mazeBeaconGameObject = CraftData.InstantiateFromPrefab(
                Mod.cachedPrefabs[TechType.Beacon],
                TechType.Beacon
            );
            mazeBeaconGameObject.transform.position = basePosition + new Vector3(5f, 2f, -1f);
            mazeBeaconGameObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Beacon beacon = mazeBeaconGameObject.GetComponent<Beacon>();
            beacon.label = "Maze Start";

            mazeBeaconGameObject.transform.parent = Mod.mazeBase.transform;

            mazeBeaconGameObject.SetActive(true);
        }

        private static void DestroyMazeBeacon()
        {
            if (mazeBeaconGameObject != null)
            {
                UnityEngine.Object.Destroy(mazeBeaconGameObject);
                mazeBeaconGameObject = null;
            }
        }

        public static void Update()
        {
            if (rebuildOnNextFrame)
            {
                Mod.mazeBase.RebuildGeometry();
                rebuildOnNextFrame = false;
            }

            ProcessBaseDestroyQueue();
            ProcessBasePlacementQueue();
            ProcessCreaturePlacementQueue();
            ProcessPosterPlacementQueue();
        }

        public static bool MazeIsGenerating()
        {
            return queuedBaseDestroys.Count > 0
                || queuedBasePlacements.Count > 0
                || queuedCreaturePlacements.Count > 0
                || queuedPosterPlacements.Count > 0
            ;
        }

        public class QueuedBasePlacement
        {
            readonly GameObject gameObject;
            readonly Vector3 position;
            readonly int degreesRotated;

            public QueuedBasePlacement(GameObject gameObject, Vector3 position, int degreesRotated)
            {
                this.gameObject = gameObject;
                this.position = position;
                this.degreesRotated = degreesRotated;
            }

            public void Place()
            {
                Mod.PlaceBasePrefab(
                    gameObject,
                    position,
                    degreesRotated
                );
            }
        }

        private static void ProcessBasePlacementQueue()
        {
            //if (Time.time - lastBasePlacementTime < basePlacementDelay) { return; }

            if (queuedBaseDestroys.Count > 0) { return; };

            if (queuedBasePlacements.Count > 0)
            {
                //lastBasePlacementTime = Time.time;
                QueuedBasePlacement item = queuedBasePlacements.Dequeue();
                item.Place();

                if (!MazeIsGenerating())
                {
                    Mod.mazeBase.RebuildGeometry();
                    rebuildOnNextFrame = true;
                }
            }
        }

        private static void DestroyQueuedBase(Int3 cell)
        {
            // private readonly List<BaseGhost> ghosts = new List<BaseGhost>();
            FieldInfo ghostsField = typeof(Base).GetField("ghosts", BindingFlags.NonPublic | BindingFlags.Instance);
            List<BaseGhost> ghosts = ghostsField.GetValue(Mod.mazeBase) as List<BaseGhost>;

            // Clear all existing base pieces
            Int3 startCell = Mod.mazeBase.WorldToGrid(basePosition);
            Transform startTransform = Mod.mazeBase.GetCellObject(startCell);

            Transform transform = Mod.mazeBase.GetCellObject(cell);
            if (transform == null || transform == startTransform) return;

            BaseDeconstructable baseDeconstructable = transform.gameObject.GetComponentInChildren<BaseDeconstructable>();
            if (baseDeconstructable != null)
            {
                baseDeconstructable.Deconstruct();

                if (ghosts.Count > 0 && ghosts[0] != null)
                {
                    BaseGhost curGhost = ghosts[0];

                    curGhost.Deconstruct(Mod.mazeBase, new Int3.Bounds(ghosts[0].targetOffset, ghosts[0].targetOffset), null, FaceType.None);
                    ghosts.Remove(curGhost);
                }
            }
        }

        private static void DestroyDanglingBaseDeconstructables()
        {
            if (Mod.mazeBase != null)
            {
                Transform transform = Mod.mazeBase.transform;
                for (var i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform childChild = transform.GetChild(i);
                    if (childChild.name.Contains("BaseDeconstructable"))
                    {
                        UnityEngine.Object.Destroy(childChild.gameObject);
                    }
                }
            }
        }

        private static void ProcessBaseDestroyQueue()
        {
            //if (Time.time - lastDestroyBaseTime < destroyBaseDelay) { return; }

            if (queuedBaseDestroys.Count > 0)
            {
                Int3 cell = queuedBaseDestroys.Dequeue();
                //lastDestroyBaseTime = Time.time;
                DestroyQueuedBase(cell);

                if (queuedBaseDestroys.Count == 0)
                {
                    DestroyDanglingBaseDeconstructables();
                }

                if (!MazeIsGenerating()) {
                    Mod.mazeBase.RebuildGeometry();
                    rebuildOnNextFrame = true;
                }
            }
        }

        private static void ProcessCreaturePlacementQueue()
        {
            //if (Time.time - lastCreaturePlacementTime < creaturePlacementDelay) { return; }

            if (queuedBasePlacements.Count > 0) { return; };
            if (queuedBaseDestroys.Count > 0) { return; };

            if (queuedCreaturePlacements.Count > 0)
            {
                //lastCreaturePlacementTime = Time.time;
                Vector3 position = queuedCreaturePlacements.Dequeue();

                CreatureHandler.SpawnCreature(
                    TechType.CrashHome,
                    position + new Vector3(0f, -1.4f, 0f)
                );

                if (!MazeIsGenerating())
                {
                    Mod.mazeBase.RebuildGeometry();
                    rebuildOnNextFrame = true;
                }
            }
        }

        public static GameObject SpawnPoster(Vector3 position, Quaternion rotation)
        {
            // Spawn a poster
            GameObject gameObject = UnityEngine.Object.Instantiate(Mod.cachedPrefabs[TechType.PosterKitty]);
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;

            // Fix poster's lighting
            SubRoot currentSub = Mod.mazeBase.GetComponent<SubRoot>();
            gameObject.transform.parent = currentSub.GetModulesRoot();
            SkyEnvironmentChanged.Send(gameObject, currentSub);

            // Prevent player from pickup up poster
            Pickupable pickupable = gameObject.GetComponent<Pickupable>();
            UnityEngine.Object.Destroy(pickupable);

            return gameObject;
        }

        private static void DestroyPosters()
        {
            foreach (var item in posters) { UnityEngine.Object.Destroy(item.gameObject); }
            posters.Clear();
        }

        private static void ProcessPosterPlacementQueue()
        {
            if (queuedBasePlacements.Count > 0) { return; };
            if (queuedBaseDestroys.Count > 0) { return; };
            if (queuedCreaturePlacements.Count > 0) { return; };

            if (queuedPosterPlacements.Count > 0)
            {
                PosterPlacement placement = queuedPosterPlacements.Dequeue();

                GameObject poster = SpawnPoster(placement.position, placement.rotation);
                posters.Add(poster);

                if (!MazeIsGenerating())
                {
                    Mod.mazeBase.RebuildGeometry();
                    rebuildOnNextFrame = true;
                }
            }
        }

        [HarmonyPatch(typeof(Base))]
        [HarmonyPatch("RebuildGeometry")]
        internal class Patch_Base_RebuildGeometry
        {
            [HarmonyPrefix]
            public static bool Prefix(Base __instance)
            {
                if (__instance == Mod.mazeBase && MazeIsGenerating())
                {
                    return false;
                }

                return true;
            }
        }

        public static void DestroyMaze()
        {
            if (Mod.mazeBase == null) { return; }

            try
            {
                Int3.Bounds bounds = Mod.mazeBase.Bounds;
                for (int z = bounds.mins.z; z < bounds.maxs.z + 1; z++)
                {
                    for (int y = bounds.mins.y; y < bounds.maxs.y + 1; y++)
                    {
                        for (int x = bounds.mins.x; x < bounds.maxs.x + 1; x++)
                        {
                            Int3 cell = new Int3(x, y, z);
                            if (Mod.mazeBase.GetCellType(cell) != CellType.Empty)
                            {
                                queuedBaseDestroys.Enqueue(cell);
                            }
                        }
                    }
                }

                DestroyPosters();
                CreatureHandler.DestroyCreatures();
                PictureFrameHandler.DestroyFinishWall();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogInfo($"DestroyMaze exception:\n{e}");
            }
        }
    }
}
