using System;
using UnityEngine;

public class WinCondService : LogicOnlyService<WinCondService>
{
    private string _selectedNPCName;
    private string _selectedEvidenceName;

    private const string _correctNPCName = "Johny";
    private const string _correctEvidenceName = "EvidenceImage";
    public int CurrentSelectingState { get; private set; }

    public void SetSelectedNPCName(string npcName)
    {
        _selectedNPCName = npcName;
        CurrentSelectingState = 1;
    }

    public void SetSelectedEvidenceName(string evidenceName)
    {
        _selectedEvidenceName = evidenceName;
        ValidateWinCondition();
    }

    public Action OnWinConditionMet;
    public Action OnWinConditionFailed;

    private void ValidateWinCondition()
    {
        if (_selectedNPCName == _correctNPCName && _selectedEvidenceName == _correctEvidenceName)
        {
            OnWinConditionMet?.Invoke();
            Debug.Log("Win condition met!");
            // Trigger win condition logic here
        }
        else
        {
            OnWinConditionFailed?.Invoke();
            Debug.Log("Win condition not met.");
            // Handle incorrect selection logic here
        }
    }
}