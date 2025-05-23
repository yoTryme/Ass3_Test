using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }
    private List<RelicData> relicConfigs;
    private List<RelicData> activeRelics = new List<RelicData>();
    private PlayerSpellController psc;
    private Hittable playerHittable;
    // stand-still ��Ҫ��״̬
    private Vector3 playerLastPos;
    private bool playerWasMoving = true;
    private float standStillStartTime;

    /// <summary>
    /// �ж�����Ƿ��Ѿ�ӵ��ָ�����ֵ�����
    /// </summary>
    public bool HasRelic(string relicName)
    {
        return activeRelics != null
            && activeRelics.Any(r => r.name == relicName);
    }

    void Awake()
    {
        Instance = this;
        psc = Object.FindFirstObjectByType<PlayerSpellController>();
        playerHittable = psc.GetComponent<Hittable>();
        psc.mana = 0;
        GameManager.Instance.currentWave = 1;
  
            playerLastPos       = psc.transform.position;
    playerWasMoving     = true;
    standStillStartTime = 0f;

        // ���� relics.json
        var txt = Resources.Load<TextAsset>("relics");
        relicConfigs = JsonUtility
            .FromJson<RelicsContainer>("{\"relics\":" + txt.text + "}")
            .relics.ToList();


        // ���� �������б�Ҫ�¼� ���� 
        EventBus.Instance.OnDamage += HandleTakeDamage;    // ��take-damage��
        EventBus.Instance.OnKill += HandleKill;          // ��on-kill��
        EventBus.Instance.OnDealDamage += HandleDealDamage;    // ��on-deal-damage��
        EventBus.Instance.OnCastSpell += HandleCastSpell;     // ��cast-spell��

    }

    void OnDestroy()
    {
        // ���� ��Ӧȡ������ ���� 
        EventBus.Instance.OnDamage -= HandleTakeDamage;
        EventBus.Instance.OnKill -= HandleKill;
        EventBus.Instance.OnDealDamage -= HandleDealDamage;
        EventBus.Instance.OnCastSpell -= HandleCastSpell;
    }

    // ���д���������
    void HandleTakeDamage(Vector3 p, Damage d, Hittable target)
    {
        if (target != playerHittable) return;
        foreach (var r in activeRelics.Where(x => x.trigger.type == "take-damage"))
            ApplyEffect(r);
    }

    public void HandleKill()
    {
        foreach (var r in activeRelics.Where(x => x.trigger.type == "on-kill"))
            ApplyEffect(r);

    }

    public void HandleStandStill()
    {
        //Debug.Log("[RelicManager] HandleStandStill called");
        if (activeRelics == null || activeRelics.Count == 0)
            return;
        foreach (var r in activeRelics.Where(x => x.trigger != null && x.trigger.type == "stand-still"))
        {
            // ���� ��10 wave 5 * +�� ֮��� amount
            var parts = r.effect.amount.Split(' ');
            int baseAmt = int.Parse(parts[0]);
            int waveBonus = 0;
            var idx = System.Array.IndexOf(parts, "wave");
            if (idx >= 0 && idx + 1 < parts.Length)
                waveBonus = GameManager.Instance.currentWave * int.Parse(parts[idx + 1]);
            int total = baseAmt + waveBonus;
            //Debug.Log($"[Relic] {r.name} granted temporary +{total} spellpower");
            psc.AddTempSpell(total);
        }
    }

    public void HandleMove()
    {
        psc.ClearTempSpell();
    }

    // ���� �������������˺�ʱ���� heal��Life Link�� ���� 
    private void HandleDealDamage(Vector3 where, Damage dmg, Hittable target)
    {
        foreach (var r in activeRelics.Where(x => x.trigger.type == "on-deal-damage"))
            ApplyEffect(r);
    }

// ���� ������ʩ��ʱ���� reduce-cooldown��Swift Stone���� increase-damage��Rage Emblem �� on-take-damage�� ���� 
    private void HandleCastSpell()
    {
        foreach (var r in activeRelics.Where(x => x.trigger.type == "cast-spell"))
            ApplyEffect(r);
    }

    // ���ģ����� effect.type �ַ��߼�
    void ApplyEffect(RelicData r)
    {
        switch (r.effect.type)
        {
            case "gain-mana":
                int manaAmt = int.Parse(r.effect.amount);
                psc.AddMana(manaAmt);
                Debug.Log($"[Relic] {r.name} granted {manaAmt} mana");
                break;

            case "gain-spellpower":
                int spAmt = int.Parse(r.effect.amount);
                if (r.effect.until == "cast-spell")
                {
                    psc.nextSpellBonus += spAmt;
                    Debug.Log($"[Relic] {r.name}: next spell +{spAmt} spellpower");
                }
                else
                {
                    psc.AddTempSpell(spAmt);
                    Debug.Log($"[Relic] {r.name} granted temporary +{spAmt} spellpower");
                }
                break;

            // ���� ����Ч������ ���� 
            case "heal":
                int healAmt = int.Parse(r.effect.amount);
                psc.Heal(healAmt);
                Debug.Log($"[Relic] {r.name} healed {healAmt} HP");
                break;

            case "reduce-cooldown":
                float cd = float.Parse(r.effect.amount);
                psc.ReduceCooldown(cd);
                Debug.Log($"[Relic] {r.name} reduced next cooldown by {cd}s");
                break;

            case "increase-damage":
                float pct = float.Parse(r.effect.amount) / 100f;
                psc.BoostNextSpellDamage(pct);
                Debug.Log($"[Relic] {r.name} increased next damage by {pct * 100}%");
                break;

            default:
                Debug.LogWarning($"Unknown effect type: {r.effect.type}");
                break;
        }
    }

    public void ActivateRelic(string name)
    {
        var r = relicConfigs.FirstOrDefault(x => x.name == name);
        if (r != null) activeRelics.Add(r);
    }
    void Update()
    {
        // ��ǰ֡���λ��
        Vector3 curPos = psc.transform.position;
        bool moving = Vector3.Distance(curPos, playerLastPos) > 0.001f;

        // �� �ƶ� �� ��ֹ����¼��ֹ��ʼʱ��
        if (!moving && playerWasMoving)
            standStillStartTime = Time.time;

        // �Ѿ�������ֹ���� 3 �룬����һ�� StandStill
        if (!moving && !playerWasMoving && Time.time - standStillStartTime >= 3f)
        {
            HandleStandStill();
            // ���Ϊ���Ѵ�������ֹ������ֹ�ظ�
            playerWasMoving = true;
        }

        // �� ��ֹ �� �ƶ��������ʱ�ӳ�
        if (moving && !playerWasMoving)
            HandleMove();

        // ����״̬
        playerWasMoving = moving;
        playerLastPos = curPos;
    }

}
