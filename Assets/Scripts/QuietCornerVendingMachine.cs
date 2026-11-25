using System.Collections;
using UnityEngine;

public sealed class QuietCornerVendingMachine : MonoBehaviour
{
    public Transform drink;
    public Renderer drinkSurface;
    public TextMesh status;
    public bool dispensing { get; private set; }
    public string readyDrink { get; private set; }
    public int collectedDrinks { get; private set; }
    QuietCornerController experience;
    AudioSource chime;
    Vector3 restingPosition;

    void Awake()
    {
        experience = FindFirstObjectByType<QuietCornerController>();
        restingPosition = drink.localPosition;
        drink.gameObject.SetActive(false);
        chime = gameObject.AddComponent<AudioSource>();
        chime.playOnAwake = false;
        chime.volume = .25f;
        chime.spatialBlend = 0;
        const int rate = 22050;
        var samples = new float[rate / 4];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            samples[i] = Mathf.Sin(t * 2 * Mathf.PI * 660) * Mathf.Exp(-t * 18) * Mathf.Min(1, t * 100);
        }
        chime.clip = AudioClip.Create("Drink ready chime", samples.Length, 1, rate, false);
        chime.clip.SetData(samples, 0);
    }

    public void SelectDrink(string selection)
    {
        if (dispensing) return;
        if (!string.IsNullOrEmpty(readyDrink))
        {
            experience.ShowNotice("Take your drink from the tray first.");
            return;
        }
        StartCoroutine(Dispense(selection == "tea" ? "Tea" : "Water"));
    }

    IEnumerator Dispense(string selection)
    {
        dispensing = true;
        status.text = "PREPARING...";
        drinkSurface.material.color = selection == "Tea" ? new Color(.7f,.4f,.15f) : new Color(.35f,.7f,.85f);
        drink.gameObject.SetActive(true);
        for (float elapsed = 0; elapsed < .65f; elapsed += Time.deltaTime)
        {
            drink.localPosition = restingPosition + Vector3.up * Mathf.Lerp(.22f, 0, elapsed / .65f);
            yield return null;
        }
        drink.localPosition = restingPosition;
        readyDrink = selection;
        dispensing = false;
        status.text = selection.ToUpperInvariant() + " READY\nCLICK TRAY TO TAKE";
        chime.Play();
        experience.ShowNotice(selection + " is ready. Click the tray to take it.");
    }

    public void TakeDrink()
    {
        if (dispensing) return;
        if (string.IsNullOrEmpty(readyDrink))
        {
            experience.ShowNotice("Choose WATER or TEA first.");
            return;
        }
        experience.ShowNotice(readyDrink + " collected. Enjoy your break!");
        collectedDrinks++;
        readyDrink = null;
        drink.gameObject.SetActive(false);
        status.text = "CHOOSE A DRINK";
    }
}
