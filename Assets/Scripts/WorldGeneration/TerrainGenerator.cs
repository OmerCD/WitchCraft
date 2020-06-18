using System;
using System.Collections.Generic;
using UnityEngine;
using WorldGeneration.Data;

namespace WorldGeneration
{
    public class TerrainGenerator : MonoBehaviour
    {
        private const float ViewerMoveThresholdForChunkUpdate = 25f;

        private const float SqrViewerMoveThresholdForChunkUpdate =
            ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

        public int colliderLODIndex;
        public LODInfo[] detailLevels;
        public Transform viewer;
        public Material mapMaterial;

        public MeshSettings meshSettings;
        public HeightMapSettings heightMapSettings;
        public TextureData textureSettings;

        private Vector2 viewerPosition;
        private Vector2 viewerPositionOld;
        private float _worldSize;
        private int _chunksVisibleInViewDistance;

        private readonly Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary =
            new Dictionary<Vector2, TerrainChunk>();

        private readonly List<TerrainChunk> VisibleTerrainChunks =
            new List<TerrainChunk>();

        private Vector3 _tempViewerPosition;

        private void Start()
        {
            textureSettings.ApplyToMaterial(mapMaterial);
            textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
            
            float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
            _worldSize = meshSettings.MeshWorldSize;
            _chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _worldSize);

            UpdateVisibleChunks();
        }

        private void Update()
        {
            _tempViewerPosition = viewer.position;
            viewerPosition = new Vector2(_tempViewerPosition.x, _tempViewerPosition.z);

            if (viewerPosition != viewerPositionOld)
            {
                foreach (var terrainChunk in VisibleTerrainChunks)
                {
                    terrainChunk.UpdateCollisionMesh();
                }
            }


            if ((viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate)
            {
                viewerPositionOld = viewerPosition;
                UpdateVisibleChunks();
            }
        }

        void UpdateVisibleChunks()
        {
            var alreadyUpdatedChunkCoords = new HashSet<Vector2>();
            for (int i = VisibleTerrainChunks.Count - 1; i > -1; i--)
            {
                alreadyUpdatedChunkCoords.Add(VisibleTerrainChunks[i].Coord);
                VisibleTerrainChunks[i].UpdateTerrainChunk();
            }

            int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / _worldSize);
            int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / _worldSize);

            for (int yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++)
            {
                for (int xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++)
                {
                    var viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                    if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                    {
                        if (_terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                        {
                            _terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                        }
                        else
                        {
                            var terrainChunk = new TerrainChunk(
                                viewedChunkCoord, 
                                heightMapSettings, 
                                meshSettings,
                                detailLevels, 
                                colliderLODIndex,
                                transform,
                                viewer,
                                mapMaterial);
                            _terrainChunkDictionary.Add(viewedChunkCoord,
                                terrainChunk);

                            terrainChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                            terrainChunk.Load();
                        }
                    }
                }
            }
        }

        void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
        {
            if (isVisible)
            {
                VisibleTerrainChunks.Add(chunk);
            }
            else
            {
                VisibleTerrainChunks.Remove(chunk);
            }
        }
    }

    [Serializable]
    public struct LODInfo
    {
        [Range(0, MeshSettings.NumberOfSupportedLODs - 1)]
        public int lod;

        public float visibleDistanceThreshold;
        public float SqrVisibleDstThreshold => visibleDistanceThreshold * visibleDistanceThreshold;
    }
}