using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ash
{
    /// <summary>Original procedural art, with shared materials and combined static geometry.</summary>
    public sealed class SettlementArt : IDisposable
    {
        public static readonly Color Sand = new Color32(112, 104, 83, 255);
        public static readonly Color Concrete = new Color32(133, 132, 117, 255);
        public static readonly Color Rust = new Color32(137, 67, 43, 255);
        public static readonly Color Metal = new Color32(62, 76, 77, 255);
        public static readonly Color Timber = new Color32(112, 82, 55, 255);
        public static readonly Color Amber = new Color32(226, 174, 75, 255);
        public static readonly Color Glass = new Color32(88, 150, 157, 255);
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly List<MeshFilter> staticParts = new List<MeshFilter>();
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly Mesh roof;
        private readonly Mesh rock;

        public SettlementArt()
        {
            roof = MakeMesh(new[] {
                new Vector3(-.5f,0,-.5f), new Vector3(.5f,0,-.5f), new Vector3(0,1,-.5f),
                new Vector3(-.5f,0,.5f), new Vector3(.5f,0,.5f), new Vector3(0,1,.5f)
            }, new[] { 0,2,1, 3,4,5, 0,3,5, 0,5,2, 2,5,4, 2,4,1, 0,1,4, 0,4,3 });
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2 / 7, b = (i + 1) * Mathf.PI * 2 / 7;
                vertices.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                vertices.Add(new Vector3(0, 1, 0));
                vertices.Add(new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)));
                triangles.Add(i*3); triangles.Add(i*3+1); triangles.Add(i*3+2);
            }
            rock = MakeMesh(vertices.ToArray(), triangles.ToArray());
        }
        private Mesh MakeMesh(Vector3[] points, int[] indices)
        {
            // Split face vertices for deliberately flat low-poly normals.
            var vertices = new Vector3[indices.Length]; var triangles = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++) { vertices[i] = points[indices[i]]; triangles[i] = i; }
            var mesh = new Mesh { vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh); return mesh;
        }
        public Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var found)) return found;
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            var shader = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) throw new InvalidOperationException("No supported settlement material shader available.");
            var material = new Material(shader) { color = color, enableInstancing = true };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .12f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
            materials.Add(color, material); return material;
        }
        public Transform Box(Transform parent, string name, Vector3 position, Vector3 scale, Color color, bool stationary = true, PrimitiveType shape = PrimitiveType.Cube)
        {
            var obj = GameObject.CreatePrimitive(shape); obj.name = name;
            var collider = obj.GetComponent<Collider>(); if (collider != null) Release(collider);
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (stationary) staticParts.Add(obj.GetComponent<MeshFilter>());
            return obj.transform;
        }
        public Transform Roof(Transform parent, Vector3 position, Vector3 scale)
            => MeshPart(parent, "Pitched rust roof", position, scale, roof, Rust);
        public Transform Rock(Transform parent, Vector3 position, Vector3 scale)
            => MeshPart(parent, "Faceted stone", position, scale, rock, Concrete);
        private Transform MeshPart(Transform parent, string name, Vector3 position, Vector3 scale, Mesh mesh, Color color)
        {
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            staticParts.Add(obj.GetComponent<MeshFilter>()); return obj.transform;
        }
        public void Combine(Transform root)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var part in staticParts)
            {
                var material = part.GetComponent<Renderer>().sharedMaterial;
                if (!groups.TryGetValue(material, out var group)) groups.Add(material, group = new List<CombineInstance>());
                group.Add(new CombineInstance { mesh = part.sharedMesh, transform = root.worldToLocalMatrix * part.transform.localToWorldMatrix });
                Release(part.gameObject);
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(group.Value.ToArray()); ownedMeshes.Add(mesh);
                var obj = new GameObject("Combined settlement geometry", typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(root, false);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<Renderer>().sharedMaterial = group.Key;
            }
            staticParts.Clear();
        }
        public static void Release(UnityEngine.Object target)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(target); else UnityEngine.Object.DestroyImmediate(target);
        }
        public void Dispose()
        {
            foreach (var material in materials.Values) Release(material);
            foreach (var mesh in ownedMeshes) Release(mesh);
            materials.Clear(); ownedMeshes.Clear();
        }
    }
}
