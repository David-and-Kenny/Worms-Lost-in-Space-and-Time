using System;
using UnityEngine;
using Worms;
using Worms.Core;
using Worms.Gameplay;
public class Oxygen : MonoBehaviour
{
    public float air = 10;
    public float maxAir = 10;
    public float airBurnRate = 1f;
    public Texture2D bgTexture;
    public Texture2D airBarTexture;
    public int iconWidth = 32;
    public Vector2 airOffset = new Vector2(5, 5);
    [SerializeField] private Player player;
    public OxygenSupply Supply { get; private set; }
    public float Fraction => Supply != null ? Supply.Fraction : (maxAir > 0f ? Mathf.Clamp01(air / maxAir) : 0f);
    public event Action<float> FractionChanged;
    private void Start()
    {
        if (!player) player = FindFirstObjectByType<Player>();
        Supply = new OxygenSupply(air, maxAir, airBurnRate);
        Supply.Changed += OnChanged;
        Supply.Depleted += OnDepleted;
    }
    private void OnChanged(float fraction)
    {
        air = Supply.Current;
        FractionChanged?.Invoke(fraction);
    }
    private void OnDepleted()
    {
        if (!player) return;
        var explosion = player.GetComponent<Explode>();
        if (explosion) explosion.OnExplode();
        enabled = false;
    }
    public void addO2()
    {
        air = 10f;
        if (Supply != null) Supply.Refill();
        else FractionChanged?.Invoke(Fraction);
    }
    private void Update()
    {
        if (!player || Supply == null) return;
        var host = LevelSessionHost.Current;
        if (host && (host.IsPaused || host.Session.State != SessionState.Playing)) return;
        Supply.Tick(Time.deltaTime);
        air = Supply.Current;
    }
    private void OnDestroy()
    {
        if (Supply == null) return;
        Supply.Changed -= OnChanged;
        Supply.Depleted -= OnDepleted;
    }
    private void OnValidate()
    {
        if (maxAir <= 0f || airBurnRate < 0f) Debug.LogError("Oxygen requires positive maxAir and nonnegative burn rate.", this);
    }
}
