using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SellingObjectUI : MonoBehaviour
{
    [SerializeField] private string name;
    [SerializeField, TextArea(1,5)] private string description;
    [SerializeField] private int price;
    [SerializeField] private Sprite icon;

    [SerializeField] private UnityEvent onBuy;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Buy);
        GetComponent<Image>().sprite = icon;
    }

    public void Buy()
    {
        if(GameManager.Instance.currentMoney >= price)
        {
            GameManager.Instance.currentMoney -= price;
            onBuy.Invoke();
        }
    }

}
