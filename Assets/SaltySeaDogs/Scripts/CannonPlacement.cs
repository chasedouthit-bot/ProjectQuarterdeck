using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SaltySeaDogs
{
    public class CannonPlacement : MonoBehaviour {

        [SerializeField]
        GameObject[] CannonLocations;

        [SerializeField]
        GameObject CannonPrefab;

        [SerializeField]
        GameObject[] CannonTipLocations;

        [SerializeField]
        GameObject CannonTipPrefab;

	    // Use this for initialization
	    void Start () {
		    //place cannons and tips at the locators
            for (int i = 0; i < CannonLocations.Length; i++)
            {
                var cannon = Instantiate(CannonPrefab, CannonLocations[i].transform.position, CannonLocations[i].transform.rotation);
                cannon.transform.SetParent(transform);
            }

            for (int i = 0; i < CannonTipLocations.Length; i++)
            {
                var cannon = Instantiate(CannonTipPrefab, CannonTipLocations[i].transform.position, CannonTipLocations[i].transform.rotation);
                cannon.transform.SetParent(transform);
            }
        }
    }

}
