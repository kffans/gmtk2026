using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Linq;

[System.Serializable]
public struct CurrencyDenomination
{
    public int value;
    public GameObject prefab;
}

public class MoneyChange : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _amountSlider;
    [SerializeField] private TMP_Text _amountText;

    [Header("Spawning Settings")]
    [SerializeField] private Transform _spawnContainer;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 0f);

    [Header("Currency Settings")]
    [Tooltip("Dodaj prefaby. Skrypt sam posortuje je od największej wartości do najmniejszej.")]
    [SerializeField] private CurrencyDenomination[] _denominations;

    private List<GameObject> _spawnedMoney = new List<GameObject>();

    void Start()
    {
        _denominations = _denominations.OrderByDescending(d => d.value).ToArray();

        _amountSlider.onValueChanged.AddListener(OnSliderValueChanged);

        OnSliderValueChanged(_amountSlider.value);
    }

    private void OnDestroy()
    {
        if (_amountSlider != null)
        {
            _amountSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    private void OnSliderValueChanged(float sliderValue)
    {
        int amount = Mathf.RoundToInt(sliderValue);
        _amountText.text = $"Geld: {amount}";
        GenerateMoney(amount);
    }

    private void GenerateMoney(int targetAmount)
    {
        ClearPreviousMoney();

        int currentAmount = targetAmount;
        int spawnIndex = 0; 

        foreach (var denomination in _denominations)
        {
            while (currentAmount >= denomination.value)
            {
                currentAmount -= denomination.value;

                SpawnGeldPrefab(denomination.prefab, spawnIndex);
                spawnIndex++;
            }
        }
    }

    private void SpawnGeldPrefab(GameObject prefab, int index)
    {
        if (prefab == null) return;

        GameObject newMoney = Instantiate(prefab, _spawnContainer);
        
        newMoney.transform.localPosition = new Vector3(index * _spacing.x, index * _spacing.y, 0);

        _spawnedMoney.Add(newMoney);
    }

    private void ClearPreviousMoney()
    {
        foreach (var moneyGo in _spawnedMoney)
        {
            if (moneyGo != null)
            {
                Destroy(moneyGo);
            }
        }
        _spawnedMoney.Clear();
    }
}