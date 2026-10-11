using UnityEngine;
using TMPro;

public class WaveHud : MonoBehaviour
{
    // === Inspector 참조 ===
    [SerializeField] private WaveManager manager;
    [SerializeField] private TMP_Text waveLabel;
    [SerializeField] private TMP_Text aliveLabel;

    private void OnEnable()
    {
        manager.OnWaveStarted += HandleWaveStarted;
        manager.OnAliveCountChanged += HandleAliveChanged;
        manager.OnAllWavesCleared += HandleAllCleared;
    }

    private void OnDisable()
    {
        manager.OnWaveStarted -= HandleWaveStarted;
        manager.OnAliveCountChanged -= HandleAliveChanged;
        manager.OnAllWavesCleared -= HandleAllCleared;
    }

    private void HandleWaveStarted(int waveIndex)
    {
        waveLabel.text = $"Wave {waveIndex + 1} / {manager.TotalWaves}";
    }

    private void HandleAliveChanged(int alive)
    {
        aliveLabel.text = $"Alive: {alive}";
    }

    private void HandleAllCleared()
    {
        waveLabel.text = "ALL CLEARED!";
    }
}
