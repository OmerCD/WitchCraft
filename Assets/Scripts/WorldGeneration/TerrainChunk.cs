using System;
using UnityEngine;
using WorldGeneration.Data;

namespace WorldGeneration
{
    public class TerrainChunk
    {
        private const float ColliderGenerationDistanceThreshold = 5;
        public event Action<TerrainChunk, bool> OnVisibilityChanged;

        public Vector2 Coord;
        private readonly HeightMapSettings _heightMapSettings;
        private readonly MeshSettings _meshSettings;

        private readonly GameObject _meshObject;
        private readonly Vector2 _sampleCentre;
        private Bounds _bounds;

        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;

        private readonly LODInfo[] _detailLevels;
        private readonly int _colliderLodIndex;
        private readonly Transform _viewer;
        private readonly LODMesh[] _lodMeshes;

        private HeightMap _heightMap;
        private bool _heightMapReceived;
        private int _previousLodIndex = -1;
        private bool _hasSetCollider;
        private float _maxViewDistance;

        public TerrainChunk(Vector2 coord, 
            HeightMapSettings heightMapSettings,
            MeshSettings meshSettings,
            LODInfo[] detailLevels, 
            int colliderLodIndex,
            Transform parent,
            Transform viewer,
            Material material)
        {
            Coord = coord;
            _heightMapSettings = heightMapSettings;
            _meshSettings = meshSettings;
            _detailLevels = detailLevels;
            _colliderLodIndex = colliderLodIndex;
            _viewer = viewer;
            _sampleCentre = coord * meshSettings.MeshWorldSize / meshSettings.meshScale;
            var position = coord * meshSettings.MeshWorldSize;
            _bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);


            _meshObject = new GameObject("Terrain Chunk");
            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshCollider = _meshObject.AddComponent<MeshCollider>();
            _meshRenderer.material = material;

            _meshObject.transform.position = new Vector3(position.x, 0, position.y);
            _meshObject.transform.parent = parent;
            SetVisible(false);

            _lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                _lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                _lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
                if (i == colliderLodIndex)
                {
                    _lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
                }
            }

            _maxViewDistance = _detailLevels[_detailLevels.Length - 1].visibleDistanceThreshold;
           
        }

        public void Load()
        {
            ThreadedDataRequester.RequestData(
                () => HeightMapGenerator.GenerateHeightMap(_meshSettings.NumberOfVerticesPerLine,
                    _meshSettings.NumberOfVerticesPerLine, _heightMapSettings, _sampleCentre), OnHeightMapReceived); 
        }
        private Vector2 ViewerPosition => new Vector2(_viewer.position.x, _viewer.position.z);

        void OnHeightMapReceived(object heightMap)
        {
            _heightMap = (HeightMap) heightMap;
            _heightMapReceived = true;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (_heightMapReceived)
            {
                var viewerDistanceFromNearestEdge = Math.Sqrt(_bounds.SqrDistance(ViewerPosition));
                bool wasVisible = IsVisible();
                bool visible = viewerDistanceFromNearestEdge <= _maxViewDistance;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < _detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > _detailLevels[i].visibleDistanceThreshold)
                        {
                            ++lodIndex;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != _previousLodIndex)
                    {
                        LODMesh lodMesh = _lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            _previousLodIndex = lodIndex;
                            _meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(_heightMap, _meshSettings);
                        }
                    }
                }

                if (wasVisible != visible)
                {
                    SetVisible(visible);
                    OnVisibilityChanged?.Invoke(this, visible);
                }
            }
        }

        public void UpdateCollisionMesh()
        {
            if (!_hasSetCollider)
            {
                float sqrDstFromViewerToEdge = _bounds.SqrDistance(ViewerPosition);
                if (sqrDstFromViewerToEdge < _detailLevels[_colliderLodIndex].SqrVisibleDstThreshold)
                {
                    if (!_lodMeshes[_colliderLodIndex].hasRequestedMesh)
                    {
                        _lodMeshes[_colliderLodIndex].RequestMesh(_heightMap,_meshSettings);
                    }
                }

                if (sqrDstFromViewerToEdge <
                    ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)
                {
                    if (_lodMeshes[_colliderLodIndex].hasMesh)
                    {
                        _meshCollider.sharedMesh = _lodMeshes[_colliderLodIndex].mesh;
                        _hasSetCollider = true;
                    }
                }
            }
        }

        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }
    }

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private readonly int _lod;
        public event Action UpdateCallback;

        public LODMesh(int lod)
        {
            _lod = lod;
        }

        void OnMeshDataReceived(object meshData)
        {
            mesh = ((MeshData) meshData).CreateMesh();
            hasMesh = true;

            UpdateCallback();
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(
                () => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, _lod), OnMeshDataReceived);
        }
    }
}