using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace Assets.Generation
{
    class MapDisplay : MonoBehaviour
    {
        public GameObject player;
        private Tilemap tileMap;

        private ID[,] map;
        private int widthMap, heightMap;
        private UnityEngine.Vector3Int spawn;

        private int widthDisplay = 60, heightDisplay = 40;
     

        private void Awake()
        {           
            tileMap = gameObject.GetComponent<Tilemap>();

            MapGenerator mapGenerate = new MapGenerator();
            map = mapGenerate.GenerateMap();
            widthMap = map.GetLength(0);
            heightMap = map.GetLength(1);
            tileMap.size = new Vector3Int(widthMap, heightMap, 1);
            spawn = mapGenerate.spawn;


            player.transform.position = new Vector3(spawn.x, spawn.y, 1);
        }

        private void Update()
        {
            createFragmentMap((int)(player.transform.position.x / 0.32f), (int)(player.transform.position.y / 0.32f));
        }

        private void createFragmentMap(int startX, int startY)
        {
            //int count = 0;
            for (int y = startY - heightDisplay; y < startY + heightDisplay && y >= 0 && y < map.GetLength(1); y++)
            {
                for (int x = startX - widthDisplay; x < startX + widthDisplay && x >= 0 && x < map.GetLength(0); x++)
                {
                    if (tileMap.GetTile(new Vector3Int(x, y, 0)) == null)
                    {
                        Tile tile = Resources.Load<Tile>("Map/TileSet/Tiles/" + map[x, heightMap - 1 - y].ToString());
                        tileMap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                }
            }
          //  Debug.Log(count + " ");
        }
    }
}
