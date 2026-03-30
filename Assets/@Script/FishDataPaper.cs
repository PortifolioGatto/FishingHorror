using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class FishDataPaper : MonoBehaviour
{
    

    [Space]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI oceansText;
    [SerializeField] private TextMeshProUGUI sizeText;
    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private TextMeshProUGUI priceText;

    public void FillData(FishInstance fish)
    {
        string name = fish.fishData.fishName.GetLocalizedString();
        string oceans = LocalizedDatabase.instance.GetLocalizedStringOceans(fish.fishData.oceans);
        string size = LocalizedDatabase.instance.GetSize(fish.fishData.CalculateSize(fish.size));
        string weight = LocalizedDatabase.instance.GetWeight(fish.fishData.CalculateWeight(fish.size));
        string price = fish.price.ToString();

        nameText.text = name;
        oceansText.text = oceans;
        sizeText.text = size;
        weightText.text = weight;
        priceText.text = "$" + price;
    }

    
}
