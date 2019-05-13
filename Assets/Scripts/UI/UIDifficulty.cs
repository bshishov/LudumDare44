using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DifficultyManager;
using UnityEngine.UI;
using TMPro;

public class UIDifficulty : MonoBehaviour
{

    public TextMeshProUGUI CurrDiffText;
    public TextMeshProUGUI NextDiffText;
    public Slider DiffSlider;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var PrevDifficulty = DifficultyManager.Instance.GetDifficulty(DifficultyManager.Instance.CurrentDifficultyIndex - 1);
        var CurrentDifficulty = DifficultyManager.Instance.GetDifficulty(DifficultyManager.Instance.CurrentDifficultyIndex);
        var NextDifficulty = DifficultyManager.Instance.GetDifficulty(DifficultyManager.Instance.CurrentDifficultyIndex + 1);

        CurrDiffText.text = CurrentDifficulty.DifficultyName;
        if (NextDifficulty == null)
            NextDiffText.text = "Infinity";
        else
            NextDiffText.text = NextDifficulty.DifficultyName;

        if (PrevDifficulty == null)
            DiffSlider.minValue = 0;
        else
            DiffSlider.minValue = PrevDifficulty.NextDifficultyStamp;

        DiffSlider.value = DifficultyManager.Instance.ReturnDiffTime();
        DiffSlider.maxValue = CurrentDifficulty.NextDifficultyStamp;
        Debug.Log(DifficultyManager.Instance.CurrentDifficultyIndex);

    }
}
