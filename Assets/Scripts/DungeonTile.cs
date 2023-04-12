using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class DungeonTile : MonoBehaviour
{

    public int[] Edges;
    public string[] Tags;
    public Vector2 Location;
    public int DijkstraIndex;

    public List<GameObject> Props;

    public string DebugText;
    void OnDrawGizmos()
    {
        Handles.Label(transform.position, DebugText);
    }

    void Start()
    {
        PopulateProps();

        // disable portals
        transform.Find("Portals")?
            .gameObject.SetActive(false);
    }

    public void SetSpawn()
    {
        this.DijkstraIndex = 0;
        transform.Find("Portals")?
            .gameObject.SetActive(true);
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
}

public enum Orientation
{
    Up,
    Right,
    Down,
    Left
}