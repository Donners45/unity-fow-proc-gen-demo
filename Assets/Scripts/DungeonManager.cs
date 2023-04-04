using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// todo
/// 
///  - Djikastra mapping (Not stable)
///     - Pick an arbitary starting point
///     - Traverse every tile associating a value with distance from start
///     - High number indicates difficulty?
///     - Lot's of rooms causes the current algo to blow up.
///
///  - Tidy up seed
///     - If a seed exahausts {MaxItterations} there will be empty walls
///     - Fill with a terminator or create a room until terminated
///     - Can do this as part of Dijkstra mapping, I'm already traversing every tile.
///
///  - Room parsing
///     - Distinguish tiles that create a room
///     - Rooms allow for interesting options when spawning content - enemies, events etc?
///     - Rooms allow for gated content
///     - A room with 1 enterence can be locked without making a dungeon impossible (providing you don't spawn in it)
///
///     -- Good seed for testing rooms that can block each other 752157708 | 100
///
///     197732605 | 100 - long boi seed. 
///  - 
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
    private Vector2 _currentTile;
    private int _maxDjikastraIndex = 0;
    private System.Random _random;

    private UniqueTree<DungeonTile> _dungeonTree;

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
            TraverseDungeon();
            Debug.Log($"Created dungeon of {_dungeon.Count} tiles, with {_maxDjikastraIndex} max DK index");

            //var i = 0;
            //foreach(var t in _dungeonTree.TraverseBottomUpBFS(_tiles[_random.Next(_tiles.Count)].GetComponent<DungeonTile>(), 10))
            //{
                
            //    t.DebugText = $"t {++i}";
            //}

        });
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

    IEnumerator HandleCreateLoop(System.Action callback)
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

                            yield return new WaitForSeconds(0.05f);

                            CreateTile(tileToCreate, targetLocation);
                        }
                    }
                }
                iteration++;
            }            
        }

        callback();    
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

        root.DebugText = "R";

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
            // attempt at adding navigatable tiles to a tree
            // this would allow traversal from any given point
            //_dungeonTree.Add(children, tile);
        }
        if (children.Any())
        {
            RecurseTiles(children, visted);
        }
    }

    /// <summary>
    /// todo
    /// Not stable with over 250 iteration maps
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
                    // todo this is slow as balls
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
                    // todo tidy this up
                    // creates a terminator block when no neighbor is found
                    //
                    // idea - terminators could be smarter to create a smaller wall where needed? 
                    //
                    // idea - terminators could be used as teleports (doors) to prefabbed rooms
                    //          gets around the polyfilling problems!

                    //var terminatorToUse = GetTile(i, null, TerminatorTiles);

                    //if (terminatorToUse == null)
                    //{
                    //    terminatorToUse = TerminatorTiles[0];
                    //}

                    //var go = Instantiate(terminatorToUse, neighborLocation * TileSize, Quaternion.Euler(0, 0, 0));
                    //var newTile = go.GetComponent<DungeonTile>();
                    //newTile.Location = neighborLocation;
                    //_tiles.Add(go);
                    //_dungeon.Add(neighborLocation);


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
}
