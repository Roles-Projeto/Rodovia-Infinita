using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Integrante 5: pontuação do jogo. Sobe enquanto o jogador sobrevive (mais rápido
/// quanto maior a velocidade), para no Game Over e zera ao reiniciar.
/// Cria o próprio HUD (canto superior esquerdo) e guarda o recorde no computador.
/// Dispara o evento OnMarco a cada X pontos (500, 1000, 1500...), usado para trocar de pista.
/// Vai no GameObject "GameManager".
/// </summary>
public class Pontuacao : MonoBehaviour
{
    public static Pontuacao Instance { get; private set; }

    /// <summary>Disparado ao passar de 500, 1000, 1500... (envia o valor do marco).</summary>
    public static event Action<int> OnMarco;

    [Header("Ajustes")]
    [Tooltip("Pontos por segundo na velocidade inicial.")]
    [SerializeField] private float pontosPorSegundo = 10f;

    [Tooltip("A cada quantos pontos acontece um marco (troca de pista). Padrão: 500.")]
    [SerializeField] private int pontosPorMarco = 500;

    private const string ChaveRecorde = "RodoviaInfinita_Recorde";

    private float pontosExatos;
    private int ultimoMarco;
    private int ultimoValorMostrado = -1;
    private Text txtPontos;
    private Text txtRecorde;
    private Font fonte;

    public int Pontos => Mathf.FloorToInt(pontosExatos);
    public int Recorde { get; private set; }
    public bool NovoRecorde { get; private set; }

    private void Awake()
    {
        Instance = this;
        Recorde = PlayerPrefs.GetInt(ChaveRecorde, 0);
        fonte = Font.CreateDynamicFontFromOSFont(new[] { "Impact", "Arial Black", "Arial" }, 64);
        ConstruirHud();
        AtualizarTextos();
    }

    private void OnEnable()
    {
        GameManager.OnGameOver += AoTerminar;
        GameManager.OnJogoReiniciado += Zerar;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= AoTerminar;
        GameManager.OnJogoReiniciado -= Zerar;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.JogoEncerrado) return;

        float multiplicador = VelocidadeProgressiva.Instance != null
            ? VelocidadeProgressiva.Instance.Multiplicador
            : 1f;

        pontosExatos += pontosPorSegundo * multiplicador * Time.deltaTime;

        if (Pontos != ultimoValorMostrado)
            AtualizarTextos();

        VerificarMarco();
    }

    private void VerificarMarco()
    {
        if (pontosPorMarco <= 0) return;

        int marcoAtual = Pontos / pontosPorMarco;
        if (marcoAtual > ultimoMarco)
        {
            ultimoMarco = marcoAtual;
            OnMarco?.Invoke(marcoAtual * pontosPorMarco);
        }
    }

    private void AoTerminar()
    {
        NovoRecorde = Pontos > Recorde;

        if (NovoRecorde)
        {
            Recorde = Pontos;
            PlayerPrefs.SetInt(ChaveRecorde, Recorde);
            PlayerPrefs.Save();
        }

        AtualizarTextos();
    }

    private void Zerar()
    {
        pontosExatos = 0f;
        ultimoMarco = 0;
        NovoRecorde = false;
        AtualizarTextos();
    }

    private void AtualizarTextos()
    {
        ultimoValorMostrado = Pontos;
        txtPontos.text = "PONTOS  " + Pontos.ToString("D6");
        txtRecorde.text = "RECORDE  " + Recorde.ToString("D6");
    }

    // ------------------------------------------------------------------- HUD

    private void ConstruirHud()
    {
        var canvasGo = new GameObject("PontuacaoCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // abaixo da tela de Game Over (100)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        txtPontos = CriarTexto("Pontos", canvasGo.transform, 64, Color.white, new Vector2(40f, -30f));
        txtRecorde = CriarTexto("Recorde", canvasGo.transform, 36, new Color(1f, 0.85f, 0.3f, 1f), new Vector2(40f, -105f));
    }

    private Text CriarTexto(string nome, Transform pai, int tamanho, Color cor, Vector2 posicao)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        var t = go.AddComponent<Text>();
        t.font = fonte;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        var contorno = go.AddComponent<Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = new Vector2(3f, -3f);

        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(900f, 80f);

        return t;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}