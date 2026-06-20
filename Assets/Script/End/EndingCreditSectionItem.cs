using TMPro;
using UnityEngine;

public class EndingCreditSectionItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private RectTransform _memberRoot;

    public RectTransform MemberRoot => _memberRoot;

    public void SetTitle(string title)
    {
        if (_titleText == null) return;

        _titleText.text = title;
        _titleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
    }
}