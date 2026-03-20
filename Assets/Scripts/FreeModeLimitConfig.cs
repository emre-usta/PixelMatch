using UnityEngine;

[CreateAssetMenu(fileName = "FreeModeLimitConfig", menuName = "PixelMatch/Free Mode Limit Config")]
public class FreeModeLimitConfig : ScriptableObject
{
    [System.Serializable]
    public class GridLimit
    {
        public int columns;
        public int rows;
        public DifficultyLevel difficulty;
        public float timeLimit;
        public int moveLimit;
    }

    public GridLimit[] limits;

    public GridLimit GetLimit(int columns, int rows, DifficultyLevel difficulty)
    {
        foreach (var limit in limits)
        {
            if (limit.columns == columns &&
                limit.rows == rows &&
                limit.difficulty == difficulty)
                return limit;
        }
        // Bulunamazsa default döndür
        return new GridLimit
        {
            columns = columns,
            rows = rows,
            difficulty = difficulty,
            timeLimit = 120f,
            moveLimit = 30
        };
    }
}