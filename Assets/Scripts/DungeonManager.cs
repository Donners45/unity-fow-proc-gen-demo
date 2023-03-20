using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    public int MaxItterations;
    public GameObject[] AllTiles;
    public int Seed;

    private List<GameObject> _tiles;
    private HashSet<Vector2> _dungeon;
    private Vector2 _currentTile;

    private System.Random _random;

    // Start is called before the first frame update
    void Start()
    {
        SeedDungeon();

        StartCoroutine(HandleCreateLoop());
    }

    void SeedDungeon()
    {
        initSeed();
        _tiles = new List<GameObject>();
        _dungeon = new HashSet<Vector2>();

        var baseTile = Instantiate(AllTiles[0], Vector2.zero, Quaternion.Euler(0, 0, 0));

        baseTile
            .GetComponent<DungeonTile>()
            .Location = Vector2.zero;

        _tiles.Add(baseTile);

        _dungeon.Add(Vector2.zero);
        _currentTile = Vector2.zero;

        void initSeed()
        {
            int seed;
            if (Seed != 0)
            {
                seed = Seed;
            }
            else
            {
                seed = Random.Range(int.MinValue, int.MaxValue);
            }
            _random = new System.Random(seed);
            Debug.Log($"Creating dungeon with seed: {seed}");
        }
    }

    IEnumerator HandleCreateLoop()
    {
        for (var i = 0; i < MaxItterations; i++)
        {
            if (_tiles.Count - 1 < i)
                break;

            var tile = _tiles[i].GetComponent<DungeonTile>();
            _currentTile = tile.Location;

            for (var e = 0; e <= tile.Edges.Length - 1; e++)
            {
                if (tile.Edges[e] == 1)
                {
                    var offset = DungeonTile.GetOffsetFromEdge(e);
                    var targetLocation = offset + _currentTile;

                    if (!_dungeon.Contains(targetLocation))
                    {

                        yield return new WaitForSeconds(1);

                        var go = Instantiate(AllTiles[2], targetLocation * 30, Quaternion.Euler(0, 0, 0));
                        go
                            .GetComponent<DungeonTile>()
                            .Location = targetLocation;
                        _tiles.Add(go);
                        _dungeon.Add(targetLocation);
                        
                    }
                }
            }

        }

        
        
    }
}
