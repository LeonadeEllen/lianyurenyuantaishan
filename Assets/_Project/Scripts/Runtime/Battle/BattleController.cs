using System.Collections;
using System.Collections.Generic;
using HearTheTide.App;
using HearTheTide.Core;
using UnityEngine;

namespace HearTheTide.Battle
{
    /// <summary>
    /// 一场战斗：搭建占位场景、生成女主/男主/蚀影、按波次推进、结算。
    /// 潮汐节奏判定统一走 Core 的 TideJudge，与服务器校验保持一致。
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        public const float ArenaRadius = 16f;

        public PlayerActor Player { get; private set; }
        public PartnerActor Partner { get; private set; }
        public BattleHUD Hud { get; private set; }
        public ResonanceGauge Resonance { get; private set; }
        public bool Finished { get; private set; }
        public int Wave { get; private set; }
        public StageDefinition Stage { get; private set; }
        public Loadout Loadout { get; private set; }
        public Camera Camera { get { return _camera; } }

        private GameRoot _app;
        private readonly List<EnemyActor> _enemies = new List<EnemyActor>();
        private readonly List<PendingHit> _pending = new List<PendingHit>();
        private Camera _camera;
        private Coroutine _slowMo;

        private sealed class PendingHit
        {
            public EnemyActor Enemy;
            public TideCue Cue;
            public float ImpactTime;
            public float Damage;
        }

        public void Init(GameRoot app, StageDefinition stage, Loadout loadout, CharacterDefinition partnerDef)
        {
            _app = app;
            Stage = stage;
            Loadout = loadout;
            Resonance = new ResonanceGauge();

            BuildArena();

            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(transform, false);
            Player = playerGo.AddComponent<PlayerActor>();
            Player.Battle = this;
            Player.Stats = loadout.PlayerStats;
            Player.Hp = loadout.PlayerStats.MaxHp;
            Player.Loadout = loadout;
            Player.SetupVisual(Placeholder.Character("女主(美术留白)", new Color(0.95f, 0.75f, 0.8f), playerGo.transform, 0.9f));

            var partnerGo = new GameObject("Partner_" + partnerDef.Id);
            partnerGo.transform.SetParent(transform, false);
            partnerGo.transform.position = new Vector3(-2f, 0, -1f);
            Partner = partnerGo.AddComponent<PartnerActor>();
            Partner.Battle = this;
            Partner.Definition = partnerDef;
            Partner.Stats = loadout.PartnerStats;
            Partner.Hp = loadout.PartnerStats.MaxHp;
            Partner.SetupVisual(Placeholder.Character(partnerDef.Name + "(美术留白)", UIUtil.Hex(partnerDef.PlaceholderColorHex), partnerGo.transform, 1.05f));

            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(transform, false);
            Hud = hudGo.AddComponent<BattleHUD>();
            Hud.Init(this, partnerDef);

            StartCoroutine(RunWaves());
        }

        private void BuildArena()
        {
            var camGo = new GameObject("BattleCamera");
            camGo.transform.SetParent(transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.08f, 0.12f, 0.16f);
            _camera.fieldOfView = 50f;
            camGo.tag = "MainCamera";

            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.95f, 0.85f);

            // 码头地面（美术留白）
            var ground = Placeholder.Primitive(PrimitiveType.Cylinder, "Ground(美术留白：雾港码头)", new Color(0.22f, 0.27f, 0.3f), transform);
            ground.transform.localScale = new Vector3(ArenaRadius * 2f + 2f, 0.05f, ArenaRadius * 2f + 2f);
            ground.transform.position = new Vector3(0, -0.05f, 0);
            // 一圈"海水"
            var sea = Placeholder.Primitive(PrimitiveType.Cylinder, "Sea", new Color(0.1f, 0.25f, 0.38f), transform);
            sea.transform.localScale = new Vector3(120f, 0.04f, 120f);
            sea.transform.position = new Vector3(0, -0.12f, 0);
        }

        private void LateUpdate()
        {
            if (Player == null || _camera == null) return;
            var target = Player.transform.position + new Vector3(0, 11f, -8.5f);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            _camera.transform.LookAt(Player.transform.position + Vector3.up * 1f);
        }

        private IEnumerator RunWaves()
        {
            yield return new WaitForSeconds(0.5f);
            for (Wave = 1; Wave <= Stage.Waves; Wave++)
            {
                Hud.Banner("第 " + Wave + " / " + Stage.Waves + " 波");
                bool bossWave = Stage.HasBoss && Wave == Stage.Waves;
                int count = bossWave ? Mathf.Max(1, Stage.EnemiesPerWave - 2) : Stage.EnemiesPerWave;
                for (int i = 0; i < count; i++) SpawnEnemy(i, count, false);
                if (bossWave) SpawnEnemy(0, 1, true);
                while (_enemies.Count > 0 && !Finished) yield return null;
                if (Finished) yield break;
                yield return new WaitForSeconds(0.8f);
            }
            Finish(true);
        }

        private void SpawnEnemy(int index, int count, bool boss)
        {
            float angle = (index / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            var pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (boss ? 9f : 11f);
            var go = new GameObject(boss ? "Boss_蚀影" : "蚀影_" + index);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var e = go.AddComponent<EnemyActor>();
            e.Battle = this;
            e.IsBoss = boss;
            float hp = 600f * Stage.EnemyHpMultiplier * (boss ? 6f : 1f);
            float atk = 60f * Stage.EnemyAttackMultiplier * (boss ? 1.5f : 1f);
            e.Stats = new CombatStats(atk, hp, boss ? 60f : 30f);
            e.Hp = hp;
            if (boss)
            {
                e.HeavyChance = 0.7f;
                e.AttackRange = 3f;
                e.HeavyRadius = 4.2f;
                e.MoveSpeed = 2.6f;
                e.HeavyWindup = 1.0f;
            }
            var color = boss ? new Color(0.35f, 0.15f, 0.45f) : new Color(0.2f, 0.45f, 0.55f);
            e.SetupVisual(Placeholder.Character(boss ? "Boss(美术留白)" : "蚀影(美术留白)", color, go.transform, boss ? 1.8f : 1f));
            _enemies.Add(e);
        }

        // ------------------------------------------------------------ 查询

        public EnemyActor NearestEnemy(Vector3 from, float maxDistance)
        {
            EnemyActor best = null;
            float bestD = maxDistance;
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead) continue;
                float d = Vector3.Distance(from, e.transform.position);
                if (d <= bestD) { bestD = d; best = e; }
            }
            return best;
        }

        public List<EnemyActor> EnemiesInRadius(Vector3 center, float radius)
        {
            var list = new List<EnemyActor>();
            foreach (var e in _enemies)
                if (e != null && !e.IsDead && Vector3.Distance(center, e.transform.position) <= radius) list.Add(e);
            return list;
        }

        public List<EnemyActor> EnemiesInFront(Transform t, float range)
        {
            var list = new List<EnemyActor>();
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead) continue;
                var to = e.transform.position - t.position;
                to.y = 0;
                float d = to.magnitude;
                if (d > range + (e.IsBoss ? 1f : 0f)) continue;
                if (d < 0.8f || Vector3.Dot(t.forward, to / d) > 0.2f) list.Add(e);
            }
            return list;
        }

        public EnemyActor FindRedWindup()
        {
            EnemyActor best = null;
            float bestD = float.MaxValue;
            foreach (var e in _enemies)
            {
                if (e == null || e.IsDead || !e.IsWindingUpRed) continue;
                float d = Vector3.Distance(Player.transform.position, e.transform.position);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        // ------------------------------------------------------------ 潮汐节奏判定

        /// <summary>重攻击命中时刻由敌人调用。</summary>
        public void ResolveHeavyAttack(EnemyActor enemy, TideCue cue, float impactTime, float radius, float rawDamage)
        {
            if (Finished || Player.IsDead) return;
            float dist = Vector3.Distance(enemy.transform.position, Player.transform.position);
            if (dist > radius + 0.4f) return; // 不在范围内
            float damage = DamageFormula.Compute(rawDamage, 1f, Player.Stats.Defense);
            float window = Loadout.TideWindow;

            // 1) 命中前已经做出的应对
            var outcome = TideOutcome.Hit;
            if (cue == TideCue.Blue && Player.Guarding)
                outcome = TideJudge.Judge(cue, TideResponse.Parry, Player.LastParryPressTime - impactTime, window);
            if (outcome != TideOutcome.Rebound && impactTime - Player.LastDodgeTime <= window)
            {
                var o = TideJudge.Judge(cue, TideResponse.Dodge, Player.LastDodgeTime - impactTime, window);
                if (o == TideOutcome.Rebound) outcome = o;
            }

            if (outcome == TideOutcome.Rebound) { ApplyOutcome(enemy, outcome, damage); return; }
            if (Player.Invulnerable) { Hud.Float(Player.transform.position + Vector3.up * 2.5f, "闪避", Color.white); return; }

            // 2) 红潮无宽限；蓝/白潮给 window 秒的"晚按"宽限
            if (cue == TideCue.Red || window <= 0f) { ApplyOutcome(enemy, outcome, damage); return; }
            _pending.Add(new PendingHit { Enemy = enemy, Cue = cue, ImpactTime = impactTime, Damage = damage });
        }

        /// <summary>玩家按下格挡 / 闪避时调用，用于宽限期内的"晚按"判定。</summary>
        public void OnPlayerResponse(TideResponse response)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                var outcome = TideJudge.Judge(p.Cue, response, Time.time - p.ImpactTime, Loadout.TideWindow);
                if (outcome != TideOutcome.Rebound) continue;
                _pending.RemoveAt(i);
                ApplyOutcome(p.Enemy, outcome, p.Damage);
            }
        }

        private void Update()
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                if (Time.time - p.ImpactTime <= Loadout.TideWindow) continue;
                _pending.RemoveAt(i);
                var outcome = p.Cue == TideCue.Blue && Player.Guarding
                    ? TideJudge.Judge(p.Cue, TideResponse.Parry, Player.LastParryPressTime - p.ImpactTime, Loadout.TideWindow)
                    : TideOutcome.Hit;
                if (Player.Invulnerable) continue;
                ApplyOutcome(p.Enemy, outcome, p.Damage);
            }
        }

        private void ApplyOutcome(EnemyActor enemy, TideOutcome outcome, float damage)
        {
            switch (outcome)
            {
                case TideOutcome.Rebound:
                    Resonance.Add(ResonanceGauge.ReboundGain);
                    Hud.Float(Player.transform.position + Vector3.up * 3f, "回潮！", UIColors.Rebound, 1.6f);
                    if (enemy != null && !enemy.IsDead)
                        enemy.TakeDamage(DamageFormula.Compute(Player.Stats.Attack, 2.5f, enemy.Stats.Defense), UIColors.Rebound);
                    SlowMotion(0.3f, 0.8f);
                    break;
                case TideOutcome.Mitigated:
                    Player.TakeDamage(damage * TideJudge.DamageMultiplier(outcome), UIColors.Hurt);
                    Hud.Float(Player.transform.position + Vector3.up * 3f, "格挡", Color.white);
                    break;
                case TideOutcome.Hit:
                    Player.TakeDamage(damage, UIColors.Hurt);
                    break;
            }
        }

        public void PlayResonanceSkill()
        {
            Hud.Banner("双人共鸣技 · " + Loadout.ResonanceSkillName + "\n（电影镜头演出 · 美术留白）");
            SlowMotion(0.15f, 1.2f);
            foreach (var e in EnemiesInRadius(Player.transform.position, 30f))
                e.TakeDamage(DamageFormula.Compute(Player.Stats.Attack + Partner.Stats.Attack, Loadout.ResonanceDamageMultiplier, e.Stats.Defense), UIColors.Rebound);
        }

        private void SlowMotion(float scale, float realSeconds)
        {
            if (_slowMo != null) StopCoroutine(_slowMo);
            _slowMo = StartCoroutine(SlowMoRoutine(scale, realSeconds));
        }

        private IEnumerator SlowMoRoutine(float scale, float realSeconds)
        {
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(realSeconds);
            Time.timeScale = 1f;
            _slowMo = null;
        }

        // ------------------------------------------------------------ 结算

        public void OnEnemyDied(EnemyActor e)
        {
            _enemies.Remove(e);
            _pending.RemoveAll(p => p.Enemy == e);
        }

        public void OnPlayerDefeated() { Finish(false); }

        public void Retreat() { Finish(false); }

        private void Finish(bool victory)
        {
            if (Finished) return;
            Finished = true;
            Time.timeScale = 1f;
            var reward = _app.OnBattleFinished(Stage.Id, Partner.Definition.Id, victory);
            Hud.ShowResult(victory, reward);
        }

        public void Exit() { _app.ExitBattle(); }

        private void OnDestroy() { Time.timeScale = 1f; }
    }

    internal static class UIUtil
    {
        public static Color Hex(string hex) { return HearTheTide.UI.UIKit.Hex(hex); }
    }
}
