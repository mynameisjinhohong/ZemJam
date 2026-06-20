using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingCreditMemberRowItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _image;

    public void SetData(string name, Sprite image)
    {
        if (_nameText != null)
        {
            _nameText.text = name;
        }

        if (_image != null)
        {
            bool hasImage = image != null;

            _image.gameObject.SetActive(hasImage);
            _image.sprite = image;
            _image.preserveAspect = true;
        }
    }
}