﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldGeneration.Data;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        var skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numVertsPerLine = meshSettings.NumberOfVerticesPerLine;

        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 0.5f;
        
        var meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

        var vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        var meshVertexIndex = 0;
        var outOfMeshVertexIndex = -1;

        for (var y = 0; y < numVertsPerLine; y++)
        {
            for (var x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 &&
                                       ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    --outOfMeshVertexIndex;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    ++meshVertexIndex;
                }
            }
        }

        for (var y = 0; y < numVertsPerLine; y++)
        {
            for (var x = 0; x < numVertsPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 &&
                                       ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (!isSkippedVertex)
                {
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isMeshEdgeVertex =
                        (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) &&
                        !isOutOfMeshVertex;

                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 &&
                                        !isOutOfMeshVertex && !isMeshEdgeVertex;

                    bool isEdgeConnectionVertex =
                        ((y == 2) || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) &&
                        !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    var vertexIndex = vertexIndicesMap[x, y];
                    var percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);

                    var vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;
                    var height = heightMap[x, y];

                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int distanceToMainVertexA = ((isVertical ? y : x) - 2) % skipIncrement;
                        int distanceToMainVertexB = skipIncrement - distanceToMainVertexA;
                        float distancePercentFromAToB = distanceToMainVertexA / (float) skipIncrement;

                        float heightMainVertexA = heightMap[(isVertical ? x : x - distanceToMainVertexA),
                            (isVertical ? y - distanceToMainVertexA : y)];
                        float heightMainVertexB = heightMap[(isVertical ? x : x + distanceToMainVertexB),
                            (isVertical ? y + distanceToMainVertexB : y)];

                        height = heightMainVertexA * (1 - distancePercentFromAToB) +
                                 heightMainVertexB * distancePercentFromAToB;
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent,
                        vertexIndex);

                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 &&
                                          (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3)
                            ? skipIncrement
                            : 1;


                        var a = vertexIndicesMap[x, y];
                        var b = vertexIndicesMap[x + currentIncrement, y];
                        var c = vertexIndicesMap[x, y + currentIncrement];
                        var d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        meshData.ProcessMesh();

        return meshData;
    }
}

public class MeshData
{
    private readonly bool _useFlatShading;
    private Vector3[] _vertices;
    private readonly int[] _triangles;
    private Vector2[] _uvs;
    private Vector3[] _bakedNormals;

    private Vector3[] _outOfMeshVertices;
    private int[] _outOfMeshTriangles;

    private int _triangleIndex;
    private int _outOfMeshTriangleIndex;

    public MeshData(int numberOfVerticesPerLine, int skipIncrement, bool useFlatShading)
    {
        _useFlatShading = useFlatShading;

        int numMeshEdgeVertices = (numberOfVerticesPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numMeshEdgeVertices - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numMeshEdgeVertices - 5) / skipIncrement + 1;
        int numMainVertices = numMeshEdgeVertices * numMeshEdgeVertices;
        
        // var perLine = numberOfVerticesPerLine * numberOfVerticesPerLine;
        _vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        _uvs = new Vector2[_vertices.Length];

        int numMeshEdgeTriangles = ((numberOfVerticesPerLine - 4)) * 8;
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        _triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        _outOfMeshVertices = new Vector3[numberOfVerticesPerLine * 4 - 4];
        _outOfMeshTriangles = new int[(numberOfVerticesPerLine - 2) * 24];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            _outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            _vertices[vertexIndex] = vertexPosition;
            _uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            _outOfMeshTriangles[_outOfMeshTriangleIndex] = a;
            _outOfMeshTriangles[_outOfMeshTriangleIndex + 1] = b;
            _outOfMeshTriangles[_outOfMeshTriangleIndex + 2] = c;
            _outOfMeshTriangleIndex += 3;
        }
        else
        {
            _triangles[_triangleIndex] = a;
            _triangles[_triangleIndex + 1] = b;
            _triangles[_triangleIndex + 2] = c;
            _triangleIndex += 3;
        }
    }

    private Vector3[] CalculateNormals()
    {
        var vertexNormals = new Vector3[_vertices.Length];
        var triangleCount = _triangles.Length / 3;
        for (var i = 0; i < triangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertexIndexA = _triangles[normalTriangleIndex];
            var vertexIndexB = _triangles[normalTriangleIndex + 1];
            var vertexIndexC = _triangles[normalTriangleIndex + 2];

            var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        var borderTriangleCount = _outOfMeshTriangles.Length / 3;
        for (var i = 0; i < borderTriangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertexIndexA = _outOfMeshTriangles[normalTriangleIndex];
            var vertexIndexB = _outOfMeshTriangles[normalTriangleIndex + 1];
            var vertexIndexC = _outOfMeshTriangles[normalTriangleIndex + 2];

            var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
                vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0)
                vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0)
                vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (var i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        var pointA = (indexA < 0) ? _outOfMeshVertices[-indexA - 1] : _vertices[indexA];
        var pointB = (indexB < 0) ? _outOfMeshVertices[-indexB - 1] : _vertices[indexB];
        var pointC = (indexC < 0) ? _outOfMeshVertices[-indexC - 1] : _vertices[indexC];

        var sideAB = pointB - pointA;
        var sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh()
    {
        if (_useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    private void BakeNormals()
    {
        _bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        var flatShadedVertices = new Vector3[_triangles.Length];
        var flatShadedUvs = new Vector2[_triangles.Length];
        for (int i = 0; i < _triangles.Length; i++)
        {
            flatShadedVertices[i] = _vertices[_triangles[i]];
            flatShadedUvs[i] = _uvs[_triangles[i]];
            _triangles[i] = i;
        }

        _vertices = flatShadedVertices;
        _uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;
        mesh.uv = _uvs;
        if (_useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = _bakedNormals;
        }

        return mesh;
    }
}