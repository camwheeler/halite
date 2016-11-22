using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AStar
{
    // Immutable stack
    public class Path<Node> : IEnumerable<Node>
    {
        public Node LastStep { get; private set; }
        public Path<Node> PreviousSteps { get; private set; }
        public double TotalCost { get; private set; }

        private Path(Node lastStep, Path<Node> previousSteps, double totalCost) {
            LastStep = lastStep;
            PreviousSteps = previousSteps;
            TotalCost = totalCost;
        }

        public Path(Node start) : this(start, null, 0) { }
        public Path<Node> AddStep(Node step, double stepCost) { return new Path<Node>(step, this, TotalCost + stepCost); }

        public IEnumerator<Node> GetEnumerator() {
            for (var p = this; p != null; p = p.PreviousSteps)
                yield return p.LastStep;
        }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }

    class PriorityQueue<P, V>
    {
        private SortedDictionary<P, Queue<V>> list = new SortedDictionary<P, Queue<V>>();

        public void Enqueue(P priority, V value) {
            Queue<V> q;
            if (!list.TryGetValue(priority, out q)) {
                q = new Queue<V>();
                list.Add(priority, q);
            }
            q.Enqueue(value);
        }

        public V Dequeue() {
            // will throw if there isn’t any first element!
            var pair = list.First();
            var v = pair.Value.Dequeue();
            if (pair.Value.Count == 0) // nothing left of the top priority.
                list.Remove(pair.Key);
            return v;
        }

        public bool IsEmpty {
            get { return !list.Any(); }
        }
    }

    public interface IHaveUncontestedNeighbors<N>
    {
        IEnumerable<N> OpenNeighbors { get; }
    }

    public static Path<T> FindPath<T>(T start, T destination, Func<T, T, double> distance, Func<T, double> estimate) where T : IHaveUncontestedNeighbors<T> {
        var closed = new HashSet<T>();
        var queue = new PriorityQueue<double, Path<T>>();
        queue.Enqueue(0, new Path<T>(start));
        while (!queue.IsEmpty) {
            var path = queue.Dequeue();
            if (closed.Contains(path.LastStep))
                continue;
            if (path.LastStep.Equals(destination))
                return path;
            closed.Add(path.LastStep);
            foreach (var n in path.LastStep.OpenNeighbors) {
                var d = distance(path.LastStep, n);
                var newPath = path.AddStep(n, d);
                queue.Enqueue(newPath.TotalCost + estimate(n), newPath);
            }
        }
        return null;
    }
}