using UnityEngine;

namespace WorldGeneration
{
    public class FallofGenerator
    {
        public static float[,] GenerateFallofMap(int size)
        {
            var map = new float[size,size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var x = i / (float)size * 2 - 1;
                    var y = j / (float)size * 2 - 1;

                    float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    map[i, j] = Evaluate(value);
                }
            }

            return map;
        }

        static float Evaluate(float value)
        {
            var a = 3f;
            var b = 2.2f;

            var pow = Mathf.Pow(value, a);
            return pow / (pow + Mathf.Pow(b - b * value, a));
        }
    }
}