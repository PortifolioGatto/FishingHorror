using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SonarSystem : MonoBehaviour
{
    [SerializeField] private GameObject boatObject;
    [SerializeField] private SonarVisualLine sonarVisualLine;
    [SerializeField] private float scanConversion = 0.16f;
    [SerializeField] private float scanRadius = 0.16f;
    [SerializeField] private float sonarTickRate = 1f;
    [SerializeField] private LayerMask scanLayerMask;
    [SerializeField] private GameObject inRadarObject;
    [SerializeField] private bool onlyInScanLine;

    private List<Collider> detectedObjects = new List<Collider>();


    private void Start()
    {
        StartCoroutine(ESonarHandler());

        InvokeRepeating(nameof(SonarPing), 0f, sonarTickRate * 3);
    }

    private void SonarPing()
    {
        AudioManager.Instance.PlaySFX("sonarping", transform.position, 0.25f, 0f);
    }

    private IEnumerator ESonarHandler()
    {
        while (true)
        {
            Collider[] hits = Physics.OverlapSphere(
                boatObject.transform.position,
                scanRadius * 4f,
                scanLayerMask
            );


            foreach (Collider hit in hits)
            {
                if(detectedObjects.Contains(hit))
                    continue;

                Vector3 targetPos = hit.transform.position;
                targetPos.y = 0f;

                // Converte para espaço local do barco
                Vector3 localPos = boatObject.transform.InverseTransformPoint(targetPos);
                localPos.y = 0f;

                float distance = new Vector2(localPos.x, localPos.z).magnitude;

                if (distance > scanRadius * 4)
                    continue;

                float distancePercent = distance / scanRadius;

                Vector2 dir2D = new Vector2(-localPos.x, localPos.z).normalized;

                Vector2 scanPos = dir2D * distancePercent * scanConversion;

                bool isInScanLine = Vector2.Dot(dir2D, sonarVisualLine.GetScanDirection()) > 0.95f;
                bool isInsideScanRadius = true;

                if (isInScanLine || !onlyInScanLine)
                {
                    if(detectedObjects.Contains(hit))
                        continue;

                    detectedObjects.Add(hit);

                    StartCoroutine(WaitToEvent(() =>
                    {
                        detectedObjects.Remove(hit);
                    }, sonarTickRate * 1f));

                    //Clamp scanPos dentro do círculo
                    if (distance > scanRadius)
                    {
                        scanPos = scanPos.normalized * .9f * scanConversion;
                        isInsideScanRadius = false;
                    }

                    GameObject scaned = Instantiate(inRadarObject, sonarVisualLine.transform);

                    TextMeshPro textMesh = scaned.GetComponentInChildren<TextMeshPro>();

                    textMesh.transform.localScale = new Vector3(-1, 1, 1);

                    textMesh.text = isInsideScanRadius ? Vector3.Distance(boatObject.transform.position, hit.transform.position).ToString("F1") + "m" : "";

                    scaned.transform.localPosition = new Vector3(scanPos.x, scanPos.y, 0f);


                    SpriteRenderer render = scaned.GetComponent<SpriteRenderer>();

                    render.color = new Color(0, 1, 0, 0);

                    render.DOFade(1f, sonarTickRate)
                          .OnComplete(() =>
                          {
                              render.DOFade(0, sonarTickRate)
                                .OnComplete(() => Destroy(scaned));
                          });

                    textMesh.color = new Color(0, 1, 0, 0);

                    textMesh.DOFade(1f, sonarTickRate)
                            .OnComplete(() =>
                            {
                                textMesh.DOFade(0, sonarTickRate);
                            });
                    //textMesh.DOFade(0, sonarTickRate);
                }

                
            }

            UpdateTextureHeight();

            if (onlyInScanLine)
            {

                yield return new WaitForEndOfFrame();
            }
            else
            {
                yield return new WaitForSeconds(sonarTickRate * 2.15f);

            }
        }

    }

    private IEnumerator WaitToEvent(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }


    private void InitializeTexture()
    {
        
    }

    private void UpdateTextureHeight()
    {
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if(boatObject != null)
        {
            Gizmos.DrawWireSphere(boatObject.transform.position, scanRadius);
        }
    }
}
