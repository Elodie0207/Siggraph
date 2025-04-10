using UnityEngine;
using TMPro;

public class BPMManager : MonoBehaviour
{
    public Camera playerCamera;
    public TMP_Text bpmText;

    [Header("BPM Settings")]
    public int bpmRepos = 60;
    public int bpmStress = 120;
    public float changementVitesse = 30f; // vitesse à laquelle le BPM s'ajuste

    public float detectionAngle = 60f;
    public float detectionDistance = 20f;
    public string tagAraignee = "Araignee";

    public LayerMask obstacleMask;


    private float bpmActuel;

    void Start()
    {
        bpmActuel = bpmRepos;
    }

    void Update()
    {
        bool araigneeVue = EstAraigneeVisible();

        float bpmCible = araigneeVue ? bpmStress : bpmRepos;
        bpmActuel = Mathf.MoveTowards(bpmActuel, bpmCible, changementVitesse * Time.deltaTime);

        bpmText.text = $"BPM : {Mathf.RoundToInt(bpmActuel)}\nVue: {EstAraigneeVisible()}";

    }

    bool EstAraigneeVisible()
    {
        GameObject[] araignees = GameObject.FindGameObjectsWithTag(tagAraignee);

        foreach (GameObject araignee in araignees)
        {
            Vector3 direction = araignee.transform.position - playerCamera.transform.position;
            float distance = direction.magnitude;
            float angle = Vector3.Angle(playerCamera.transform.forward, direction);

            if (angle < detectionAngle / 2f && distance < detectionDistance)
            {
                Vector3 viewportPoint = playerCamera.WorldToViewportPoint(araignee.transform.position);
                if (viewportPoint.z > 0 && viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1)
                {
                    Ray ray = new Ray(playerCamera.transform.position, direction.normalized);

                    // ➤ 1er raycast : détecte si on touche l’araignée (sans mask)
                    if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
                    {
                        if (hit.collider.gameObject == araignee)
                        {
                            // ➤ 2e raycast : vérifie qu’aucun obstacle ne bloque
                            if (!Physics.Raycast(ray, out RaycastHit block, distance, obstacleMask))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }



}
