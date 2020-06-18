using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldGeneration;
using WorldGeneration.Data;

public class MapPreview : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode
    {
        NoiseMap,
        Mesh,
        FallofMap
    }

    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public Material terrainMaterial;

    [Range(0, MeshSettings.NumberOfSupportedLODs - 1)]
    public int editorPreviewLevelOfDetail;

    public bool autoUpdate;


    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.NumberOfVerticesPerLine,
            meshSettings.NumberOfVerticesPerLine, heightMapSettings, Vector2.zero);
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
                break;
            case DrawMode.Mesh:
                DrawMesh(
                    MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLevelOfDetail));
                break;
            case DrawMode.FallofMap:
                DrawTexture(
                    TextureGenerator.TextureFromHeightMap(
                        new HeightMap(FallofGenerator.GenerateFallofMap(meshSettings.NumberOfVerticesPerLine), 0, 1)));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}