using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SaltySeaDogs
{
    public class Ship : MonoBehaviour {

        private Sail[] allSails = { };
        private SailSetting currentSailSetting = SailSetting.Full;

        /// <summary>
        /// Get the current sail setting only
        /// </summary>
        public SailSetting CurrentSailSetting
        {
            get
            {
                return currentSailSetting;
            }            
        }


        [Header("Helm Properties")]
        /// <summary>
        /// The maximum angle the helm can turn in both directions.
        /// </summary>
        [Tooltip("The maximum angle the helm can turn in both directions.")]
        public float MaxHelmAngle = 35f;

        /// <summary>
        /// The helm angle requested at this time.
        /// </summary>
        public float HelmAngle {
            get { return helmAngle * MaxHelmAngle; }
            set { helmAngle = value; }
        }
        [Range(-1f, 1f), Tooltip("The helm angle requested at this time.")]
        private float helmAngle = 0f;

        /// <summary>
        /// Speed that the yards turn at.
        /// </summary>
        [Tooltip("Speed that the helm turn at.")]
        public float HelmTurnSpeed = 10f;

        /// <summary>
        /// Helm to rudder ratio
        /// </summary>
        [Tooltip("Ratio of turns on helm to angle of rudder")]
        public float HelmTurnRatio = 10f;

        /// <summary>
        /// private variable of the helm angle that is used for transitioning to the target angle
        /// </summary>
        private float _currentHelmAngle = 0.01f;
        public float CurrentHelmAngle
        {
            get { return _currentHelmAngle; }
        }

        [Header("Helm and Rudder Objects")]
        /// <summary>
        /// Helm to rudder ratio
        /// </summary>
        [Tooltip("Rudder Object")]
        public Transform Rudder;

        /// <summary>
        /// Rotational Axes for Rudder
        /// </summary>
        [Tooltip("Rotational Axes for Rudder")]
        public RotationalAxes RudderRotationalAxes = RotationalAxes.Z;

        /// <summary>
        /// Helm object
        /// </summary>
        [Tooltip("Helm Object")]
        public Transform Helm;


        [Header("Yard/Brace Properties")]
        /// <summary>
        /// The maximum angle the yards can turn in both directions. Applies to both Gaff and Square.
        /// </summary>
        [Tooltip("The maximum angle the yards can turn in both directions. Applies to both Gaff and Square.")]
        public float MaxAngle = 35f;

        /// <summary>
        /// The yard angle requested at this time.
        /// </summary>       
        public float YardAngle {
            set
            {
                if (value > MaxAngle)
                {
                    yardAngle = MaxAngle;
                } else if (value < -MaxAngle)
                {
                    yardAngle = -MaxAngle;
                } else
                {
                    yardAngle = value;
                }                
            }
            get
            {
                return yardAngle;
            }
        }
        [SerializeField, Range(-35f, 35f), Tooltip("The yard angle requested at this time.")]
        private float yardAngle = 0f;
        /// <summary>
        /// Speed that the yards turn at.
        /// </summary>
        [Tooltip("Speed that the yards turn at.")]
        public float YardTurnSpeed = .3f;

        /// <summary>
        /// Rotational Axes for Yards
        /// </summary>
        [Tooltip("Rotational Axes for Yards")]
        public RotationalAxes YardRotationalAxes = RotationalAxes.Y;

        [Space ,Header("Strength of sail deformation")]
        /// <summary>
        /// Strength of the 'wind' that deforms the sails.
        /// </summary>
        [Range(0.01f,1f), Tooltip("Strength of the 'wind' that deforms the sails.")]
        public float WindStrength = 1f;

        /// <summary>
        /// private variable of the wind strength that is used for transitioning to the target amount.
        /// </summary>
        private float _windStrength = 1f;

        /// <summary>
        /// private variable of the yard angle that is used for transitioning to the target angle
        /// </summary>
        private float _currentAngle = 0.01f;

        /// <summary>
        /// private variable that is used to determine the direction that the gaff/jibs should blow toward
        /// </summary>
        private ShipSide _windSide = ShipSide.Stbd;
         
        [Space(10), Header("Collections for objects to pivot")]
        /// <summary>
        /// The collection of yards that you want to turn.
        /// </summary>
        [ Tooltip("The collection of yards that you want to turn.")]
        List<GameObject> yards = new List<GameObject>();

        /// <summary>
        /// Collection of gaff booms.
        /// </summary>
        [ Tooltip("Collection of gaff booms.")]
        List<GameObject> gaffs = new List<GameObject>();

        /// <summary>
        /// Collection of all of the square sails.
        /// </summary>
        [Tooltip("Collection of all of the square sails.")]
        List<Sail> SquareSails = new List<Sail>();

        /// <summary>
        /// Collection of the jibs/foresails.
        /// </summary>
        [Tooltip("Collection of the jibs/foresails.")]
        List<Sail> Jibs = new List<Sail>();

        /// <summary>
        /// Collection of the Gaff/lateen/spankers.
        /// </summary>
        [Tooltip("Collection of the Gaff/lateen/spankers.")]
        List<Sail> GaffSails = new List<Sail>();

        /// <summary>
        /// Sails to set for 'battle' condition. None and Full settings affect all sails.
        /// </summary>
        [Tooltip("Sails to set for 'battle' condition. None and Full settings affect all sails.")]
        List<Sail> BattleSails = new List<Sail>();

        [Header("Guns")]
        /// <summary>
        /// Collection of the gun locators.
        /// </summary>
        [Tooltip("Collection of the port gun locators")]
        List<Cannon> GunsPort = new List<Cannon>();

        /// <summary>
        /// Collection of the gun stbd  locators.
        /// </summary>
        [Tooltip("Collection of the stbd gun locators")]
        List<Cannon> GunsStbd = new List<Cannon>();

        /// <summary>
        /// Collection of the gun bow locators.
        /// </summary>
        [Tooltip("Collection of the gun bow locators")]
        List<Cannon> GunsBow = new List<Cannon>();

        /// <summary>
        /// Collection of the gun stern locators.
        /// </summary>
        [Tooltip("Collection of the gun stern locators")]
        List<Cannon> GunsStern = new List<Cannon>();

        [Header("Objects")]
        [SerializeField, Tooltip("Hull object that has the emissive material on it.")]
        GameObject hull = null;
        private List<emissiveMaterial> materials = new List<emissiveMaterial>();
        [SerializeField, ColorUsage(false,true,0,4,0,1)]
        Color lightColor = Color.yellow;


        [Header("Line Renderers")]
        [SerializeField, Tooltip("Show running rigging")]
        bool showRunningLines = true;
        [SerializeField, Tooltip("Line Renderer prefab")]
        GameObject lineRendererPrefab = null;
        private GameObject lineParent = null;

        List<lineRenderer> linePoints = new List<lineRenderer>();
        private bool updateRopes = false; //used to update the ropes on the following frame from update call so that they get to the right position
        [SerializeField]
        float lineDetail = 1f;

        [Header("Cannons")]
        [SerializeField, Tooltip("Model to use for full cannons")]
        GameObject fullCannon = null;
        [SerializeField, Tooltip("Model to use for tips of cannons")]
        GameObject tipCannon = null;
        

        /// <summary>
        /// Light intensity for emissive texture.
        /// </summary>
        public float LightIntensity
        {
            set
            {
                value = 1f - Mathf.Clamp01(value);
                foreach (emissiveMaterial em in materials)
                {
                    em.material.SetColor("_EmissionColor", Color.Lerp(em.emissiveColor, Color.black, value));
                    //if (ms.GetTexture("_EmissionMap") != null)
                    //{
                    //    ms.EnableKeyword("_EMISSION");
                    //    ms.SetFloat("_EmissionScale", value);
                    //    //Color c = ms.GetColor("_EmissionColor");
                    //    //c.a = value;
                    //    ////c = new Color(c.r, c.g, c.b, value);
                    //    //ms.SetColor("_EmissionColor", c);
                    //}
                }
            }
        }

        /// <summary>
        /// Fire the guns on a specific side of the ship.
        /// </summary>
        /// <param name="_battery"></param>
        public void FireGuns(ShipSide _battery)
        {
            List<Cannon> _guns = null;
            switch (_battery)
            {
                case ShipSide.Port:
                    _guns = GunsPort;
                    break;
                case ShipSide.Stbd:
                    _guns = GunsStbd;
                    break;
                case ShipSide.Stern:
                    _guns = GunsStern;
                    break;
                case ShipSide.Bow:
                    _guns = GunsBow;
                    break;
            }
            StartCoroutine(FireGuns(_guns));
        }

        IEnumerator FireGuns(List<Cannon> _guns)
        {
            for (int i = 0; i < _guns.Count; i++)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, .25f));
                _guns[i].FireGun();
                //_guns[i].GetComponentInChildren<ParticleSystem>().Play(); 
            }
            yield return null;
        }

        void Start()
        {

            //find all of the cannon points, add a model and cannon object at the fire point
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.gameObject.name.Contains("Cannon_") || t.gameObject.name.Contains("CannonTip_"))
                {
                    GameObject g = (t.gameObject.name.Contains("CannonTip")) ? Instantiate(tipCannon) : Instantiate(fullCannon);
                    g.transform.parent = t;
                    g.transform.localScale = Vector3.one;
                    g.transform.localPosition = Vector3.zero;
                    g.transform.localRotation = Quaternion.identity;
                    //find the muzzle of the new object
                    foreach (Transform c in g.GetComponentsInChildren<Transform>())
                    {
                        //this is the one, put a cannon object here
                        if (c.gameObject.name == "Muzzle")
                        {
                            Cannon cannon = c.gameObject.GetComponent<Cannon>();
                            if (t.gameObject.name.Contains("Port"))
                            {
                                GunsPort.Add(cannon);
                                cannon.GunLocation = ShipSide.Port;
                            } else if (t.gameObject.name.Contains("Stbd"))
                            {
                                GunsStbd.Add(cannon);
                                cannon.GunLocation = ShipSide.Stbd;
                            } else if (t.gameObject.name.Contains("Bow"))
                            {
                                GunsBow.Add(cannon);
                                cannon.GunLocation = ShipSide.Bow;
                            } else
                            {
                                GunsStern.Add(cannon);
                                cannon.GunLocation = ShipSide.Stern;
                            } 
                            
                        }
                    }

                }
            }
            
            //clear the lists for sails and yards
            yards.Clear();
            gaffs.Clear();
            SquareSails.Clear();
            Jibs.Clear();
            GaffSails.Clear();
            BattleSails.Clear();
            
            //get all of the sails
            allSails = gameObject.GetComponentsInChildren<Sail>();

            //build the sail and yard collections
            foreach (Sail s in allSails)
            {
                switch (s.SailType)
                {
                    case SailType.Squaresail:
                        yards.Add(s.gameObject);
                        SquareSails.Add(s);
                        break;
                    case SailType.Gaff:
                        gaffs.Add(s.gameObject);
                        GaffSails.Add(s);
                        break;
                    case SailType.Staysail:
                        Jibs.Add(s);
                        break;
                }

                if (s.BattleSail)
                {
                    BattleSails.Add(s);
                }
            }

            //build materials for lighting
            foreach (Material ms in hull.GetComponent<MeshRenderer>().sharedMaterials)
            {
                if (ms.GetTexture("_EmissionMap") != null)
                {
                    materials.Add(new emissiveMaterial(ms, lightColor));
                    ms.EnableKeyword("_EMISSION");
                }
            }
            LightIntensity = 0f;

            //this will either render or not render the running rigging
            if (showRunningLines)
            {
                //build a list of all line render points, using the 'Line_' prefix
                var linepoints = GetComponentsInChildren<Transform>();
                var lineObjects = new List<lineObject>();
                lineParent = new GameObject("Lines");
                lineParent.transform.parent = transform;
                lineParent.transform.position = transform.position;
                lineParent.transform.localPosition = Vector3.zero;
                lineParent.transform.localScale = Vector3.one;
                //lineParent = lineparent.transform;

                foreach (Transform t in linepoints)
                {
                    if (t.gameObject.name.Contains("Line_") || t.gameObject.name.Contains("LineB_") || t.gameObject.name.Contains("LineS_") || t.gameObject.name.Contains("LineL_"))
                    {
                        //get the name of the line
                        string[] names = t.gameObject.name.Split('_');
                        if (names[1] != null)
                        {
                            //for each named line, add it to the list of lines
                            var line = new lineObject(t.gameObject, names[1]);
                            for (int i = 0; i < names.Length; i++)
                            {
                                //get the list of targets that this line points to
                                if (i > 1)
                                {
                                    line.targets.Add(names[i]);
                                }
                            }
                            lineObjects.Add(line);
                        }
                    }
                }

                foreach (var point in lineObjects)
                {
                    //here is where we build the actual line renders
                    //only process ones with a target
                    if (point.targets.Count > 0)
                    {
                        //get the target name and find it
                        foreach (var t in point.targets)
                        {
                            //find the target as a base object
                            foreach (var o in lineObjects)
                            {
                                if (o.name == t)
                                {
                                    var go = Instantiate(lineRendererPrefab);
                                    go.transform.parent = lineParent.transform;
                                    go.transform.localScale = Vector3.one;
                                    
                                    //we have both parts needed, time to make a line renderer
                                    //let's see what kind of line it is: B = brace, S = sail, L = lift
                                    var _slack = 0.05f;
                                    if (point.lineItem.name.Contains("LineB"))
                                    {
                                        _slack = 0.1f;
                                    } else if (point.lineItem.name.Contains("LineS"))
                                    {
                                        _slack = 0.15f;
                                    }
                                    else if (point.lineItem.name.Contains("LineL"))
                                    {
                                        _slack = 0.01f;
                                    }

                                    var lineItem = new lineRenderer(go.GetComponent<LineRenderer>(), lineParent.transform, point.lineItem.transform, o.lineItem.transform, _slack, lineDetail);
                                    //lineItem.Update();
                                    linePoints.Add(lineItem);
                                }
                            }
                        }
                    }
                }
            }
            

        }

        public void SetAllSails()
        {
            for (int i=0; i < allSails.Length; i++)
            {
                allSails[i].ChangeStatus(SailStatus.Set);
            }
            currentSailSetting = SailSetting.Full;
        }

        public void FurlAllSails()
        {
            for (int i = 0; i < allSails.Length; i++)
            {
                allSails[i].ChangeStatus(SailStatus.Furled);
            }
            currentSailSetting = SailSetting.Furled;
        }

        public void SetBattleSails()
        {
            FurlAllSails();
            for (int i = 0; i < BattleSails.Count; i++)
            {
                BattleSails[i].ChangeStatus(SailStatus.Set);
            }
            currentSailSetting = SailSetting.Battle;
        }

        public void SailsIncrease()
        {
            switch (currentSailSetting)
            {
                case SailSetting.Furled:
                    SetBattleSails();
                    break;
                case SailSetting.Battle:
                    SetAllSails();
                    break;
            }
        }

        public void DecreaseSails()
        {
            switch (currentSailSetting)
            {
                case SailSetting.Full:
                    SetBattleSails();
                    break;
                case SailSetting.Battle:
                    FurlAllSails();
                    break;
            }
        }

        void Update () {

            //this happens a frame later than the checks
            if (updateRopes && showRunningLines)
            {
                lineParent.transform.position = lineParent.transform.parent.transform.position;
                for (int i = 0; i < linePoints.Count; i++)
                {
                    linePoints[i].Update();
                }
                updateRopes = false;
            }

            if (HelmAngle != _currentHelmAngle)
            {
                _currentHelmAngle = Mathf.MoveTowards(_currentHelmAngle, -HelmAngle, HelmTurnSpeed * Time.deltaTime);
                if (Helm != null)
                {
                    Helm.localRotation = Quaternion.Euler(0f, 0f, _currentHelmAngle * HelmTurnRatio);
                }

                Rudder.localRotation = Quaternion.Euler(
                    (RudderRotationalAxes == RotationalAxes.X) ? _currentHelmAngle : 0f,
                     (RudderRotationalAxes == RotationalAxes.Y) ? _currentHelmAngle : 0f,
                     (RudderRotationalAxes == RotationalAxes.Z) ? _currentHelmAngle : 0f
                    );
            }

            for (int j = 0; j < allSails.Length; j++)
            {
                if (allSails[j].changing)
                {

                    updateRopes = true;
                    break;
                }
            }

            if (YardAngle != _currentAngle)
            {
                updateRopes = true;

                //determine the side of the boat that the sails should be going toward for gaffs/jibs
                _windSide = (_currentAngle > 0f) ? ShipSide.Stbd : ShipSide.Port;

                //shift the current angle toward the requested angle. It eventually meets the requested angle and stops
                _currentAngle = Mathf.MoveTowards(_currentAngle, YardAngle, YardTurnSpeed * Time.deltaTime);

                //main yards turning to the rotation
                for (int i = 0; i < yards.Count; i++)
                {
                    yards[i].transform.localRotation = Quaternion.Euler(
                    (YardRotationalAxes == RotationalAxes.X) ? _currentAngle : 0f,
                     (YardRotationalAxes == RotationalAxes.Y) ? _currentAngle : 0f,
                     (YardRotationalAxes == RotationalAxes.Z) ? _currentAngle : 0f
                    );
                }

                //gaffs go to the reverse rotation
                for (int i = 0; i < gaffs.Count; i++)
                {
                    gaffs[i].transform.localRotation = Quaternion.Euler(
                    (YardRotationalAxes == RotationalAxes.X) ? -_currentAngle : 0f,
                     (YardRotationalAxes == RotationalAxes.Y) ? -_currentAngle : 0f,
                     (YardRotationalAxes == RotationalAxes.Z) ? -_currentAngle : 0f
                    );
                }

                //if the yard moves, it needs to adjust the sails so we are going to slightly change the windspeed to force the sails to change their scale.
                _windStrength += .00001f;
            }

            if (WindStrength != _windStrength)
            {
                updateRopes = true;

                //shifting the current wind toward the target strength amount
                _windStrength = Mathf.MoveTowards(_windStrength, WindStrength, 2f * Time.deltaTime);

                //Used to make the sails flatten out as they cross the centerline of the ship to keep them from 'snapping' to the other side
                float _strengthExponent = Mathf.Clamp(Mathf.Abs(_currentAngle) / MaxAngle,0.01f,1f);
                  
                //Square sails
                for (int i = 0; i < SquareSails.Count; i++)
                {
                    SquareSails[i].WindPower = Mathf.Clamp(_windStrength, 0.01f, 1f);
                }

                //Jibs
                for (int i = 0; i < Jibs.Count; i++)
                {
                    Jibs[i].WindPower = _strengthExponent * ((_windSide == ShipSide.Stbd) ? _windStrength : -_windStrength);
                }

                //Gaff/lateen/spanker
                for (int i = 0; i < GaffSails.Count; i++)
                {
                    GaffSails[i].WindPower = _strengthExponent * ((_windSide == ShipSide.Stbd) ? _windStrength : -_windStrength);
                }
            }
        }
    }

    struct emissiveMaterial
    {
        public Material material;
        public Color emissiveColor;

        public emissiveMaterial (Material _material, Color _emissiveColor)
        {
            material = _material;
            emissiveColor = _emissiveColor;
        }
    }

    struct lineObject
    {
        public GameObject lineItem;
        public string name;
        public List<string> targets;

        public lineObject (GameObject _lineItem, string _name)
        {
            lineItem = _lineItem;
            name = _name;

            targets = new List<string>();
        }
    }

    class lineRenderer
    {
        public LineRenderer line;
        public Transform root;
        public Transform startTransform;
        public Transform endTransform;
        public int vertices = 7;
        public float slack = 0.05f;


        public lineRenderer (LineRenderer _line, Transform _root, Transform _start, Transform _end, float _slack, float _lineDetail)
        {
            slack = _slack;
            line = _line;
            root = _root;
            startTransform = _start;
            endTransform = _end;
            line.startWidth = line.startWidth * _root.parent.localScale.magnitude;
            line.endWidth = line.endWidth * _root.parent.localScale.magnitude;
            line.transform.localPosition = Vector3.zero;

            if (_slack == 0)
            {
                line.positionCount = 2;
            } else {
                line.positionCount = (int)Mathf.Round(Vector3.Distance(_start.position, _end.position) * _lineDetail + 2f); 
            }
            
            Update();
        }

        public void Update()
        {
            float factor = (float)(line.positionCount - 1);
            for (int i = 0; i < line.positionCount; i++)
            {
                //current position is
                
                var currentPosition = Vector3.Lerp(startTransform.position, endTransform.position, (float)i / factor);
                currentPosition.y -= Mathf.Sin((Mathf.Deg2Rad * 180f) * ((float)i / factor)) * (Vector3.Distance(startTransform.position, endTransform.position) * slack);
                line.SetPosition(i, root.InverseTransformPoint(currentPosition));
                //line.SetPosition(i, currentPosition);
            }
            //line.SetPosition(0, root.InverseTransformPoint((startTransform.position - root.position)));
            //line.SetPosition(1, root.InverseTransformPoint((endTransform.position - root.position)));
        }
    }
}

