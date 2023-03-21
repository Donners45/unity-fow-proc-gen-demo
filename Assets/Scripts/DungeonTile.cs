using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DungeonTile : MonoBehaviour
{

    public int[] Edges;
    public string[] Tags;
    public Vector2 Location;

    public int DijkstraIndex;


    /// <summary>
    /// Only used for debugging purposes
    /// </summary>
    public string DebugText;
    void OnDrawGizmos()
    {
        Handles.Label(transform.position, DebugText);
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
}

// 0 0 0 0 0 0 0 0 0 0 0 0 

public enum Orientation
{
    Up,
    Right,
    Down,
    Left
}