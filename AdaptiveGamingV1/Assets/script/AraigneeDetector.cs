using UnityEngine;
using UnityEngine.UI; // ou TMPro si tu utilises TextMeshPro
using System.Collections.Generic;

public class AraigneeDetector : MonoBehaviour
{
    public Camera playerCamera;
    public Text infoText; // ou public TMP_Text infoText;
    public string tagAraignee = "Araignee";
    public float detectionAngle = 60f; // champ de vision en degrés
    public float detectionDistance = 20f;

    void Update()
    {
        GameObject[] araignees = GameObject.FindGameObjectsWithTag(tagAraignee);
        bool vue = false;

        foreach (GameObject araignee in araignees)
        {
            Vector3 directionToAraignee = araignee.transform.position - playerCamera.transform.position;
            float angle = Vector3.Angle(playerCamera.transform.forward, directionToAraignee);

            // Dans l'angle ET dans la distance
            if (angle < detectionAngle / 2f && directionToAraignee.magnitude < detectionDistance)
            {
                Ray ray = new Ray(playerCamera.transform.position, directionToAraignee);
                if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
                {
                    if (hit.collider.gameObject == araignee)
                    {
                        vue = true;
                        break;
                    }
                }
            }
        }

        if (vue)
        {
            infoText.text = "Une araignée est en vue ! 😱";
        }
        else
        {
            infoText.text = "";
        }
    }
}
