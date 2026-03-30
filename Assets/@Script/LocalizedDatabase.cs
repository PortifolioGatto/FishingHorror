using UnityEngine;
using UnityEngine.Localization;

public class LocalizedDatabase : MonoBehaviour
{
    [SerializeField] private LocalizedString weightLocalization;
    [SerializeField] private LocalizedString sizeLocalization;


    [System.Serializable]
    private struct LocalizedOcean
    {
        public Oceans ocean;
        public LocalizedString localization;
    }

    [SerializeField] private LocalizedOcean[] localizedOceans;

    public static LocalizedDatabase instance;

    private void Awake()
    {
        if(instance != null)
        {
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public string GetWeight(float weight)
    {
        string measure = "g";

        float weightT = weight;

        if(weightT > 999)
        {
            measure = "kg";
            weightT /= 1000f;
        }

        if(weightT > 999)
        {
            measure = "t";
            weightT /= 1000f;
        }

        return weightLocalization.GetLocalizedString() + ": " + weightT.ToString("F2") + measure;
    }
    public string GetSize(float size)
    {
        string measure = "cm";
        
        float sizeT = size;

        if (sizeT > 99)
        {
            measure = "m";
            sizeT /= 100f;
        }

        return sizeLocalization.GetLocalizedString() + ": " + sizeT.ToString("F2") + measure;
    }


    public string GetLocalizedStringOcean(Oceans ocean)
    {
        LocalizedOcean oceanLoc = default;

        for (int i = 0; i < localizedOceans.Length; i++)
        {
            if (localizedOceans[i].ocean == ocean)
            {
                oceanLoc = localizedOceans[i];
                break;
            }
        }

        if (oceanLoc.localization == null) return "";

        return oceanLoc.localization.GetLocalizedString();
    }

    public string GetLocalizedStringOceans(Oceans[] oceans)
    {
        string[] allOceans = new string[oceans.Length];

        for (int i = 0; i < allOceans.Length; i++)
        {
            allOceans[i] = GetLocalizedStringOcean(oceans[i]);
        }

        return string.Join(", ", allOceans);
    }
}
