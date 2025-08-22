using UnityEngine;
using System.Collections;
using static CoreGameDialog;

public class BlendShapeManager : MonoBehaviour
{
    [Header("Target Mesh")]
    [Tooltip("SkinnedMeshRenderer dari karakter yang memiliki blendshape")]
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("BlendShape Index Mapping")]
    [Tooltip("Index untuk blendshape mulut bicara")]
    public int mouthBlendShapeIndex = 1;

    [Tooltip("Index untuk blendshape mata berkedip")]
    public int blinkBlendShapeIndex = 0;

    private Coroutine talkingCoroutine;

    /// <summary>
    /// Dipanggil dari sistem dialog untuk mengaktifkan ekspresi berdasarkan NPC
    /// </summary>
    public void SetExpressionByNpcName(NpcName npc)
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogWarning("SkinnedMeshRenderer belum di-assign!");
            return;
        }

        if (npc == NpcName.Ibu)
        {
            StartTalking();
            StartCoroutine(AutoStopExpression(3f)); // durasi ekspresi aktif
        }
        else
        {
            StopTalking(); // pastikan ekspresi dihentikan untuk NPC lain
        }
    }

    /// <summary>
    /// Hentikan ekspresi setelah durasi tertentu
    /// </summary>
    private IEnumerator AutoStopExpression(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopTalking();
    }

    /// <summary>
    /// Mulai coroutine bicara + kedipan terbatas
    /// </summary>
    public void StartTalking()
    {
        StopTalking(); // pastikan tidak ada coroutine ganda
        talkingCoroutine = StartCoroutine(TalkingRoutine());
        StartCoroutine(LimitedBlinkRoutine()); // 👁️ Kedipan terbatas
    }

    /// <summary>
    /// Hentikan semua ekspresi dan reset blendshape
    /// </summary>
    public void StopTalking()
    {
        if (talkingCoroutine != null)
        {
            StopCoroutine(talkingCoroutine);
            talkingCoroutine = null;
        }

        SetBlendShapeWeightSafe(mouthBlendShapeIndex, 0f); // Tutup mulut
        SetBlendShapeWeightSafe(blinkBlendShapeIndex, 0f); // Mata terbuka
    }

    /// <summary>
    /// Coroutine utama untuk bicara
    /// </summary>
    private IEnumerator TalkingRoutine()
    {
        while (true)
        {
            float mouthWeight = Random.Range(30f, 100f);
            SetBlendShapeWeightSafe(mouthBlendShapeIndex, mouthWeight);

            yield return new WaitForSeconds(Random.Range(0.1f, 0.25f));
        }
    }

    /// <summary>
    /// Kedipan terbatas selama bicara
    /// </summary>
    private IEnumerator LimitedBlinkRoutine()
    {
        int blinkCount = Random.Range(1, 3); // 1 atau 2 kali kedip
        for (int i = 0; i < blinkCount; i++)
        {
            float delay = Random.Range(0.5f, 2f); // jeda sebelum kedip
            yield return new WaitForSeconds(delay);
            yield return StartCoroutine(BlinkOnce());
        }
    }

    /// <summary>
    /// Coroutine satu kali kedipan
    /// </summary>
    private IEnumerator BlinkOnce()
    {
        SetBlendShapeWeightSafe(blinkBlendShapeIndex, 100f); // Mata tertutup
        yield return new WaitForSeconds(0.1f);
        SetBlendShapeWeightSafe(blinkBlendShapeIndex, 0f);   // Mata terbuka
    }

    /// <summary>
    /// Validasi index blendshape sebelum set
    /// </summary>
    private void SetBlendShapeWeightSafe(int index, float weight)
    {
        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
            return;

        int count = skinnedMeshRenderer.sharedMesh.blendShapeCount;
        if (index >= 0 && index < count)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
        }
        else
        {
            Debug.LogWarning($"Blendshape index {index} out of bounds! Mesh only has {count} blendshapes.");
        }
    }
}
