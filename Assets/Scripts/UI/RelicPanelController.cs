using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelicPanelController : MonoBehaviour
{
    [Header("Prefab & ������")]
    [SerializeField] GameObject relicViewPrefab;  // ���� Prefab Assets/Prefabs/UI/RelicView.prefab
    [SerializeField] Transform container;        // ���� RelicPanel �µĿ� GameObject (��� RelicContainer)

    [Header("ͼ�꼯 & ѡ����")]
    [SerializeField] Sprite[] sprites;          // ���鳤�� �� JSON ��� sprite ����+1
    [SerializeField] int choiceCount = 3;  // �����ѡһ���� 3

    List<RelicInfo> allRelics;
    List<RelicInfo> pick3;

    void Awake()
    {
        Debug.Log("RelicPanelController Awake");

        // ���� relics.json������ Assets/Resources/relics.json��
        var txt = Resources.Load<TextAsset>("relics");
        var wrapper = JsonUtility.FromJson<RelicInfoContainer>(
            "{\"relics\":" + txt.text + "}"
        );
        allRelics = wrapper.relics;

        // Ĭ���������
        gameObject.SetActive(false);

        // ������������/�ر�
        EventCenter.AddListener(EventDefine.ShowSpellPart, ShowChoices);
        EventCenter.AddListener(EventDefine.CloseSpellPart, HidePanel);
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener(EventDefine.ShowSpellPart, ShowChoices);
        EventCenter.RemoveListener(EventDefine.CloseSpellPart, HidePanel);
    }

    void ShowChoices()
    {
        Debug.Log("RelicPanelController ShowChoices");

        // 1) ���ȡδӵ�е�����
        pick3 = allRelics
            .Where(r => !RelicManager.Instance.HasRelic(r.name))
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(choiceCount)
            .ToList();

        // 2) ��վɵĲ�
        Debug.Log($"���ǰ container.childCount={container.childCount}");

        foreach (Transform child in container)
            Destroy(child.gameObject);
        Debug.Log($"��պ� container.childCount={container.childCount}");


        // 3) ��¡����ʼ��
        foreach (var info in pick3)
        {
            var go = Instantiate(relicViewPrefab, container);
            Debug.Log("��¡��һ�� RelicView ʵ�� => " + info.name);

            // �ҵ������
            var icon = go.transform.Find("Icon").GetComponent<Image>();
            var desc = go.transform.Find("DescText").GetComponent<TMP_Text>();
            var btn = go.GetComponent<Button>()
                       ?? go.transform.Find("Button").GetComponent<Button>();
            var name = btn.transform.Find("NameText").GetComponent<TMP_Text>();

            // ������
            icon.sprite = sprites[info.sprite];
            desc.text = info.effect.description;
            name.text = info.name;

            // ��ť�ص�
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnPick(info));
        }

        // 4) ��ʾ & ��ͣ
        Debug.Log($"��¡�� container.childCount={container.childCount}");

        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    void OnPick(RelicInfo chosen)
    {
        // ����
        RelicManager.Instance.ActivateRelic(chosen.name);

        // �ر��Լ� & �رշ������
        HidePanel();
        EventCenter.Broadcast(EventDefine.CloseSpellPart);

        // ������һ��
        var sp = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        if (sp != null) sp.NextWave();
    }

    void HidePanel()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    // ���� �ڲ����ݽṹ ���� 
    [Serializable]
    class RelicInfoContainer { public List<RelicInfo> relics; }
    [Serializable]
    public class RelicInfo
    {
        public string name;
        public int sprite;
        public Trigger trigger;
        public Effect effect;
    }
    [Serializable] public class Trigger { public string description, type, amount; }
    [Serializable] public class Effect { public string description, type, amount, until; }
}
