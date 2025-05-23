using UnityEngine;

using System;

[Serializable]
public class TriggerData
{
    public string description;
    public string type;   // "take-damage","on-kill","stand-still"��
    public int amount; // ���� stand-still ����
}

[Serializable]
public class EffectData
{
    public string description;
    public string type;   // "gain-mana","gain-spellpower"��
    public string amount; // ��ֵ����ʽ
    public string until;  // ��ѡ���� "move","cast-spell"
}

[Serializable]
public class RelicData
{
    public string name;
    public int sprite;
    public TriggerData trigger;
    public EffectData effect;
}

[Serializable]
public class RelicsContainer
{
    public RelicData[] relics;
}
