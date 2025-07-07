using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafOffScreenResetter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform leafTransform;
    [SerializeField] private Vector3 resetPosition = Vector3.zero;
    private Renderer leafRenderer;
    private bool waitingToResetLeaf = false;

    void Start()
    {
        leafRenderer = leafTransform.GetComponent<Renderer>();
    }

    private void Update()
    {
        if (!waitingToResetLeaf && !leafRenderer.isVisible)
        {
            StartCoroutine(ResetPositionAfterDelay());
        }
    }

    private IEnumerator ResetPositionAfterDelay()
    {
        waitingToResetLeaf = true;

        yield return new WaitForSeconds(2);

        waitingToResetLeaf = false;

        // Check again, if still not visible, reset position
        if (!leafRenderer.isVisible)
        {
            leafTransform.position = resetPosition;
        }
    }
}
