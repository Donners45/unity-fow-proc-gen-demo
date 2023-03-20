using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonTile : MonoBehaviour
{

    public int[] Edges;
    public string[] Tags;
    public Vector2 Location;

    static Orientation GetOrientationFromEdge(int edgeIndex)
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
            var i when i == Orientation.Up => new Vector2(0, -1),
            var i when i == Orientation.Down => new Vector2(0, 1),

            var i when i == Orientation.Right => new Vector2(1, 0),
            var i when i == Orientation.Left => new Vector2(-1, 0),

            _ => Vector2.zero,
        };
    }
}


public enum Orientation
{
    Up,
    Right,
    Down,
    Left
}