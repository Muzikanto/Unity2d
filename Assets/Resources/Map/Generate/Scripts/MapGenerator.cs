using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;


namespace Assets.Generation
{
    class MapGenerator : MonoBehaviour
    {
        [Header("Stats noise")]
        public int width = 3000;
        public int height = 1500;
        public float scale = 0.07f;
        [Range(0, 1)]
        public float range = 0.37f;
        [Header("Start texture point")]
        public int originTextureX = 0;
        public int originTextureY = 0;

        [Header("Spaces")]
        public int UpSpace = 350;
        public int DownSpace = 100;

        [Header("Ground")]
        public int heightEarth = 50;
        public int stepGround = 30;
        public int amplitudeUpGround = 59;
        public int amplitudeADGround = 50;
        public int FindSizeHoles = 20;
        [Range(2f, 2.3f)]
        public float SmoothGround = 2;

        [Header("Ocean")]
        public int oceanAmplitude = 15;

        [Range(0f, 1f)]
        public float chanceWater = 0.03f;
        [Range(0f, 1f)]
        public float chanceLava = 0.1f;

        [SerializeField]
        [Header("Dange")]
        DangeSettings dangeSettings;

        private ID[,] map;
        private DangeGenerator dange;

        public UnityEngine.Vector3Int spawn;
 

        public ID[,] GenerateMap()
        {
            map = new ID[width, height];

            //Noise
            float[,] noiseMap = GenerateNoiseMap();
            replace(noiseMap);
            FindHoles();

            //Steps
            int secHeight = height - UpSpace - heightEarth;
            resourcesItemsUpStartPos = UpSpace + heightEarth;
            resourcesItemsMiddleStartPos = UpSpace + heightEarth + secHeight / 40;
            resourcesItemsDownStartPos = UpSpace + heightEarth + secHeight / 4 * 2;

            CreateBioms();

            //Horizont
            int[] arrHeight = SmoothLine(OceansLine(HorizontalLine(stepGround, amplitudeUpGround), oceanAmplitude));
            //Ad Line
            int[] arrHeightAD = SmoothLine(HorizontalLine(stepGround, amplitudeADGround));

          
            SpawnResources();

            CreateGround(arrHeight, arrHeightAD);

            PlaceOceans(arrHeight);

            CreateRooms(arrHeight[0]);

            CreateWaterInCaves(arrHeight, arrHeightAD);

            CreateAdFortes(arrHeightAD);

            CreateIslands();

            // CreateDange();

            SetSpawn(arrHeight[(int)(width / 2)]);

            return map;
        }

        private void SetSpawn(int y)
        {
            spawn = new Vector3Int((int)((width / 2) * 0.32f), (int)((height - 1 - (UpSpace + y)) * 0.32f) + 2, 1);
        }


        //------------------------------------
        private float[,] GenerateNoiseMap()
        {
            float[,] noiseMap = new float[width, height];

            if (scale <= 0)
                scale = 0.0001f;

            for (int y = UpSpace; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xCoord = originTextureX + x * scale;
                    float yCoord = originTextureY + y * scale;
                    float perLineValue = Mathf.PerlinNoise(xCoord, yCoord);
                    noiseMap[x, y] = perLineValue;
                }
            }
            return noiseMap;
        }

        private void replace(float[,] noiseMap)
        {
            for (int y = UpSpace; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (noiseMap[x, y] < range)
                        map[x, y] = 0;
                    else
                        map[x, y] = ID.Stone;
                }
            }
        }

        private void FindHoles()
        {
            if (FindSizeHoles == 0 || FindSizeHoles > 100)
                return;
            for (int x = FindSizeHoles; x < width - FindSizeHoles; x++)
            {
                for (int y = UpSpace; y < height - DownSpace; y++)
                {
                    bool draw = true;
                    for (int i = 0; i < FindSizeHoles; i++)
                    {
                        if (map[x + i, y] == 0)
                            draw = false;
                        if (map[x, y + i] == 0)
                            draw = false;
                        if (map[x + FindSizeHoles - i, y + FindSizeHoles] == 0)
                            draw = false;
                        if (map[x + FindSizeHoles, y + FindSizeHoles - i] == 0)
                            draw = false;


                    }
                    if (draw)
                    {
                        for (int i = 0; i < FindSizeHoles; i++)
                        {
                            for (int j = 0; j < FindSizeHoles; j++)
                            {
                                map[x + i, y + j] = ID.Stone;
                            }
                        }
                    }
                }
            }
        }


        //------------------------------------
        private int[] HorizontalLine(int stepGround, int amplitudeGround)
        {
            int[] arrHeight = new int[width];

            //BASE
            int lastHeight = 0;
            for (int x = stepGround; x < width - stepGround; x++)
            {
                if (x % (2 * stepGround) == 0)
                    lastHeight = (arrHeight[x - stepGround] + arrHeight[x + stepGround]) / 2 + Random.Range(-1 * stepGround, 1 * stepGround);
                arrHeight[x] = (int)(Mathf.Sin((Mathf.PI / 180) * lastHeight) * amplitudeGround);
            }

            lastHeight = arrHeight[0];

            return arrHeight;
        }

        private int[] OceansLine(int[] arrHeight, int oceanAmplitude)
        {
            //Oceans
            int startXOcean = bioms[0].width / 4 * 3;
            for (int x = startXOcean, x1 = startXOcean + 1; x >= 0; x--, x1++)
                arrHeight[x] = (int)Mathf.Sqrt(oceanAmplitude * Mathf.Abs(x - startXOcean)) + 25;
            for (int x = startXOcean; x < bioms[0].width - stepGround / 2; x++)
                if (arrHeight[x + 1] - arrHeight[x] <= -2)
                    arrHeight[x + 1] = (int)((float)(arrHeight[x + stepGround / 2] + arrHeight[x]) / SmoothGround);
            startXOcean = width - bioms[bioms.Length - 1].width / 4 * 3;
            for (int x = startXOcean, x1 = startXOcean - 1; x < width; x++, x1--)
                arrHeight[x] = (int)Mathf.Sqrt(oceanAmplitude * Mathf.Abs(x - startXOcean)) + 25;
            for (int x = startXOcean; x > width - bioms[0].width + stepGround / 2; x--)
                if (arrHeight[x - 1] - arrHeight[x] <= -2)
                    arrHeight[x - 1] = (int)((float)(arrHeight[x - stepGround / 2] + arrHeight[x]) / SmoothGround);
            return arrHeight;
        }

        private int[] SmoothLine(int[] arrHeight)
        {
            //Smooth
            for (int x = 0, x2 = width - 1; x < width - 1; x++, x2--)
            {
                if (arrHeight[x + 1] - arrHeight[x] <= -2)
                {
                    arrHeight[x + 1] = (int)((float)((arrHeight[x + 1] + arrHeight[x])) / SmoothGround);
                    arrHeight[x + 1] = (int)((float)((arrHeight[x + 1] + arrHeight[x])) / SmoothGround);
                }
                if (arrHeight[x2 - 1] - arrHeight[x2] <= -2)
                {
                    arrHeight[x2 - 1] = (int)((float)((arrHeight[x2 - 1] + arrHeight[x2])) / SmoothGround);
                    arrHeight[x2 - 1] = (int)((float)((arrHeight[x2 - 1] + arrHeight[x2])) / SmoothGround);
                }
            }
            for (int x = width - 1, x2 = 0; x > 0; x--, x2++)
            {
                if (arrHeight[x - 1] - arrHeight[x] >= 2)
                {
                    arrHeight[x - 1] = (int)((float)((arrHeight[x - 1] + arrHeight[x])) / SmoothGround);
                    arrHeight[x - 1] = (int)((float)((arrHeight[x - 1] + arrHeight[x])) / SmoothGround);
                }
                if (arrHeight[x2 + 1] - arrHeight[x2] >= 2)
                {
                    arrHeight[x2 + 1] = (int)((float)((arrHeight[x2 + 1] + arrHeight[x2])) / SmoothGround);
                    arrHeight[x2 + 1] = (int)((float)((arrHeight[x2 + 1] + arrHeight[x2])) / SmoothGround);
                }
            }
            return arrHeight;
        }

        //------------------------------------

        private void CreateGround(int[] arrHeight, int[] arrHeightAD)
        {
            int startBiomX = 0;
            foreach (Biom biom in bioms)
            {
                for (int x = startBiomX; x < startBiomX + biom.width; x++)
                {
                    //UpSpace
                    for (int y = UpSpace + arrHeight[x]; y > UpSpace - arrHeight[x]; y--)
                    {
                        map[x, y] = 0;
                    }

                    //Grass
                    map[x, UpSpace + arrHeight[x]] = biom.ground;
                    map[x, UpSpace + arrHeight[x] + 1] = biom.ground;

                    //Dirt
                    for (int y = UpSpace + arrHeight[x] + 2; y < resourcesItemsUpStartPos + arrHeight[x]; y++)
                    {
                        if (map[x, y] == ID.Stone || map[x, y] == 0)
                            map[x, y] = biom.dirt;
                    }


                    //Stone High Solid
                    for (int y = resourcesItemsDownStartPos + arrHeightAD[x]; y < height - DownSpace + arrHeightAD[x]; y++)
                    {
                        if (map[x, y] == ID.Stone)
                            map[x, y] = ID.StoneHighSolid;
                    }

                    //Stone
                    for (int y = resourcesItemsUpStartPos + arrHeight[x]; y < resourcesItemsDownStartPos + arrHeightAD[x]; y++)
                    {
                        if (map[x, y] == ID.Stone)
                            map[x, y] = biom.stone;
                    }

                    //DownSpace
                    for (int y = height - DownSpace + arrHeightAD[x]; y < height - 30 + arrHeightAD[x]; y++)
                    {
                        if (y < height)
                            map[x, y] = 0;
                        else
                            break;
                    }

                    //Ad Ground
                    for (int y = height - 30 + arrHeightAD[x]; y < height; y++)
                    {
                        if (map[x, y] == ID.Stone)
                            map[x, y] = ID.AdGround;
                        else
                            map[x, y] = ID.Lava;
                    }

                }
                startBiomX += biom.width;
            }
        }

        private void CreateBioms()
        {
            bioms = new Biom[7];
            for (int i = 0; i < bioms.Length; i++)
                bioms[i] = null;


            bioms[3] = standart;
            bioms[0] = beach;
            bioms[bioms.Length - 1] = beach;
            if (Random.Range(0f, 1f) > 0.5f)
                bioms[1] = standart;
            else
                bioms[bioms.Length - 2] = standart;


            int size = width / 6;
            jungle.width = size;
            desert.width = size;
            snow.width = size;
            standart.width = size;
            beach.width = size / 2;


            Biom[] randArr = { jungle, desert, snow };
            System.Random rand = new System.Random();
            for (int i = randArr.Length - 1; i > 0; i--)
            {
                int j = rand.Next(0, i + 1);
                Biom tmp = randArr[i];
                randArr[i] = randArr[j];
                randArr[j] = tmp;
            }


            for (int i = 0, j = 0; i < bioms.Length; i++)
                if (bioms[i] == null)
                    bioms[i] = randArr[j++];




        }

        private void PlaceOceans(int[] arrHeight)
        {
            int startXOcean = bioms[0].width / 4 * 3;
            for (int x = startXOcean; x >= 0; x--)
                for (int y = UpSpace + arrHeight[x] - 1; y > UpSpace + arrHeight[startXOcean]; y--)
                    map[x, y] = ID.Water;

            startXOcean = width - bioms[bioms.Length - 1].width / 4 * 3;
            for (int x = startXOcean; x < width; x++)
                for (int y = UpSpace + arrHeight[x] - 1; y > UpSpace + arrHeight[startXOcean]; y--)
                    map[x, y] = ID.Water;
        }

        //------------------------------------
        private void SpawnResources()
        {
            int maxCount = 5;
            ResourceItem[] resourseItems = resourcesItemsUpGround;
            ID resourse = ID.Stone;

            for (int y = UpSpace; y < height - DownSpace; y += 20)
            {
                if (y < resourcesItemsUpStartPos)
                {
                    maxCount = 5;
                    resourseItems = resourcesItemsUpGround;
                }
                else if (y < resourcesItemsMiddleStartPos)
                {
                    maxCount = 8;
                    resourseItems = resourcesItemsUp;
                }
                else if (y < resourcesItemsDownStartPos)
                {
                    maxCount = 12;
                    resourseItems = resourcesItemsMiddle;
                }
                else if (y < height - DownSpace)
                {
                    maxCount = 16;
                    resourseItems = resourcesItemsDown;
                }
                for (int x = 10; x < width - 10; x += 20)
                {
                    resourse = RandomResourse(resourseItems);
                    SetResources(x + Random.Range(-10, 10), y + Random.Range(-10, 10), maxCount, resourse);
                }
            }
        }

        private void SetResources(int startX, int startY, int maxCount, ID resouces)
        {
            int Count = Random.Range(3, maxCount);
            int x = startX;
            int y = startY;

            for (int i = 0, j = 0; i <= Count && j < 50; j++)
            {
                if (x < 0 || x >= width)
                    x = startX;
                if (y < UpSpace || y > height - DownSpace)
                    y = startY;

                if (map[x, y] == ID.Stone)
                {
                    map[x, y] = resouces;
                    i++;
                }
                else
                {
                    int rand = Random.Range(0, 4);
                    switch (rand)
                    {
                        case 0:
                            x += 1;
                            break;
                        case 1:
                            x -= 1;
                            break;
                        case 2:
                            y += 1;
                            break;
                        case 3:
                            y -= 1;
                            break;
                    }
                }
            }
        }

        private ID RandomResourse(ResourceItem[] arr)
        {
            float rand = Random.Range(0f, 1f);

            float[] arrChance = new float[arr.Length];
            arrChance[0] = arr[0].chance;
            for (int i = 1; i < arr.Length; i++)
            {
                arrChance[i] = arrChance[i - 1] + arr[i].chance;
            }

            for (int i = 0; i < arr.Length; i++)
            {
                if (rand <= arrChance[i])
                    return arr[i].resourse;
            }

            return arr[0].resourse;
        }

        private Biom GetBiomFromX(int x)
        {
            int secondX = 0;
            for (int i = 0; i < bioms.Length; i++)
            {
                secondX += bioms[i].width;
                if (x < secondX)
                    return bioms[i];
            }
            return null;
        }

        //------------------------------------

        private void CreateRooms(int maxDownHeightLine)
        {
            ID[,] room;

            for (int y = UpSpace + heightEarth + maxDownHeightLine; y < resourcesItemsDownStartPos; y += 100)
            {
                for (int x = 31; x < width - 31; x += 100)
                {
                    int startX = x + Random.Range(-30, 30);
                    int startY = y + Random.Range(-30, 30);

                    Biom biom = GetBiomFromX(x);
                    room = ObjectsMap.GetRandomRoom(biom.wallRoom, biom.chest);
                    for (int i = 0; i < room.GetLength(0); i++)
                    {
                        for (int j = 0; j < room.GetLength(1); j++)
                        {
                            if (startX - j >= 0 && room[i, j] != ID.NotSet)
                                map[startX - j, startY + i] = room[i, j];
                        }
                    }
                }
            }
        }

        private void CreateAdFortes(int[] arrHeightAD)
        {
            ID[,] fort;
            int xIncr = Random.Range(20, 50);
            for (int x = width - 10; x >= 50; x -= xIncr)
            {
                fort = ObjectsMap.GetRandomFortes(ID.AdBriks, ID.AdChest);
                int fortWidth = fort.GetLength(0);
                int fortHeight = fort.GetLength(1);
                xIncr = Random.Range(fortWidth + stepGround, fortWidth + stepGround * 5);

                int y = height + arrHeightAD[x] - amplitudeADGround - fortHeight;
                if (y + fortWidth < height)
                    while (map[x - fortWidth / 2, y + fortWidth] == ID.None)
                        y++;
                else
                    while (y + fortWidth > height)
                        y--;

                for (int y1 = 0; y1 < fortWidth; y1++)
                    for (int x1 = 0; x1 < fortHeight; x1++)
                        if (x - x1 >= 0 && x - x1 < width && y + y1 < height && fort[y1, x1] != ID.NotSet)
                            map[x - x1, y + y1] = fort[y1, x1];
            }
        }

        private void CreateWaterInCaves(int[] arrHeight, int[] arrHeightAD)
        {
            for (int x = 0; x < width; x += 10)
            {
                for (int y = height - DownSpace - 1; y > UpSpace + arrHeight[x]; y -= 10)
                {
                    if (map[x, y] == ID.None)
                    {
                        if (y < resourcesItemsDownStartPos && Random.Range(0f, 1f) < chanceWater)
                            CheckPlaceWaterInLines(arrHeightAD, x, y, ID.Water);
                        else
                             if (y > resourcesItemsDownStartPos && Random.Range(0f, 1f) < chanceLava)
                            CheckPlaceWaterInLines(arrHeightAD, x, y, ID.Lava);
                    }
                }
            }
        }

        private void CheckPlaceWaterInLines(int[] arrHeightAD, int x, int y, ID source)
        {
            int count = 100;
            int forIsActive = 2;
            for (int x1 = 1, x2 = -1; forIsActive > 0 && count > 0; x1++, x2--, count--)
            {
                if (x + x1 >= 0 && x + x1 < width && map[x + x1, y] == ID.None)
                    for (int y1 = 0; y >= 0 && y < height; y1--)
                    {
                        if (y - y1 > height - DownSpace + arrHeightAD[x + x1])
                            return;
                        if (y - y1 > 0 && y - y1 < height && map[x + x1, y - y1] == ID.None)
                        {
                            map[x + x1, y - y1] = source;
                        }
                        else
                        {
                            CheckPlaceWaterInLines(arrHeightAD, x + x1, y - y1 / 2, source);
                            break;
                        }
                    }
                else
                    forIsActive--;

                if (x + x2 >= 0 && x + x2 < width && map[x + x2, y] == ID.None)
                    for (int y1 = 0; y >= 0 && y < height; y1--)
                    {
                        if (y - y1 > height - DownSpace + arrHeightAD[x + x2])
                            return;
                        if (y - y1 > 0 && y - y1 < height && map[x + x2, y - y1] == ID.None)
                            map[x + x2, y - y1] = source;
                        else
                        {
                            CheckPlaceWaterInLines(arrHeightAD, x + x2 - 1, y - y1 / 2, source);
                            break;
                        }
                    }
                else
                    forIsActive--;
            }
        }

        private void CreateIslands()
        {
            ID[,] island;
            int xIncr = Random.Range(200, 450);
            int y = UpSpace / 2;
            for (int x = width - 1 - xIncr; x >= 70; x -= xIncr)
            {
                int startY = y + Random.Range(-50, 30);

                island = ObjectsMap.GetRandomIsland();

                xIncr = island.GetLength(1) + Random.Range(150, 300);
                if (x - island.GetLength(1) >= width && x - island.GetLength(1) < 0)
                    continue;
                for (int i = 0; i < island.GetLength(0); i++)
                    for (int j = 0; j < island.GetLength(1); j++)
                        if (x - j >= 0 && island[i, j] != ID.NotSet)
                            map[x - j, startY + i] = island[i, j];
            }
        }

        private void CreateDange()
        {
            DangeGenerator dange = new DangeGenerator(dangeSettings);
            Vector2Int enter;
            int xIncr;
            if (bioms[1].biomId == BiomID.Standart)
            {
                map = dange.CreateDange(map, bioms[0].width, resourcesItemsMiddleStartPos);
                enter = dange.getEnterToDangeLeft();
                xIncr = 1;
            }
            else
            {
                map = dange.CreateDange(map, width - bioms[bioms.Length - 1].width - bioms[bioms.Length - 2].width, resourcesItemsMiddleStartPos);
                enter = dange.getEnterToDangeRight();
                xIncr = -1;
            }

            for (int x = enter.x, y = enter.y; y > UpSpace || map[x, y] != ID.None; x += xIncr, y--)
                for (int i = 0; i < 5; i++)
                    map[x - i, y] = ID.None;
        }

        public class ObjectsMap
        {
            public static ID[,] GetRoom(int id, ID wall, ID chest)
            {
                ID w = wall;
                ID c = chest;
                ID n = ID.NotSet;

                if (id == 0)
                    return new ID[,]
                    {
                    {w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,c,0,0,0,0,0,c,0,0,0,w},
                    {w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w}
                    };
                else if (id == 1)
                    return new ID[,]
                    {
                    {n,n,n,n,w,w,w,w,w,w,w,w},
                    {n,n,n,n,w,0,0,0,0,0,0,w},
                    {n,n,n,n,w,0,0,0,0,0,0,w},
                    {w,w,w,w,w,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,c,0,0,0,0,0,w},
                    {w,w,w,w,w,w,w,w,w,w,w,w},
                    };
                else
                    return new ID[,]
                    {
                    {w,w,w,w,w,w,w,w,w,w,w,w},
                    {w,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,c,0,0,0,0,0,w},
                    {w,w,w,w,w,w,w,w,w,w,w,w},
                    };
            }

            public static ID[,] GetRandomRoom(ID wall, ID chest)
            {
                float rand = Random.Range(0f, 1f);
                float[] chances = { 0.7f, 0.3f };
                float chance = 0;
                for (int i = 0; i < chances.Length; i++)
                {
                    chance += chances[i];
                    if (rand < chance)
                        return GetRoom(i, wall, chest);
                }
                return GetRoom(0, wall, chest);
            }


            public static ID[,] GetFortes(int id, ID wall, ID chest)
            {
                ID w = wall;
                ID c = chest;
                ID n = ID.NotSet;

                if (id == 0)
                    return new ID[,]
                    {
                    {w,w,0,0,w,w,w,w,w,w,w,w,w,w,w,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                    {w,0,0,0,0,w,w,w,w,w,w,w,w,w,w,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,w,w,w,w,w,0,0,0,0,w,w,w,w,w,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,c,0,0,0,0,w},
                    {w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w},
                    };
                else
                    return new ID[,]
                    {
                    {w,w,w,w,w,w,w,w,0,0,0,w,w,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,w,w,w,w,w,w,w,w,w,w,w,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,w,w,w,w,0,0,0,w,w,w,w,w,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,w,w,w,w,0,0,0,w,w,w,w,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,w,w,w,w,w,w,w,0,0,0,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,w},
                    {w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w},
                    };
            }

            public static ID[,] GetRandomFortes(ID wall, ID chest)
            {
                float rand = Random.Range(0f, 1f);
                float[] chances = { 0.7f, 0.3f };
                float chance = 0;
                for (int i = 0; i < chances.Length; i++)
                {
                    chance += chances[i];
                    if (rand < chance)
                        return GetFortes(i, wall, chest);
                }
                return GetFortes(0, wall, chest);
            }

            public static ID[,] GetIsland(int id)
            {
                ID n = ID.NotSet;
                if (id == 0)
                {
                    ID w = ID.WoodPlanks;
                    ID c = ID.Chest;
                    ID q = ID.GrassGround;
                    ID g = ID.Dirt;
                    return new ID[,]
                    {
                        {n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,n,n,n,n,n,n,n,n,n,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,n,c,n,n,n,n,n,w,n,n,n,n,n,n,n,n,n,n,n,n,n,n},
                        {n,q,q,q,q,q,q,q,q,q,q,q,q,q,q,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,q,q,q,q,q,q,q,q,q,q,q,q,q,n},
                        {q,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,q},
                        {g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g},
                        {n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g},
                        {n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n},
                        {n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n},
                        {n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n},
                        {n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n},
                        {n,n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n,n},
                        {n,n,n,n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n,n,n,n,n,n,n,n}


                    };
                }
                else if (id == 1)
                {
                    ID g = ID.CloudGround;
                    ID w = ID.Water;
                    return new ID[,]
                    {
                        {n,g,g,g,g,g,g,g,g,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,g,g,g,g,g,g,g,g,g,n},
                        {g,g,g,g,g,g,g,g,g,g,g,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,w,g,g,g,g,g,g,g,g,g,g,g,g,g},
                        {g,g,g,g,g,g,g,g,g,g,g,g,g,g,w,w,w,w,w,w,w,w,w,w,w,w,w,w,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g},
                        {n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,w,w,w,w,w,w,w,w,w,w,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g},
                        {n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n},
                        {n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n},
                        {n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n},
                        {n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n},
                        {n,n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n,n},
                        {n,n,n,n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n,n,n,n},
                        {n,n,n,n,n,n,n,n,n,n,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,g,n,n,n,n,n,n,n,n,n}

                    };
                }
                else
                    return new ID[,]
                    {

                    };
            }

            public static ID[,] GetRandomIsland()
            {
                float rand = Random.Range(0f, 1f);
                float[] chances = { 0.4f, 0.6f };
                float chance = 0;
                for (int i = 0; i < chances.Length; i++)
                {
                    chance += chances[i];
                    if (rand < chance)
                        return GetIsland(i);
                }
                return GetIsland(0);
            }
        }



        [System.Serializable]
        public class DangeSettings
        {
            public int countRoomsSpawn = 50;
            public int countRooms = 30;
            public Vector2Int roomSpawnRange = new Vector2Int(100, 150);
            public Vector2Int wRoomRange = new Vector2Int(30, 60);
            public Vector2Int hRoomRange = new Vector2Int(20, 40);
        }

        public class DangeGenerator
        {
            private DangeSettings sett;
            private List<Room> rooms = new List<Room>();

            private ID[,] arrDange;

            private Vector2[] graph;

            private int startXInMap, startYInMap;

            public DangeGenerator(DangeSettings sett)
            {
                this.sett = sett;
                createRooms();
                SepareteRooms();
                SortForY();
                DeleteSmallRooms();
                PushRooms();
                CreateLinks();
            }

            private void createRooms()
            {
                for (int i = 0; i < sett.countRoomsSpawn; i++)
                {
                    rooms.Add(new Room(roundm((int)Random.Range(sett.wRoomRange.x, sett.wRoomRange.y), 4),
                                       roundm((int)Random.Range(sett.hRoomRange.x, sett.hRoomRange.y), 4)));
                    rooms[i].setPos(getRandomPoint());
                }
            }

            private int roundm(int n, int m)
            {
                return (int)(Mathf.Floor(((n + m - 1) / m)) * m);
            }

            private Vector2 getRandomPoint()
            {
                return new Vector2(Random.Range(sett.roomSpawnRange.x, sett.roomSpawnRange.x * 2), Random.Range(sett.roomSpawnRange.y, sett.roomSpawnRange.y * 2));
            }

            private void SepareteRooms()
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    for (int j = 0; j < rooms.Count; j++)
                    {
                        if (i != j && IsInstansiateRooms(rooms[i], rooms[j]))
                        {

                            int xIncr;
                            int yIncr;
                            if (Random.Range(0, 2) == 0)
                                yIncr = 1;
                            else
                                yIncr = -1;
                            if (Random.Range(0, 2) == 0)
                                xIncr = 1;
                            else
                                xIncr = -1;

                            while (IsInstansiateRooms(rooms[i], rooms[j]))
                            {
                                if (Random.Range(0, 2) == 0)
                                    rooms[i].x += xIncr;
                                else
                                    rooms[i].y += yIncr;
                            }
                            i = j = 0;
                        }
                    }
                }
            }

            private bool IsInstansiateRooms(Room room1, Room room2)
            {
                return ((room2.x >= room1.x && room2.x <= room1.x + room1.w) || (room1.x >= room2.x && room1.x <= room2.x + room2.w)) &&
                    ((room2.y >= room1.y && room2.y <= room1.y + room1.h) || (room1.y >= room2.y && room1.y <= room2.y + room2.h));
            }

            private void SortForY()
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    for (int j = i + 1; j < rooms.Count - 1; j++)
                    {
                        if (rooms[i].y > rooms[j].y)
                        {
                            Room tmp = rooms[i];
                            rooms[i] = rooms[j];
                            rooms[j] = tmp;
                        }
                    }
                }
            }

            private void DeleteSmallRooms()
            {
                while (rooms.Count > sett.countRooms)
                    rooms.RemoveAt(Random.Range(0, rooms.Count));
            }

            private float GetDistanceFromRooms(Room room1, Room room2)
            {
                return Vector2.Distance(new Vector2(room1.x + room1.w / 2, room1.y + room1.h / 2), new Vector2(room2.x + room2.w / 2, room2.y + room2.h / 2));
            }

            private void PushRooms()
            {
                int minX = 10000, maxX = 0, minY = 10000, maxY = 0;

                foreach (Room room in rooms)
                {
                    if (room.x < minX) minX = room.x;
                    if (room.x > maxX) maxX = room.x;
                    if (room.y < minY) minY = room.y;
                    if (room.y > maxY) maxY = room.y;
                }

                int sX = 0, sY = 0;
                if (minX != 0) sX = -minX + 5;
                if (minY != 0) sY = -minY + 5;
                for (int i = 0; i < rooms.Count; i++)
                {
                    rooms[i].x += sX;
                    rooms[i].y += sY;
                }

                arrDange = new ID[maxX + sX + sett.wRoomRange.y * 2, maxY + sY + +sett.hRoomRange.y * 2];

                foreach (Room room in rooms)
                {
                    int startX = room.x;
                    int startY = room.y;
                    int endX = startX + room.w;
                    int endY = startY + room.h;
                    for (int x = startX; x <= endX; x++)
                        for (int y = startY; y <= endY; y++)
                            if (x == startX || x == endX || y == startY || y == endY)
                                arrDange[x, y] = ID.DangeBriks;
                            else
                                arrDange[x, y] = ID.NotSet;
                }
            }

            private void CreateLinks()
            {
                int[] graph = new int[rooms.Count];
                for (int i = 0; i < graph.Length; i++) graph[i] = -1;

                for (int i = 0; i < rooms.Count; i++)
                {
                    float minDist = 10000;
                    for (int j = 0; j < rooms.Count; j++)
                    {
                        if (i != j && graph[j] == -1)
                        {
                            float distance = GetDistanceFromRooms(rooms[i], rooms[j]);
                            if (distance < minDist)
                            {
                                minDist = distance;
                                graph[i] = j;
                            }
                        }
                    }
                }


                for (int i = 0; i < graph.Length - 1; i++)
                {
                    int x = rooms[i].x, y = rooms[i].y;
                    int xIncrement, yIncrement;

                    if (rooms[i].x > rooms[graph[i]].x) xIncrement = -1;
                    else xIncrement = 1;
                    if (rooms[i].y < rooms[graph[i]].y) yIncrement = 1;
                    else yIncrement = -1;


                    while (true)
                    {
                        int secondX = x + rooms[i].w / 2;
                        int secondY = rooms[i].y + rooms[i].h / 2;
                        if (arrDange[secondX, secondY] != ID.NotSet)
                        {
                            arrDange[secondX, secondY] = ID.NotSet;
                            arrDange[secondX, secondY + 1] = ID.NotSet;
                            arrDange[secondX, secondY - 1] = ID.NotSet;

                            if (arrDange[secondX, secondY + 2] != ID.NotSet) arrDange[secondX, secondY + 2] = ID.DangeBriks;
                            if (arrDange[secondX, secondY - 2] != ID.NotSet) arrDange[secondX, secondY - 2] = ID.DangeBriks;
                        }

                        x += xIncrement;
                        if ((xIncrement < 0 && x + rooms[i].w / 2 <= rooms[graph[i]].x + rooms[graph[i]].w / 2) ||
                            (xIncrement > 0 && x + rooms[i].w / 2 >= rooms[graph[i]].x + rooms[graph[i]].w / 2))
                        {
                            if (arrDange[secondX + xIncrement, secondY - 2] != ID.NotSet) arrDange[secondX + xIncrement, secondY - 2] = ID.DangeBriks;
                            if (arrDange[secondX + xIncrement * 2, secondY - 2] != ID.NotSet) arrDange[secondX + xIncrement * 2, secondY - 2] = ID.DangeBriks;
                            if (arrDange[secondX + xIncrement * 3, secondY - 2] != ID.NotSet) arrDange[secondX + xIncrement * 3, secondY - 2] = ID.DangeBriks;
                            break;
                        }
                    }

                    y = rooms[i].y;
                    while (true)
                    {
                        int secondX = x + rooms[i].w / 2;
                        int secondY = y + rooms[i].h / 2;
                        if (arrDange[secondX, secondY] != ID.NotSet)
                        {
                            arrDange[secondX, secondY] = ID.NotSet;
                            arrDange[secondX + 1, secondY] = ID.NotSet;
                            arrDange[secondX - 1, secondY] = ID.NotSet;

                            if (arrDange[secondX + 2, secondY] != ID.NotSet) arrDange[secondX + 2, secondY] = ID.DangeBriks;
                            if (arrDange[secondX - 2, secondY] != ID.NotSet) arrDange[secondX - 2, secondY] = ID.DangeBriks;
                        }

                        y += yIncrement;
                        if ((yIncrement < 0 && y + rooms[i].h / 2 <= rooms[graph[i]].y + rooms[graph[i]].h / 2) || (yIncrement > 0 && y + rooms[i].h / 2 > rooms[graph[i]].y + rooms[graph[i]].h / 2))
                        {
                            int secondY2 = rooms[i].y + rooms[i].h / 2;
                            if (arrDange[secondX - 2, secondY2 - yIncrement] != ID.NotSet) arrDange[secondX - 2, secondY2 - yIncrement] = ID.DangeBriks;
                            if (arrDange[secondX - 2, secondY2 - yIncrement * 2] != ID.NotSet) arrDange[secondX - 2, secondY2 - yIncrement * 2] = ID.DangeBriks;
                            break;
                        }

                    }

                }
            }

            public ID[,] CreateDange(ID[,] map, int xInMap, int yInMap)
            {
                this.startXInMap = xInMap;
                this.startYInMap = yInMap;
                for (int x = 0; x < arrDange.GetLength(0); x++)
                    for (int y = 0; y < arrDange.GetLength(1); y++)
                        if (arrDange[x, y] != ID.None)
                            map[x + xInMap, y + yInMap] = arrDange[x, y];

                return map;
            }

            public Vector2Int getEnterToDangeRight()
            {
                return new Vector2Int(rooms[0].x + startXInMap + rooms[0].w / 4, rooms[0].y + startYInMap + rooms[0].h / 4 * 3);
            }

            public Vector2Int getEnterToDangeLeft()
            {
                return new Vector2Int(rooms[0].x + startXInMap + rooms[0].w - rooms[0].w / 4, rooms[0].y + startYInMap + rooms[0].h / 4 * 3);
            }

            private class Room
            {
                public int w, h, x, y;
                public Room(int w, int h, Vector2 pos)
                {
                    this.w = w;
                    this.h = h;
                    this.x = (int)pos.x;
                    this.y = (int)pos.y;
                }
                public Room(int w, int h)
                {
                    this.w = w;
                    this.h = h;
                }
                public void setPos(Vector2 pos)
                {
                    this.x = (int)pos.x;
                    this.y = (int)pos.y;
                }
            }
        }



        //------------------------------------
        Biom jungle = new Biom(BiomID.Jungle, ID.JungleGrassGround, ID.Dirt, ID.JungleStone, ID.JungleBriks, ID.JungleChest);
        Biom desert = new Biom(BiomID.Desert, ID.Sand, ID.SandStone, ID.SandStone, ID.Briks, ID.Chest);
        Biom snow = new Biom(BiomID.Snow, ID.Snow, ID.Snow, ID.SnowStone, ID.SnowWoodPlanks, ID.SnowChest);
        Biom standart = new Biom(BiomID.Standart, ID.GrassGround, ID.Dirt, ID.Stone, ID.WoodPlanks, ID.Chest);
        Biom beach = new Biom(BiomID.Beach, ID.Sand, ID.SandStone, ID.Stone, ID.WoodPlanks, ID.Chest);
        Biom[] bioms;

        //------------------------------------
        ResourceItem[] resourcesItemsUpGround = {
            new ResourceItem( ID.Copper, 0.5f),
                new ResourceItem(ID.Tin, 0.5f)
        };

        ResourceItem[] resourcesItemsUp = {
            new ResourceItem(ID.Iron, 0.3f),
                new ResourceItem(ID.Copper, 0.5f) ,
                    new ResourceItem(ID.Silver, 0.2f)
        };

        ResourceItem[] resourcesItemsMiddle = {
            new ResourceItem(ID.Iron, 0.3f),
                new ResourceItem(ID.Silver, 0.3f),
                    new ResourceItem(ID.Diamond, 0.2f),
                        new ResourceItem(ID.Gold, 0.2f)
        };

        ResourceItem[] resourcesItemsDown = {
            new ResourceItem(ID.Iron, 0.3f),
                new ResourceItem(ID.Diamond, 0.3f),
                    new ResourceItem(ID.Gold, 0.3f),
                        new ResourceItem(ID.Platina, 0.1f)
        };

        int resourcesItemsUpStartPos = 0;
        int resourcesItemsMiddleStartPos = 0;
        int resourcesItemsDownStartPos = 0;

        //------------------------------------

    }

    public enum ID { None, NotSet,
        Water, Lava, Obsidian, StoneHighSolid,
        CloudGround,
        Iron, Silver, Gold, Platina, Tin, Copper, Diamond,
        SmallJar, MediumJar,

        Dirt, GrassGround, Tree, WoodPlanks, Chest, Stone,
        Sand, SandStone,
        Snow, SnowStone, Ice, SnowTree, SnowWoodPlanks, SnowChest,
        JungleGrassGround, JungleTree, JungleChest, JungleStone, JungleBriks,
        AdBriks, AdGround, AdChest,
        DangeBriks,

        Briks
    }

    public enum BiomID
    {
        Jungle, Standart, Snow, Beach, Desert
    }

    public class Biom
    {
        public BiomID biomId;
        public int width;
        public ID ground;
        public ID dirt;
        public ID stone;
        public ID wallRoom;
        public ID chest;

        public Biom(BiomID biomId, ID ground, ID dirt, ID stone, ID wallRoom, ID chest)
        {
            this.biomId = biomId;
            this.ground = ground;
            this.dirt = dirt;
            this.stone = stone;
            this.wallRoom = wallRoom;
            this.chest = chest;
        }
        public Biom(Biom biom, int width)
        {
            this.ground = biom.ground;
            this.dirt = biom.dirt;
            this.stone = biom.stone;
            this.width = width;
        }
    }

    public class ResourceItem
    {
        public ID resourse;
        public float chance;
        public ResourceItem (ID resourse, float chance)
        {
            this.resourse = resourse;
            this.chance = chance;
        }
    }
}
