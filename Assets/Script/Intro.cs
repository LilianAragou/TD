using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroFade : MonoBehaviour
{
    public CanvasGroup cg;
    public SpriteRenderer introImage;
    public TextMeshProUGUI introText;
    public GameObject menuPanel;

    public float fadeTime = 1.5f;
    public float displayTime = 2f;

    private float timer = 0f;
    private int part = 0;

    void Start()
    {
        menuPanel.SetActive(false);
        SetAlpha(0f);
    }

    void Update()
    {
        timer += Time.deltaTime;

        switch (part)
        {
            case 0:
                SetAlpha(timer / fadeTime);
                if (timer >= fadeTime)
                {
                    timer = 0;
                    part = 1;
                }
                break;

            case 1:
                if (timer >= displayTime)
                {
                    timer = 0;
                    part = 2;
                }
                break;

            case 2:
                SetAlpha(1 - (timer / fadeTime));
                if (timer >= fadeTime)
                {
                    introImage.gameObject.SetActive(false);
                    introText.gameObject.SetActive(false);
                    menuPanel.SetActive(true);
                    this.enabled = false;
                }
                break;
        }

        if (Input.anyKeyDown)
        {
            introImage.gameObject.SetActive(false);
            introText.gameObject.SetActive(false);
            menuPanel.SetActive(true);
            this.enabled = false;
        }
    }

    void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        if (cg != null&&part==2) cg.alpha = alpha;
        introImage.color = new Color(1, 1, 1, alpha);
        introText.color = new Color(1, 1, 1, alpha);
    }
}
