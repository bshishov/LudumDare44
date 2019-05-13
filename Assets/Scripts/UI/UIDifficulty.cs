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
        var currentDiffs = DifficultyManager.Instance.ReturnDiff();
        CurrDiffText.text = currentDiffs[1].DifficultyName;
        if (currentDiffs[2] == null)
            NextDiffText.text = "Infinity";
        else
            NextDiffText.text = currentDiffs[2].DifficultyName;

        CurrDiffText.text = currentDiffs[1].DifficultyName;
        DiffSlider.maxValue = currentDiffs[1].NextDifficultyStamp;
        if (currentDiffs[0] == null)
             DiffSlider.minValue = 0;
        else
            DiffSlider.minValue = currentDiffs[0].NextDifficultyStamp;
        DiffSlider.value = DifficultyManager.Instance.ReturnDiffTime();
        //Debug.Log(string.Format("{0}, {1}, {2}",DiffSlider.minValue, DiffSlider.value, DiffSlider.maxValue));

    }
}
