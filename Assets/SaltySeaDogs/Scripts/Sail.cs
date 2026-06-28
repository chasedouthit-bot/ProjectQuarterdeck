using UnityEngine;
using System.Collections;
namespace SaltySeaDogs
{
    public class Sail : MonoBehaviour
    {

        [Header("Sail")]
        public SailType SailType = SailType.Squaresail;

        [Header("Meshes")]
        [Tooltip("Mesh used to change from full to furled. Is swapped for furled mesh when at furled scale")]
        public GameObject SailFull = null;
        public GameObject SailFurled = null;

        [Header("Status of sail")]
        public SailStatus Status = SailStatus.Set;
        public bool changing = false;

        [Header("Yard Lift")]
        [SerializeField]
        Vector3 TopPosition = Vector3.zero;
        [SerializeField]
        Vector3 BottomPosition = Vector3.zero;

        [Header("Scales and Speed")]
        public Vector3 FullScale = Vector3.one;
        public Vector3 FurledScale = new Vector3(1f, 1f, 0f);
        public float TransitionTime = 10f;
        private float transitionTime = 10f;

        public float WindPower = 1f;
        private float windPower = 1f;
        public RotationalAxes windAxis = RotationalAxes.Z;

        //public Vector3 target = Vector3.one;

        [Range(0f, 1f)]
        public float currentStatus = 1f;

        //Does this sail get set in the 'battle' configuration?
        [Tooltip("Does this sail get set in battle configuration?")]
        public bool BattleSail = false;

        //Does shader reverse normals when windpower is negative?
        [Tooltip("Does shader reverse normals when windpower is negative?")]
        [SerializeField]
        bool reverseShader = true;

        Material sailClothMaterial;

        void Start()
        {
            SailFull.SetActive(true);
            SailFurled.SetActive(false);
            ChangeStatus(SailStatus.Set);
            sailClothMaterial = SailFull.GetComponent<MeshRenderer>().material;
            if (TopPosition == Vector3.zero)
            {
                TopPosition = transform.localPosition;
            }
            if (BottomPosition == Vector3.zero)
            {
                BottomPosition = transform.localPosition;
            }
        }

        public void ChangeStatus(SailStatus _status)
        {
            StopAllCoroutines();
            StartCoroutine(changeStatus(_status));
        }

        void Update()
        {
            if (windPower != WindPower && Status != SailStatus.Furled)
            {
                updateWindScale();
                windPower = WindPower;
            }

            if (currentStatus > 0)
            {
                SailFull.SetActive(true);
                SailFurled.SetActive(false);
                SailFull.transform.localScale = Vector3.Lerp(FurledScale, FullScale, currentStatus);
                transform.localPosition = Vector3.Lerp(BottomPosition, TopPosition, currentStatus);
                updateWindScale(currentStatus);
            }
            else
            {
                SailFull.SetActive(false);
                SailFurled.SetActive(true);
            }

        }

        void updateWindScale(float scale = 1f)
        {
            if (sailClothMaterial != null && reverseShader)
            {
                if (WindPower > 0f)
                {
                    sailClothMaterial.SetFloat("_NormalIntensity", 1f);
                }
                else
                {
                    sailClothMaterial.SetFloat("_NormalIntensity", -1f);
                }
            }

            var wind = scale * WindPower;

            SailFull.transform.localScale = new Vector3((windAxis == RotationalAxes.X) ? wind : SailFull.transform.localScale.x,
                    (windAxis == RotationalAxes.Y) ? wind : SailFull.transform.localScale.y,
                    (windAxis == RotationalAxes.Z) ? wind : SailFull.transform.localScale.z);

            //if (SailType == SailType.Squaresail)
            //{
            //    SailFull.transform.localScale = new Vector3( (windAxis == RotationalAxes.X) ? wind : SailFull.transform.localScale.x,
            //        (windAxis == RotationalAxes.Y) ? wind : SailFull.transform.localScale.y,
            //        (windAxis == RotationalAxes.Z) ? wind : SailFull.transform.localScale.z);
            //}
            //else if (SailType == SailType.Gaff)
            //{
            //    SailFull.transform.localScale = new Vector3(scale * WindPower, SailFull.transform.localScale.y, SailFull.transform.localScale.z);
            //}
            //else if (SailType == SailType.Staysail)
            //{
            //    SailFull.transform.localScale = new Vector3(scale * WindPower, SailFull.transform.localScale.y, SailFull.transform.localScale.z);
            //}
        }

        IEnumerator changeStatus(SailStatus _status)
        {
            if (_status != Status)
            {
                changing = true;
                transitionTime = TransitionTime * currentStatus;
                if (_status == SailStatus.Set) //we want to count up
                {
                    Status = SailStatus.Setting;
                    while (transitionTime < TransitionTime)
                    {
                        currentStatus = transitionTime / TransitionTime;
                        transitionTime += Time.deltaTime;
                        yield return null;
                    }
                    currentStatus = 1;
                }

                if (_status == SailStatus.Furled) //we want to count down
                {
                    Status = SailStatus.Setting;
                    while (transitionTime > 0)
                    {
                        currentStatus = transitionTime / TransitionTime;
                        transitionTime -= Time.deltaTime;
                        yield return null;
                    }
                    currentStatus = 0;
                }

                Status = _status;
                changing = false;
            }
        }
    }
}

