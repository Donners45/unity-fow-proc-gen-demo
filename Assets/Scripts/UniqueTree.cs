using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class ripped straight out of CGPT
/// The idea is to store the navigatable path in a tree allowing easy access to a tiles navigatable neighbors
///
/// There are some quirks with this, I think it's related to BFS vs DFS which is resulting in weird ordering of the tiles
/// Needs some more debugging.
/// </summary>
/// <typeparam name="T"></typeparam>
public class UniqueTree<T>
{
    private readonly HashSet<T> elements;
    private readonly Dictionary<T, HashSet<T>> children;

    public UniqueTree()
    {
        elements = new HashSet<T>();
        children = new Dictionary<T, HashSet<T>>();
    }

    public void Add(IEnumerable<T> element, T parent = default)
    {
        foreach(var i in element)
        {
            Add(i, parent);
        }
    }

    public void Add(T element, T parent = default)
    {
        if (!elements.Contains(element))
        {
            elements.Add(element);
            children.Add(element, new HashSet<T>());
        }

        if (!EqualityComparer<T>.Default.Equals(parent, default))
        {
            if (!elements.Contains(parent))
            {
                elements.Add(parent);
                children.Add(parent, new HashSet<T>());
            }

            children[parent].Add(element);
        }
    }

    public IEnumerable<T> Traverse(T start = default)
    {
        var visited = new HashSet<T>();
        var queue = new Queue<T>();

        if (EqualityComparer<T>.Default.Equals(start, default))
        {
            queue.Enqueue(elements.First());
        }
        else
        {
            queue.Enqueue(start);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (visited.Contains(current))
            {
                continue;
            }

            visited.Add(current);
            yield return current;

            if (children.TryGetValue(current, out var childSet))
            {
                foreach (var child in childSet)
                {
                    queue.Enqueue(child);
                }
            }
        }
    }

    public IEnumerable<T> TraverseBottomUpBFS(T start, int iterations)
    {
        var visited = new HashSet<T>();
        var queue = new Queue<T>();
        var level = new Dictionary<T, int>();

        queue.Enqueue(start);
        level[start] = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visited.Add(current);

            if (level[current] >= iterations)
            {
                break;
            }

            if (children.TryGetValue(current, out var childSet))
            {
                foreach (var child in childSet)
                {
                    if (!visited.Contains(child))
                    {
                        queue.Enqueue(child);
                        level[child] = level[current] + 1;
                    }
                }
            }
        }

        return visited;
    }

}
