using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Integrante 4: tela de Game Over (estilo arcade), criada por código.
/// Basta adicionar este script no GameObject "GameManager".
/// Aparece sozinha quando o GameManager dispara OnGameOver e some quando o jogo é reiniciado.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private string titulo = "GAME OVER";
    [SerializeField] private string subtitulo = "VOCÊ BATEU!";
    [SerializeField] private string textoBotao = "JOGAR NOVAMENTE";

    private GameObject canvasRaiz;
    private CanvasGroup grupo;
    private RectTransform caixa;
    private Font fonte;
    private Coroutine animacao;

    private void Awake()
    {
        // Fonte do sistema (Impact no Windows; se não existir, usa Arial).
        fonte = Font.CreateDynamicFontFromOSFont(new[] { "Impact", "Arial Black", "Arial" }, 64);
        Construir();
        canvasRaiz.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.OnGameOver += Mostrar;
        GameManager.OnJogoReiniciado += Esconder;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= Mostrar;
        GameManager.OnJogoReiniciado -= Esconder;
    }

    private void Start()
    {
        // O botão precisa de um EventSystem na cena. Cria um só se não existir.
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var modulo = es.AddComponent<InputSystemUIInputModule>();
            modulo.AssignDefaultActions();
        }
    }

    // ---------------------------------------------------------------- Exibição

    private void Mostrar()
    {
        canvasRaiz.SetActive(true);
        if (animacao != null) StopCoroutine(animacao);
        animacao = StartCoroutine(Animar());
    }

    private void Esconder()
    {
        if (animacao != null) StopCoroutine(animacao);
        animacao = null;
        canvasRaiz.SetActive(false);
    }

    private void Reiniciar()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReiniciarJogo();
    }

    // Usa tempo "não escalado", porque o jogo fica congelado (timeScale = 0).
    private IEnumerator Animar()
    {
        const float duracao = 0.6f;
        float t = 0f;
        grupo.alpha = 0f;
        caixa.localScale = Vector3.one * 1.35f;

        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracao);
            float suave = 1f - Mathf.Pow(1f - k, 3f);
            grupo.alpha = suave;
            caixa.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, suave);
            yield return null;
        }

        grupo.alpha = 1f;
        caixa.localScale = Vector3.one;
    }

    // ------------------------------------------------------------ Construção da UI

    private void Construir()
    {
        canvasRaiz = new GameObject("GameOverCanvas");
        canvasRaiz.transform.SetParent(transform, false);

        var canvas = canvasRaiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasRaiz.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRaiz.AddComponent<GraphicRaycaster>();

        // Fundo escurecido com tom avermelhado
        Image fundo = CriarImagem("Fundo", canvasRaiz.transform, new Color(0.04f, 0f, 0f, 0.88f));
        Definir(fundo.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        grupo = fundo.gameObject.AddComponent<CanvasGroup>();

        // Faixa central preta com linhas vermelhas em cima e embaixo
        Image faixa = CriarImagem("Faixa", fundo.transform, new Color(0f, 0f, 0f, 0.65f));
        faixa.raycastTarget = false;
        caixa = faixa.rectTransform;
        Definir(caixa, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 520f));

        Color vermelho = new Color(0.9f, 0.1f, 0.1f, 1f);

        Image linhaTopo = CriarImagem("LinhaTopo", caixa, vermelho);
        linhaTopo.raycastTarget = false;
        Definir(linhaTopo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 6f));
        linhaTopo.rectTransform.pivot = new Vector2(0.5f, 1f);

        Image linhaBase = CriarImagem("LinhaBase", caixa, vermelho);
        linhaBase.raycastTarget = false;
        Definir(linhaBase.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 6f));
        linhaBase.rectTransform.pivot = new Vector2(0.5f, 0f);

        // Título
        Text txtTitulo = CriarTexto("Titulo", caixa, titulo, 170, new Color(1f, 0.15f, 0.15f, 1f));
        Definir(txtTitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1600f, 220f));
        var contorno = txtTitulo.gameObject.AddComponent<Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = new Vector2(5f, -5f);
        var brilho = txtTitulo.gameObject.AddComponent<Shadow>();
        brilho.effectColor = new Color(1f, 0f, 0f, 0.45f);
        brilho.effectDistance = new Vector2(0f, -12f);

        // Subtítulo
        Text txtSub = CriarTexto("Subtitulo", caixa, subtitulo, 46, new Color(0.85f, 0.85f, 0.85f, 1f));
        Definir(txtSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(1200f, 70f));

        // Botão
        Image imgBotao = CriarImagem("Botao", caixa, Color.white);
        Definir(imgBotao.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(560f, 110f));

        var botao = imgBotao.gameObject.AddComponent<Button>();
        botao.targetGraphic = imgBotao;
        var cores = botao.colors;
        cores.normalColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        cores.highlightedColor = new Color(1f, 0.25f, 0.25f, 1f);
        cores.pressedColor = new Color(0.55f, 0f, 0f, 1f);
        cores.selectedColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        cores.fadeDuration = 0.1f;
        botao.colors = cores;
        botao.onClick.AddListener(Reiniciar);

        Text txtBotao = CriarTexto("TextoBotao", imgBotao.transform, textoBotao, 48, Color.white);
        Definir(txtBotao.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private Image CriarImagem(string nome, Transform pai, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var img = go.AddComponent<Image>();
        img.color = cor;
        return img;
    }

    private Text CriarTexto(string nome, Transform pai, string conteudo, int tamanho, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var t = go.AddComponent<Text>();
        t.font = fonte;
        t.text = conteudo;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private void Definir(RectTransform rt, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }
}
