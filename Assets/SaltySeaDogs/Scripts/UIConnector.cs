using UnityEngine;
using UnityEngine.UI;

namespace SaltySeaDogs
{
    public class UIConnector : MonoBehaviour {

        [SerializeField]
        ShowCase showcase = null;

        /// <summary>
        /// Set the ship you want to control
        /// </summary>
        [SerializeField]
        Ship ship = null;

        [SerializeField]
        Slider sliderSpeed = null;
        [SerializeField]
        Slider sliderAngle = null;
        [SerializeField]
        Button buttonRandom = null;
        [SerializeField]
        Button buttonPortGuns = null;
        [SerializeField]
        Button buttonStbdGuns = null;
        [SerializeField]
        Button buttonBowGuns = null;
        [SerializeField]
        Button buttonSternGuns = null;
        [SerializeField]
        Slider sliderLights = null;
        [SerializeField]
        Button buttonNoSails = null;
        [SerializeField]
        Button buttonBattleSails = null;
        [SerializeField]
        Button buttonFullSails = null;
        [SerializeField]
        Slider sliderHelm = null;
        [SerializeField]
        Toggle toggleRotate = null;

        void Start () {
            if (ship == null)
            {
                ship = FindObjectOfType<Ship>();
            }

            //set up the sliders for the control
            sliderAngle.minValue = -ship.MaxAngle;
            sliderAngle.maxValue = ship.MaxAngle;
            sliderAngle.value = ship.YardAngle;
            sliderAngle.onValueChanged.AddListener((o) => { ship.YardAngle = o; });

            sliderSpeed.minValue = 0.01f;
            sliderSpeed.maxValue = 1f;
            sliderSpeed.value = 1f;
            sliderSpeed.onValueChanged.AddListener((o) => { ship.WindStrength = o; });

            sliderHelm.onValueChanged.AddListener((o) => { ship.HelmAngle = o; });

            buttonRandom.onClick.AddListener(() =>
            {
                sliderAngle.value = Random.Range(-ship.MaxAngle, ship.MaxAngle);
                sliderSpeed.value = Random.Range(0.01f, 1f);
                Canvas.ForceUpdateCanvases();
            });

            buttonPortGuns.onClick.AddListener(() => { ship.FireGuns(ShipSide.Port); });
            buttonStbdGuns.onClick.AddListener(() => { ship.FireGuns(ShipSide.Stbd); });
            buttonBowGuns.onClick.AddListener(() => { ship.FireGuns(ShipSide.Bow); });
            buttonSternGuns.onClick.AddListener(() => { ship.FireGuns(ShipSide.Stern); });

            sliderLights.onValueChanged.AddListener((o) => { ship.LightIntensity = o; });

            buttonNoSails.onClick.AddListener(() => { ship.FurlAllSails(); });
            buttonFullSails.onClick.AddListener(() => { ship.SetAllSails(); });
            buttonBattleSails.onClick.AddListener(() => { ship.SetBattleSails(); });

            toggleRotate.onValueChanged.AddListener((o) => { if (showcase != null) { showcase.Rotate = o; } });
        }
	
    }
}

