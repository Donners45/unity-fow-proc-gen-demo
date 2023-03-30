using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using static UnityEditor.FilePathAttribute;


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
///     - Define a room?
///
///  - 
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public int MaxItterations;
    public GameObject[] AllTiles;
    public GameObject TerminatorTile;
    public int Seed;
    public  int TileSize = 30;

    private List<GameObject> _tiles;
    private HashSet<Vector2> _dungeon;
    private Vector2 _currentTile;
    private int _maxDjikastraIndex = 0;
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
            TraverseDungeon();
            Debug.Log($"Created dungeon of {_dungeon.Count} tiles, with {_maxDjikastraIndex} max DK index");
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
                    var offset = DungeonTile.GetOffsetFromEdge(e);
                    var targetLocation = offset + _currentTile;

                    if (!_dungeon.Contains(targetLocation))
                    {
                        var tileToCreate = GetTile(e, tile.Tags);
                        if (tileToCreate != null)
                        {
                            yield return new WaitForSeconds(0.05f);

                            var go = Instantiate(tileToCreate, targetLocation * TileSize, Quaternion.Euler(0, 0, 0));
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
                        // todo make boost logic a bit more rigid.
                        // this is doesn't seem efficient.
                        candidates.Add(go);
                        candidates.Add(go);
                        candidates.Add(go);
                    }
                    candidates.Add(go);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[_random.Next(candidates.Count)];
            }

            return null;
        }     
    }

    /// <summary>
    /// From a random point
    /// walk every path in the dungeon
    /// </summary>
    void TraverseDungeon()
    {
        var root = _tiles[_random.Next(_tiles.Count)].GetComponent<DungeonTile>();

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
        }
        if (children.Any())
        {
            RecurseTiles(children, visted);
        }
    }

    /// <summary>
    /// Buggy seeds
    /// 39921746 | 30 - fixed
    /// 923075182 | 100 - fixed
    ///
    /// todo
    /// Not stable with over 250 itteration maps
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
                    //var go = Instantiate(TerminatorTile, neighborLocation * TileSize, Quaternion.Euler(0, 0, 0));
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
}
