using System;
using UnityEngine;

public class ExperienceSystem : MonoBehaviour
{
    // === 필드 ===
    [SerializeField] private int currentExp;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int pendingPoints = 0;
    [SerializeField] private int pointsPerLevel = 2;

    private PlayerStats stats;

    public int CurrentExp => currentExp;
    public int CurrentLevel => currentLevel;
    public int PendingPoints => pendingPoints;
    public int PointsPerLevel => pointsPerLevel;

    public int ExpToNext => ExpForLevel(currentLevel);

    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelUp;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }
    public void AddExp(int amount) 
    {
        if (amount <= 0)
            return;

        currentExp += amount;

        while(currentExp >= ExpForLevel(currentLevel))
        {
            currentExp -= ExpForLevel(currentLevel);
            currentLevel++;
            pendingPoints += pointsPerLevel;
            OnLevelUp?.Invoke(currentLevel);
        }
        OnExpChanged?.Invoke(currentExp, ExpForLevel(currentLevel));
    }
    public bool SpendPoint(StatType type) 
    {
        if(pendingPoints == 0)
            return false;

        pendingPoints--;
        stats.Grow(type,1);
        return true;
    }
    private int ExpForLevel(int level) 
    {
        return 50 + (level -1) * 10;
    }
}