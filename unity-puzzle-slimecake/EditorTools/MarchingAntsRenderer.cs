#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace SlimeCake.LevelEditor.Selection
{
    /// <summary>
    /// Renders selection outlines using pooled LineRenderers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MarchingAntsRenderer : MonoBehaviour
    {
        [SerializeField] private Material marchingAntsMaterial = null!;
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private int initialPoolSize = 64;

        private readonly List<LineRenderer> pool = new();
        private EditorSelection? boundSelection;
        private Vector2Int tempOffset;

        public void Bind(EditorSelection selection)
        {
            if (this.boundSelection != null) this.boundSelection.Changed -= this.OnSelectionChanged;
            this.boundSelection = selection;
            this.boundSelection.Changed += this.OnSelectionChanged;
            this.Rebuild();
        }

        // Moves outlines using transform offset during drag.
        public void SetTemporaryOffset(Vector2Int offset)
        {
            if (this.tempOffset == offset) return;
            this.tempOffset = offset;
            this.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }

        private void Awake()
        {
            if (this.marchingAntsMaterial == null)
                Debug.LogError("MarchingAntsRenderer material not assigned.", this);

            for (int i = 0; i < this.initialPoolSize; i++) this.SpawnLineRenderer();
        }

        private void OnDestroy()
        {
            if (this.boundSelection != null) this.boundSelection.Changed -= this.OnSelectionChanged;
        }

        private void OnSelectionChanged() => this.Rebuild();

        private void Rebuild()
        {
            if (this.boundSelection == null)
            {
                this.HideAll();
                return;
            }

            var edges = ComputeBoundaryEdges(this.boundSelection);
            this.EnsurePoolSize(edges.Count);

            for (int i = 0; i < edges.Count; i++)
            {
                var lineRenderer = this.pool[i];
                lineRenderer.SetPosition(0, edges[i].start);
                lineRenderer.SetPosition(1, edges[i].end);
                lineRenderer.enabled = true;
            }
            for (int i = edges.Count; i < this.pool.Count; i++)
            {
                this.pool[i].enabled = false;
            }
        }

        private void HideAll()
        {
            for (int i = 0; i < this.pool.Count; i++) this.pool[i].enabled = false;
        }

        // Compute local edges to prevent allocations.
        private static List<Edge> ComputeBoundaryEdges(EditorSelection selection)
        {
            var result = new List<Edge>();
            foreach (var cell in selection.Cells)
            {
                // Add edge if neighbor is not selected.
                if (!selection.Contains(new Vector2Int(cell.x, cell.y + 1)))
                {
                    result.Add(new Edge(
                        new Vector3(cell.x - 0.5f, cell.y + 0.5f, 0f),
                        new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f)));
                }
                if (!selection.Contains(new Vector2Int(cell.x, cell.y - 1)))
                {
                    result.Add(new Edge(
                        new Vector3(cell.x - 0.5f, cell.y - 0.5f, 0f),
                        new Vector3(cell.x + 0.5f, cell.y - 0.5f, 0f)));
                }
                if (!selection.Contains(new Vector2Int(cell.x - 1, cell.y)))
                {
                    result.Add(new Edge(
                        new Vector3(cell.x - 0.5f, cell.y - 0.5f, 0f),
                        new Vector3(cell.x - 0.5f, cell.y + 0.5f, 0f)));
                }
                if (!selection.Contains(new Vector2Int(cell.x + 1, cell.y)))
                {
                    result.Add(new Edge(
                        new Vector3(cell.x + 0.5f, cell.y - 0.5f, 0f),
                        new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f)));
                }
            }
            return result;
        }

        private void EnsurePoolSize(int count)
        {
            while (this.pool.Count < count) this.SpawnLineRenderer();
        }

        private void SpawnLineRenderer()
        {
            var go = new GameObject($"AntsEdge_{this.pool.Count}");
            go.transform.SetParent(this.transform, worldPositionStays: false);

            var lineRenderer = go.AddComponent<LineRenderer>();
            // Use local space to prevent rebuilding geometry.
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = false;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = this.lineWidth;
            lineRenderer.endWidth = this.lineWidth;
            lineRenderer.numCapVertices = 0;
            lineRenderer.numCornerVertices = 0;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.material = this.marchingAntsMaterial;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.enabled = false;

            this.pool.Add(lineRenderer);
        }

        private readonly struct Edge
        {
            public readonly Vector3 start;
            public readonly Vector3 end;
            public Edge(Vector3 s, Vector3 e) { this.start = s; this.end = e; }
        }
    }
}
