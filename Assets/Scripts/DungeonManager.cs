using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// todo
/// 
///  - Djikastra mapping
///     - Being able to recalculate based on a selected tile could be useful? 
///
///  - Room parsing
///     - Easier to create bespoke rooms as prefabs
///     - RNG chance for a terminator tile to have a gated portal to prefab
///     - Could spend resources to open prefabbed portal
///
///  - Portals
///     - Spawns and targets need creating
///     - 
///
///  - Minimum Requirements
///     - If a seed doesn't yeild XYZ then re-roll?
///
/// 
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public int MaxIterations;
    public GameObject[] AllTiles;
    public GameObject[] TerminatorTiles;
    public int Seed;
    public  int TileSize = 30;

    private List<GameObject> _tiles;
    private HashSet<Vector2> _dungeon;

    private DungeonTile SpawnReference;
    private List<DungeonTile> TerminatorReferences;

    private Vector2 _currentTile;
    private int _maxDjikastraIndex = 0;
    private System.Random _random;

    // Start is called before the first frame update
    void Start()
    {
        CreateDungeon();

        if (!ValidateDungeon())
        {
            // todo recurse or loop until this is happy?
            //CreateDungeon();

        }

        Debug.Log($"Created dungeon of {_dungeon.Count} tiles, with {_maxDjikastraIndex} max DK index");
    }

    /// <summary>
    /// Seeds a new dungeon
    /// Creates tiles
    /// Traverses tiles to populate run time data
    /// </summary>
    void CreateDungeon()
    {
        SeedDungeon();
        HandleCreateLoop();
        TraverseDungeon();
        SetPortals();
    }

    void SeedDungeon()
    {
        initSeed();
        _tiles = new List<GameObject>();
        _dungeon = new HashSet<Vector2>();

        CreateTile(AllTiles[0], Vector2.zero);

        _currentTile = Vector2.zero;

        void initSeed()
        {
            int seed;

           seed = Seed != 0 ?
                Seed :
                Random.Range(int.MinValue, int.MaxValue);

            _random = new System.Random(seed);
            Debug.Log($"Creating dungeon with seed: {seed}");
        }
    }

    void HandleCreateLoop()
    {
        var iteration = 0;

        // todo this is a bit hacky
        // need to run the tile loop once more after max iterations
        // to create the final terminators, this isn't inefficient but it's difficult to read..
        while (iteration < MaxIterations + 1)
        {
            // for each tile
            // this array grows as it loops, can't foreach it. 
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles.Count - 1 < i)
                    break;

                var tile = _tiles[i].GetComponent<DungeonTile>();
                _currentTile = tile.Location;

                // for each edge in current tile
                var createdChild = false;
                for (var e = 0; e <= tile.Edges.Length - 1; e++)
                {
                    if (tile.Edges[e] == 1)
                    {
                        var targetLocation =
                            DungeonTile.GetOffsetFromEdge(e) + _currentTile;

                        if (!_dungeon.Contains(targetLocation))
                        {
                            var tileSet = iteration < MaxIterations
                                ? AllTiles : TerminatorTiles;

                            var tileToCreate = GetTile(e, tile.Tags, tileSet)
                                ?? TerminatorTiles[0];

                            CreateTile(tileToCreate, targetLocation);
                            createdChild = true;
                        }
                    }
                }
                if (createdChild == false) tile.IsTerminator = true;
                iteration++;
            }            
        }
    }

    GameObject GetTile(int edgeIndex, string[] tileAttributes, GameObject[] tileSet)
    {

        var matchingIndex = DungeonTile.GetMatchingEdgeIndex(edgeIndex);
        var candidates = new List<GameObject>();

        foreach (var go in tileSet)
        {
            var tile = go.GetComponent<DungeonTile>();

            if (tile.Edges[matchingIndex] == 1)
            {
                if (tile.Tags.Where(f => tileAttributes.Contains(f)).Any())
                {
                    // todo make boost logic a bit more rigid.
                    // this is doesn't seem efficient.
                    candidates.Add(go);
                    candidates.Add(go);
                    candidates.Add(go);
                }
                candidates.Add(go);
            }
        }

        return candidates.Count > 0 ?
            candidates[_random.Next(candidates.Count)] :
            null;
    }

    /// <summary>
    /// From a random point
    /// walk every path in the dungeon
    /// </summary>
    void TraverseDungeon()
    {
        var root = _tiles[_random.Next(_tiles.Count)].GetComponent<DungeonTile>();

        if (!DungeonTile.TileIsPathable(root))
        {
            TraverseDungeon();
            return;
        }

        root.SetSpawn();
        var visted = new HashSet<Vector2>();
        Debug.Log("Starting recurse");
        RecurseTiles(new List<DungeonTile>() { root }, visted);
        Debug.Log("Finished recurse");
    }


    void RecurseTiles(List<DungeonTile> tiles, HashSet<Vector2> visted)
    {
        List<DungeonTile> children = new List<DungeonTile>();
        foreach(var tile in tiles)
        {
            children.AddRange(RecurseTile(tile, visted));
        }
        if (children.Any())
        {
            RecurseTiles(children, visted);
        }
    }

    /// <summary>
    /// todo
    /// ideas:
    ///     store dungeon tiles instead of GO?
    ///     searching for GO with location can't be fast, if I can find a way link the hash set with the GO would be faster
    ///
    /// Walks every child of provided tile
    /// Sets a Dijkstra index for each child
    /// Returns a list of children
    /// 
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="visted"></param>
    List<DungeonTile> RecurseTile(DungeonTile tile, HashSet<Vector2> visted)
    {
        visted.Add(tile.Location);

        var neighbors = new List<DungeonTile>();
        for (var i = 0; i < tile.Edges.Length - 1; i++)
        {
            if (tile.Edges[i] == 1)
            {
                var neighborLocation = DungeonTile.GetOffsetFromEdge(i) + tile.Location;
                var neighborExists = _dungeon.Contains(neighborLocation);

                if (neighborExists)
                {
                    var matchingIndex = DungeonTile.GetMatchingEdgeIndex(i);

                    // todo this probably isn't very efficient
                    var neighbor = _tiles
                        .Where(t => t.GetComponent<DungeonTile>().Location == neighborLocation)
                        .FirstOrDefault()
                        .GetComponent<DungeonTile>();

                    if (neighbor.Edges[matchingIndex] == 1)
                    {

                        if (neighbor.DijkstraIndex > tile.DijkstraIndex + 1 ||
                            (neighbor.DijkstraIndex == 0 && !visted.Contains(neighbor.Location)))
                        {
                            neighbor.DijkstraIndex = tile.DijkstraIndex + 1;
                            neighbor.DebugText = $"{neighbor.DijkstraIndex}";
                            _maxDjikastraIndex = Mathf.Max(_maxDjikastraIndex, neighbor.DijkstraIndex);
                        }

                        // only add neighbor to list if it's not been visted
                        // this prevents stack over flows
                        if (!visted.Contains(neighbor.Location))
                        {
                            neighbors.Add(neighbor);
                        }

                    }
                }
                else
                {
                    continue;
                }
            }
        }
        return neighbors;
    }

    /// <summary>
    /// Creates a tile at the specified location
    /// Does not check placement validity
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="location"></param>
    void CreateTile(GameObject tile, Vector2 location)
    {
        var newGo = Instantiate(tile, location * TileSize, Quaternion.Euler(0, 0, 0));
        newGo.GetComponent<DungeonTile>()
            .Location = location;

        _tiles.Add(newGo);
        _dungeon.Add(location);
    }

    void SetPortals()
    {
        var tiles = _tiles.Select(t => t.GetComponent<DungeonTile>());

        tiles = tiles
            .Where(t => t.IsTerminator)
            .OrderBy(t => t.DijkstraIndex);

        foreach (var tile in tiles)
        {
            if (tile.IsTerminator)
            {
                tile.SetExit();
            }
        }
    }

    /// <summary>
    /// todo make validate more dynamic
    ///     - magic ints at the moment, this should probably be more dynamic based on a difficulty scale
    /// </summary>
    /// <returns></returns>
    bool ValidateDungeon()
    {
        var tiles = _tiles.Select(t => t.GetComponent<DungeonTile>());

        var terminatorCount = tiles.Count(t => t.IsTerminator);
        if (terminatorCount < 5)
        {
            Debug.Log($"Not enough terminators {terminatorCount} is less than 5");
            return false;
        }

        var tileCount = tiles.Count();
        if (tileCount < 20)
        {
            Debug.Log($"Not enough tiles {tileCount} is less than 20");
            return false;
        }

        // todo not working
        // still seeing all portals as active?
        // possibly a delay timing issue or a reference/value issue with the list
        // portals appear to work as expected in the editor
        var spawnCount = tiles
            .Select(t => t.Portals.Count(p => p.Type == PortalType.Spawn && p.gameObject.activeSelf))
            .Count();

        if (spawnCount != 1)
        {
            Debug.Log($"spawn portals active {spawnCount} {terminatorCount}");
            // return false once big above is fixed
            return true;
        }

        return true;
    }
}
