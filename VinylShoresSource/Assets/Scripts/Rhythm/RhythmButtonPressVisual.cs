using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace VinylShores.Rhythm
{
    public sealed class RhythmButtonPressVisual : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform label;
        private Vector2 home;
        private bool initialized;
        private bool held;
        private void OnEnable(){if(!initialized && label!=null){home=label.anchoredPosition;initialized=true;}held=false;Restore();}
        private void OnDisable(){held=false;Restore();}
        private void Restore(){if(initialized && label!=null)label.anchoredPosition=home;}
        public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left || !GetComponent<Button>().IsInteractable())return;held=true;if(initialized)label.anchoredPosition=home+Vector2.down*2;}
        public void OnPointerUp(PointerEventData e){held=false;Restore();}
        public void OnPointerEnter(PointerEventData e){if(held && initialized)label.anchoredPosition=home+Vector2.down*2;}
        public void OnPointerExit(PointerEventData e){Restore();}
    }
}
