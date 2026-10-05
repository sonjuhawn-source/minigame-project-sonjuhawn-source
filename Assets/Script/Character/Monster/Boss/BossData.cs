using UnityEngine;

[CreateAssetMenu(fileName = "NewBossData", menuName = "Game/Boss Data")]
public class BossData : ScriptableObject
{
    [Header("기본")]
    public string displayName = "Boss";
    public int maxHp = 500;
    public int attackPower = 20;
    public float moveSpeed = 3f;

    [Header("AI 범위")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("차징")]
    public float chargeRange = 8f;     
    public float telegraphDuration = 1.5f; 
    public float chargeSpeed = 12f;
    public float chargeDuration = 0.8f;
    public float chargeCooldown = 3f;

    [Header("원거리")]
    public float rangedRange = 12f;
    public float rangedCooldown = 3f;
    public float rangedSpeed = 10f;
    public Vector3 muzzleLocalOffset = new Vector3(0, 1.5f, 0.5f);
    public GameObject projectilePrefab;

    [Header("보상")]
    public int expReward = 200;
    public int goldReward = 100;
}