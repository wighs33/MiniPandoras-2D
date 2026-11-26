using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 그림에서 되돌린 모형 묶음 하나(Tools/PixelGen/picture_model.py가 굽는 models.png 아틀라스와 models.json).
    /// 면의 꼭짓점은 발밑에서 잰 (x, 남쪽 줄 g, 높이 h) 점이고 아틀라스의 점 자리(u, v)를 짚는다.
    /// 높이는 세운 그림처럼 뒤로 젖혀 올린다(<see cref="Upright.Above"/>). 그래서 앞면은 그림을 세운 판과 같은 자리에 서고 윗면·지붕은 뒤로 눕는다.
    /// 메시는 모형마다, 그림과 재질은 묶음마다 하나다. 맵의 모형은 <see cref="UprightModels"/>, 연출의 큰 모형은 <see cref="StageModelImporter"/>가 쓴다.
    /// </summary>
    internal sealed class ModelSheet
    {
        private const string FileName = "models";

        private readonly string name;
        private readonly string assetFolder;
        private readonly Vector2 atlasSize;
        private readonly Dictionary<string, ModelData> models = new Dictionary<string, ModelData>();
        private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        private ModelSheet(string name, string assetFolder, ModelList list, Material material)
        {
            this.name = name;
            this.assetFolder = assetFolder;
            Material = material;
            if (list == null)
            {
                return;
            }

            atlasSize = new Vector2(list.size[0], list.size[1]);
            foreach (ModelData model in list.models)
            {
                models.Add(model.name, model);
            }
        }

        public Material Material { get; }

        public IEnumerable<string> Names => models.Keys;

        /// <summary>folder의 모형 묶음. 모형이 없으면 비어 있다. 메시와 재질은 assetFolder에 둔다.</summary>
        public static ModelSheet Load(string folder, string assetFolder)
        {
            string name = Path.GetFileName(folder);
            string path = $"{folder}/{FileName}";
            if (!File.Exists(path + ".json"))
            {
                return new ModelSheet(name, assetFolder, null, null);
            }

            if (!AssetDatabase.IsValidFolder(assetFolder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(assetFolder).Replace('\\', '/'), Path.GetFileName(assetFolder));
            }

            Texture2D atlas = FieldSprites.ImportPicture(path + ".png");
            Material material = AssetFiles.Save(FieldSprites.ModelMaterial(atlas, name), $"{assetFolder}/{name}.mat");
            return new ModelSheet(name, assetFolder, WebGameImporter.ReadJson<ModelList>(path + ".json"), material);
        }

        public bool Has(string model)
        {
            return !string.IsNullOrEmpty(model) && models.ContainsKey(model);
        }

        /// <summary>모형의 발밑을 세운 그림(왼쪽 위 기준 점)에서 잰 자리.</summary>
        public Vector2 PivotOf(string model)
        {
            return new Vector2(models[model].pivot[0], models[model].pivot[1]);
        }

        /// <summary>같은 모형을 여러 자리에 세우면 메시를 함께 쓴다.</summary>
        public Mesh MeshOf(string model)
        {
            if (!meshes.TryGetValue(model, out Mesh mesh))
            {
                string assetName = $"{name}_{model}";
                mesh = AssetFiles.Save(Build(models[model], assetName), $"{assetFolder}/{assetName}.asset");
                meshes.Add(model, mesh);
            }

            return mesh;
        }

        private Mesh Build(ModelData model, string meshName)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            foreach (FaceData face in model.faces)
            {
                int start = vertices.Count;
                int corners = face.points.Length / 3;
                byte shade = (byte)Mathf.RoundToInt(Mathf.Clamp01(face.shade) * 255f);
                for (int i = 0; i < corners; i++)
                {
                    var ground = new Vector2(face.points[i * 3], -face.points[i * 3 + 1]) / MapRelief.DotsPerTile;
                    vertices.Add(Upright.Above(ground, face.points[i * 3 + 2] / MapRelief.DotsPerTile));
                    uvs.Add(new Vector2(face.uv[i * 2] / atlasSize.x, 1f - face.uv[i * 2 + 1] / atlasSize.y));
                    colors.Add(new Color32(shade, shade, shade, 255));
                }

                // 면의 방향. 해를 마주 보는 만큼 밝기가 달라진다(셰이더 _FACE_LIT).
                Vector3 normal = Vector3.Cross(vertices[start + 1] - vertices[start], vertices[start + 2] - vertices[start]).normalized;
                for (int i = 0; i < corners; i++)
                {
                    normals.Add(normal);
                }

                // 면은 볼록한 세모·네모라 첫 꼭짓점에서 부채꼴로 나눈다.
                for (int i = 1; i + 1 < corners; i++)
                {
                    triangles.Add(start);
                    triangles.Add(start + i);
                    triangles.Add(start + i + 1);
                }
            }

            if (model.smooth)
            {
                JoinNormals(vertices, normals);
            }

            var mesh = new Mesh { name = meshName };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>같은 자리의 꼭짓점은 만나는 면들의 방향을 고르게 섞는다(부풀린 부조가 둥글게 비친다).</summary>
        private static void JoinNormals(List<Vector3> vertices, List<Vector3> normals)
        {
            var sums = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < vertices.Count; i++)
            {
                sums.TryGetValue(vertices[i], out Vector3 sum);
                sums[vertices[i]] = sum + normals[i];
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                normals[i] = sums[vertices[i]].normalized;
            }
        }

        [Serializable]
        private sealed class ModelList
        {
            public float[] size;
            public ModelData[] models;
        }

        [Serializable]
        private sealed class ModelData
        {
            public string name;
            public float[] pivot;
            public bool smooth;
            public FaceData[] faces;
        }

        [Serializable]
        private sealed class FaceData
        {
            public float[] points;
            public float[] uv;
            public float shade;
        }
    }
}
