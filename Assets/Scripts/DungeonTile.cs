using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class DungeonTile : MonoBehaviour
{

    /// <summary>
    /// This tiles connections, all times should have an array of 12 edges
    /// 1 indicates a connection
    /// 0 indicates a wall or blocker
    /// </summary>
    public int[] Edges;

    /// <summary>
    /// Meta data used during dungeon creation
    /// Example: Tiles with similar tags may be more or less likely to spawn next to each other
    /// </summary>
    public string[] Tags;

    /// <summary>
    ///  This tile's relative location within the dungeon
    /// </summary>
    public Vector2 Location;

    /// <summary>
    /// Number determines the amount of tiles between this
    /// and the Dijkstra seed (tile with DijkastraIndex == 0)
    /// </summary>
    public int DijkstraIndex;

    /// <summary>
    /// Flag determines if the tile has no children
    /// </summary>
    public bool IsTerminator;

    public List<GameObject> Props;
    public List<Portal> Portals;
    public string DebugText;

    void OnDrawGizmos()
    {
        Handles.Label(transform.position, DebugText);
    }

    void Awake()
    {
        DisablePortals();
        PopulateProps();
    }

    public void SetSpawn()
    {
        this.DijkstraIndex = 0;

        Portals
            .FirstOrDefault(f => f.Type == PortalType.Spawn)
            ?.gameObject.SetActive(true);
    }

    public void SetExit()
    {
        Portals
            .FirstOrDefault(f => f.Type == PortalType.Exit)
            ?.gameObject.SetActive(true);
    }

    public static Orientation GetOrientationFromEdge(int edgeIndex)
    {
        return edgeIndex switch
        {
            var i when i < 3 => Orientation.Up,
            var i when i >= 3 && i < 6 => Orientation.Right,
            var i when i >= 6 && i < 9 => Orientation.Down,
            var i when i >= 9 => Orientation.Left,
            _ => Orientation.Up,
        };
    }

    public static Vector2 GetOffsetFromEdge(int edgeIndex)
    {
        return GetOrientationFromEdge(edgeIndex) switch
        {
            var i when i == Orientation.Up => new Vector2(0, 1),
            var i when i == Orientation.Down => new Vector2(0, -1),

            var i when i == Orientation.Right => new Vector2(1, 0),
            var i when i == Orientation.Left => new Vector2(-1, 0),

            _ => Vector2.zero,
        };
    }

    public static int GetMatchingEdgeIndex(int edgeIndex)
    {
        var matchingEdgeIndex = (edgeIndex % 3) switch
        {
            0 => 8 + edgeIndex,
            1 => 6 + edgeIndex,
            2 => 4 + edgeIndex,
            //Noop
            _ => 0,
        };
        if (matchingEdgeIndex > 11)
            matchingEdgeIndex -= 12;
        return matchingEdgeIndex;
    }

    public static bool TileIsPathable(DungeonTile tile)
    {
        return tile.Edges.Any(e => e == 1);
    }

    // todo make this more dynamic? currently just 80% chance to show any prop
    private void PopulateProps()
    {
        foreach (var go in Props ?? Enumerable.Empty<GameObject>())
        {
            go.SetActive(Random.Range(0.0f, 1.0f) > 0.80);
        }
    }

    private void DisablePortals()
    {
        // disable portals
        Portals.ForEach(f => f.gameObject.SetActive(false));
    }
}

public enum Orientation
{
    Up,
    Right,
    Down,
    Left
}