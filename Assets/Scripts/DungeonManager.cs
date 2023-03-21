using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using static UnityEditor.FilePathAttribute;


/// <summary>
/// todo
/// 
///  - Djikastra mapping
///     - Pick an arbitary starting point
///     - Traverse every tile associating a value with distance from start
///     - High number indicates difficulty?
///
///  - Tidy up seed
///     - If a seed exahausts {MaxItterations} there will be empty walls
///     - Fill with a terminator or create a room until terminated
///
///  - Room parsing
///     - Distinguish tiles that create a room
///     - Rooms allow for interesting options when spawning content - enemies, events etc?
///     - Rooms allow for gated content
///     - A room with 1 enterence can be locked without making a dungeon impossible (providing you don't spawn in it)
///
///  - 
/// </summary>
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

        StartCoroutine(DoSeed());
    }

    IEnumerator DoSeed()
    {
        yield return HandleCreateLoop(() =>
        {
            Debug.Log($"Created dungeon of {_dungeon.Count} tiles");
            TraverseDungeon();
        });
    }

    void SeedDungeon()
    {
        initSeed();
        _tiles = new List<GameObject>();
        _dungeon = new HashSet<Vector2>();

        var baseTile = Instantiate(AllTiles[0], Vector2.zero, Quaternion.Euler(0, 0, 0));

        var tile = baseTile
            .GetComponent<DungeonTile>();

        tile.Location = Vector2.zero;

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

    IEnumerator HandleCreateLoop(System.Action callback)
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
                   // var orientation = DungeonTile.GetOrientationFromEdge(e);
                    var offset = DungeonTile.GetOffsetFromEdge(e);
                    var targetLocation = offset + _currentTile;

                    if (!_dungeon.Contains(targetLocation))
                    {
                        var tileToCreate = GetTile(e, tile.Tags);
                        if (tileToCreate != null)
                        {
                            yield return new WaitForSeconds(0.05f);

                            var go = Instantiate(tileToCreate, targetLocation * 30, Quaternion.Euler(0, 0, 0));
                            var newTile = go.GetComponent<DungeonTile>();

                            newTile.Location = targetLocation;

                            _tiles.Add(go);
                            _dungeon.Add(targetLocation);
                        }
                    }
                }
            }
            
        }

        callback();

        GameObject GetTile(int edgeIndex, string[] tileAttributes)
        {

            var matchingIndex = DungeonTile.GetMatchingEdgeIndex(edgeIndex);
            var candidates = new List<GameObject>();

            foreach (var go in AllTiles)
            {
                var tile = go.GetComponent<DungeonTile>();

                if (tile.Edges[matchingIndex] == 1)
                {
                    if (tile.Tags.Where(f => tileAttributes.Contains(f)).Any())
                    {
                        // similar tags so boost
                        candidates.Add(go);
                        candidates.Add(go);
                        candidates.Add(go);
                        candidates.Add(go);
                    }
                    candidates.Add(go);
                }
            }

            if (candidates.Count > 0)
            {
                //RNG load
                return candidates[_random.Next(candidates.Count)];
            }

            return null;
        }     
    }


    void RecurseTiles(List<DungeonTile> tiles, HashSet<Vector2> visted)
    {
        foreach(var tile in tiles)
        {
            RecurseTile(tile, visted);
        }
    }

    void RecurseTile(DungeonTile tile, HashSet<Vector2> visted)
    {
        visted.Add(tile.Location);

        var neighbors = new List<DungeonTile>();
        for (var i = 0; i < tile.Edges.Length - 1; i++)
        {
            if (tile.Edges[i] == 1)
            {
                var neighborLocation = DungeonTile.GetOffsetFromEdge(i) + tile.Location;
                var neighborExists = _dungeon.Contains(neighborLocation);

                if (neighborExists && !visted.Contains(neighborLocation))//
                {
                    var matchingIndex = DungeonTile.GetMatchingEdgeIndex(i);
                    var neighbor = _tiles
                        .Where(t => t.GetComponent<DungeonTile>().Location == neighborLocation)
                        .FirstOrDefault()
                        .GetComponent<DungeonTile>();

                    if (neighbor.Edges[matchingIndex] == 1)
                    {
                        if (neighbor.DijkstraIndex == 0 || tile.DijkstraIndex < neighbor.DijkstraIndex + 1) //tile.DijkstraIndex < neighbor.DijkstraIndex + 1
                        {
                            neighbor.DijkstraIndex = tile.DijkstraIndex + 1;
                        }

                        neighbor.DebugText = $"{neighbor.DijkstraIndex}";
                        neighbors.Add(neighbor);
                    }
                }
                else
                {
                    continue;
                }
            }
        }
        foreach(var neighbor in neighbors)
        {
            RecurseTile(neighbor, visted);
        }
    }

    void TraverseDungeon()
    {
        var root = _tiles[_random.Next(_tiles.Count)].GetComponent<DungeonTile>();

        root.DebugText = "R";

        var visted = new HashSet<Vector2>();

        RecurseTiles(new List<DungeonTile>() { root }, visted);

    }

}
