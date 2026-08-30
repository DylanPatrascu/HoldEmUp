using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public PokerPosition pokerPosition;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Animator animator;

    [SerializeField] private Texture2D staticTexture;

    [SerializeField] private RuntimeAnimatorController TwoFrameAnimation;

    private Material targetMaterial;

    void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        if (meshRenderer != null)
        {
            targetMaterial = meshRenderer.material;
        }

        SetToStatic();
    }

    public void SetToStatic()
    {
        animator.enabled = false;
        
        // Use "_BaseMap" for URP/HDRP, or "_MainTex" for Built-in standard shaders
        if (targetMaterial.HasProperty("_BaseMap"))
        {
            targetMaterial.SetTexture("_BaseMap", staticTexture);
        }
        else
        {
            targetMaterial.SetTexture("_MainTex", staticTexture);
        }
    }

    public void SetToAnimation()
    {
        animator.runtimeAnimatorController = TwoFrameAnimation;
        animator.enabled = true;
    }

    private void OnDestroy()
    {
        if (targetMaterial != null) Destroy(targetMaterial);
    }

    void Start()
    {
        PokerGameManager.Instance.PerformedGameAction += ShowTells;
    }

    public void ShowTells(object sender, PokerGameManager.PokerEvent e)
    {
        if (e.Player == pokerPosition && e.IsBluffing)
            SetToAnimation();
    }

    public IEnumerator PlayTellAnimation(object sender, EventArgs e)
    {
        SetToAnimation();
        yield return new WaitForSeconds(3);
        SetToStatic();
    }
}
