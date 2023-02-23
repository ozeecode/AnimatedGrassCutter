using System.Collections;
using TMPro;
using UnityEngine;

public class GrassChunk : MonoBehaviour
{
#if UNITY_EDITOR
    public bool showBounds = true;
#endif
    [SerializeField] private GameObject grassPf;
    [SerializeField] private float gridX = 10f;
    [SerializeField] private float gridZ = 10f;
    [SerializeField] private float gridSpacingOffset = 1f;
    [SerializeField] private float randomizer = .1f;
    [SerializeField] private TMP_Text grassCountText;


    private WaitForSeconds waitForSeconds;

    IEnumerator Start()
    {
        waitForSeconds = new WaitForSeconds(2f);
        while (true)
        {
            grassCountText.SetText(string.Format("Grass count:{0}", transform.childCount));
            yield return waitForSeconds;
        }
    }

    public void Generate()
    {
        Clear();
        float x = 0;
        float z = 0;
        while (x <= gridX)
        {

            while (z <= gridZ)
            {
                Vector3 spawnPosition = transform.position;
                spawnPosition += new Vector3(x + Random.Range(-randomizer, randomizer), 0, z + Random.Range(-randomizer, randomizer));
                Instantiate(grassPf, spawnPosition, Quaternion.Euler(0, -90, 0), transform);
                z += gridSpacingOffset;
            }
            z = 0;
            x += gridSpacingOffset;
        }
    }
    public void Clear()
    {
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {

                Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(i).gameObject);

            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (showBounds)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + new Vector3(gridX * .5f, .5f, gridZ * .5f), new Vector3(gridX, 1, gridZ));
        }
    }
#endif
}
