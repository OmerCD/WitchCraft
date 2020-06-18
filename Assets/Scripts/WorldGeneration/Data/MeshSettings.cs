using UnityEngine;

namespace WorldGeneration.Data
{
    [CreateAssetMenu]
    public class MeshSettings : UpdatableData
    {

      
        public const int NumberOfSupportedLODs = 5;
        public const int NumberOfSupportedChunkSizes = 9;
        public const int NumberOfSupportedFlatshadedChunkSizes = 3;
        public static readonly int[] SupportedChunkSizes = {48, 72, 96, 120, 144, 168, 192, 216, 240};
        
        public float meshScale = 2.5f;
        public bool useFlatShading;
        
        [Range(0,NumberOfSupportedChunkSizes-1)]
        public int chunkSizeIndex;
    
        [Range(0,NumberOfSupportedFlatshadedChunkSizes-1)]
        public int flatshadedChunkSizeIndex;

        public int NumberOfVerticesPerLine =>
            SupportedChunkSizes[useFlatShading ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5;

        public float MeshWorldSize => (NumberOfVerticesPerLine - 3) * meshScale;

    }
}