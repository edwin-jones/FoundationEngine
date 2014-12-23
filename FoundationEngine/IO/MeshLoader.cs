using FoundationEngine.Renderer;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace FoundationEngine.IO
{
    //class BablyonMesh
    //{
    //    public Boolean autoClear { get; set; }
    //    public Color clearColor;
    //    public Color ambientColor;
    //}

    static class MeshLoader
    {
        // Loading the JSON file in an asynchronous manner
        public static Mesh[] LoadJSONFile(string fileName)
        {
            var meshes = new List<Mesh>();
            var data = File.ReadAllText(fileName);
            dynamic jsonObject = new JavaScriptSerializer().DeserializeObject(data);

            var tempArray = jsonObject["meshes"] as dynamic[];

            for (var meshIndex = 0; meshIndex < tempArray.Length; meshIndex++)
            {
                var verticesArray = tempArray[meshIndex]["vertices"];
                // Faces
                var indicesArray = tempArray[meshIndex]["indices"];

                var uvCount = tempArray[meshIndex]["uvCount"];
                var verticesStep = 1;

                // Depending of the number of texture's coordinates per vertex
                // we're jumping in the vertices array  by 6, 8 & 10 windows frame
                switch ((int)uvCount)
                {
                    case 0:
                        verticesStep = 6;
                        break;
                    case 1:
                        verticesStep = 8;
                        break;
                    case 2:
                        verticesStep = 10;
                        break;
                }

                // the number of interesting vertices information for us
                var verticesCount = verticesArray.Length / verticesStep;
                // number of faces is logically the size of the array divided by 3 (A, B, C)
                var facesCount = indicesArray.Length / 3;
                var mesh = new Mesh(tempArray[meshIndex]["name"], verticesCount, facesCount);

                // Filling the Vertices array of our mesh first
                for (var index = 0; index < verticesCount; index++)
                {
                    var x = (float)verticesArray[index * verticesStep];
                    var y = (float)verticesArray[index * verticesStep + 1];
                    var z = (float)verticesArray[index * verticesStep + 2];
                    mesh.Vertices[index] = new Vector3(x, y, z);
                }

                // Then filling the Faces array
                for (var index = 0; index < facesCount; index++)
                {
                    var a = (int)indicesArray[index * 3];
                    var b = (int)indicesArray[index * 3 + 1];
                    var c = (int)indicesArray[index * 3 + 2];
                    mesh.Faces[index] = new Face { A = a, B = b, C = c };
                }

                // Getting the position you've set in Blender
                var position = tempArray[meshIndex]["position"];
                mesh.Position = new Vector3((float)position[0], (float)position[1], (float)position[2]);
                meshes.Add(mesh);
            }

            return meshes.ToArray();
        }
    }
}
