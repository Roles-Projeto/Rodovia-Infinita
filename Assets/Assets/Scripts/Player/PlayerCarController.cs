using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// RODOVIA INFINITA — Integrante 1: carro e movimentação do jogador.
///
/// O carro fica parado no eixo Y (quem "anda" é a rodovia, Integrante 2)
/// e se move apenas para os lados. Dois modos:
///   • Faixas (padrão): ← / → ou A / D trocam de faixa com uma transição suave.
///   • Livre: segurar ← / → ou A / D move o carro continuamente, dentro dos limites.
/// No celular (ou com o mouse), tocar na metade esquerda/direita da tela também move.
///
/// API pública para os outros integrantes:
///   SetControleAtivo(bool)  → Game Over / tela inicial (Integrantes 4 e 6)
///   ResetarCarro(bool)      → reinício da partida (Integrante 6)
///   FaixaAtual, QuantidadeFaixas, PosicaoXDaFaixa(int) → obstáculos por faixa (Integrante 3)
///   AoMudarDeFaixa          → evento para som/efeito (opcional)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerCarController : MonoBehaviour
{
    [Header("Modo de movimento")]
    [Tooltip("Ligado: o carro anda por faixas. Desligado: movimento lateral livre.")]
    public bool usarFaixas = true;

    [Tooltip("Se falso, o carro começa parado (útil quando o jogo abre numa tela inicial).")]
    public bool comecarAtivo = true;

    [Header("Faixas (combinar com o Integrante 2)")]
    [Min(1)] public int quantidadeFaixas = 3;
    [Tooltip("Largura de cada faixa, em unidades do Unity.")]
    [Min(0.1f)] public float larguraFaixa = 2f;
    [Tooltip("Posição X do centro da rodovia.")]
    public float centroDaPistaX = 0f;
    [Tooltip("Faixa onde o carro começa (0 = mais à esquerda).")]
    public int faixaInicial = 1;
    [Tooltip("Tempo aproximado (s) para completar a troca de faixa.")]
    [Min(0.01f)] public float tempoTrocaFaixa = 0.12f;

    [Header("Movimento livre")]
    [Tooltip("Velocidade lateral em unidades por segundo.")]
    [Min(0.1f)] public float velocidadeLateral = 8f;
    public float limiteEsquerdo = -3f;
    public float limiteDireito = 3f;

    [Header("Visual")]
    [Tooltip("Inclinação máxima (graus) do carro ao virar.")]
    [Range(0f, 45f)] public float inclinacaoMaxima = 15f;
    [Min(0.1f)] public float suavidadeInclinacao = 10f;

    [Header("Toque / mouse")]
    [Tooltip("Tocar ou clicar na metade esquerda/direita da tela move o carro.")]
    public bool permitirToque = true;

    /// <summary>Disparado quando o carro muda de faixa (recebe o índice da nova faixa).</summary>
    public event Action<int> AoMudarDeFaixa;

    public bool ControleAtivo { get; private set; }
    public int FaixaAtual => faixaAtual;
    public int QuantidadeFaixas => quantidadeFaixas;

    private Rigidbody2D rb;
    private Vector2 posicaoInicial;
    private int faixaAtual;
    private float alvoX;
    private float velocidadeSuavizacao;
    private float entradaHorizontal;

    // ------------------------------------------------------------------ Unity

    private void Reset()
    {
        // Chamado pelo Editor quando o componente é adicionado.
        gameObject.tag = "Player";
        var colisor = GetComponent<BoxCollider2D>();
        if (colisor != null) colisor.isTrigger = true;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;  // movido por código, sem gravidade
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = false;

        posicaoInicial = transform.position;

        if (!CompareTag("Player"))
            Debug.LogWarning("[PlayerCarController] O carro deveria ter a tag \"Player\" (usada na colisão).", this);
    }

    private void Start()
    {
        ResetarCarro(comecarAtivo);
    }

    private void Update()
    {
        entradaHorizontal = 0f;
        if (!ControleAtivo) return;

        if (usarFaixas)
        {
            int direcao = 0;
            if (EsquerdaPressionada()) direcao -= 1;
            if (DireitaPressionada()) direcao += 1;

            if (direcao == 0 && permitirToque && LerToque(out float toqueX, out bool comecouAgora) && comecouAgora)
                direcao = toqueX < Screen.width * 0.5f ? -1 : 1;

            if (direcao != 0) MudarFaixa(direcao);
        }
        else
        {
            entradaHorizontal = EixoHorizontal();

            if (Mathf.Approximately(entradaHorizontal, 0f) && permitirToque && LerToque(out float toqueX, out _))
                entradaHorizontal = toqueX < Screen.width * 0.5f ? -1f : 1f;
        }
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float xAtual = rb.position.x;
        float novoX;

        if (usarFaixas)
        {
            novoX = Mathf.SmoothDamp(xAtual, alvoX, ref velocidadeSuavizacao, tempoTrocaFaixa, Mathf.Infinity, dt);
        }
        else
        {
            novoX = xAtual + entradaHorizontal * velocidadeLateral * dt;
            novoX = Mathf.Clamp(novoX, Mathf.Min(limiteEsquerdo, limiteDireito), Mathf.Max(limiteEsquerdo, limiteDireito));
        }

        rb.MovePosition(new Vector2(novoX, rb.position.y));

        // Inclina o carro na direção do movimento (direita = sentido horário = ângulo negativo).
        float velocidadeX = (novoX - xAtual) / dt;
        float fator = Mathf.Clamp(velocidadeX / velocidadeLateral, -1f, 1f);
        float anguloAlvo = -fator * inclinacaoMaxima;
        rb.MoveRotation(Mathf.LerpAngle(rb.rotation, anguloAlvo, suavidadeInclinacao * dt));
    }

    private void OnValidate()
    {
        faixaInicial = Mathf.Clamp(faixaInicial, 0, Mathf.Max(0, quantidadeFaixas - 1));
    }

    // ------------------------------------------------------- API pública

    /// <summary>Liga/desliga o controle do jogador (ex.: Game Over, pausa, tela inicial).</summary>
    public void SetControleAtivo(bool ativo)
    {
        ControleAtivo = ativo;
        if (!ativo)
        {
            entradaHorizontal = 0f;
            velocidadeSuavizacao = 0f;
        }
    }

    /// <summary>Volta o carro para a posição e faixa iniciais (usado no reinício).</summary>
    public void ResetarCarro(bool ativarControle = true)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        faixaAtual = Mathf.Clamp(faixaInicial, 0, quantidadeFaixas - 1);
        alvoX = usarFaixas ? PosicaoXDaFaixa(faixaAtual) : posicaoInicial.x;
        velocidadeSuavizacao = 0f;
        entradaHorizontal = 0f;

        var pos = new Vector2(alvoX, posicaoInicial.y);
        rb.position = pos;
        rb.rotation = 0f;
        transform.SetPositionAndRotation(new Vector3(pos.x, pos.y, transform.position.z), Quaternion.identity);

        SetControleAtivo(ativarControle);
    }

    /// <summary>Move uma faixa para a esquerda (-1) ou direita (+1). Pode ser chamado por botões de UI.</summary>
    public void MudarFaixa(int direcao)
    {
        if (!usarFaixas || !ControleAtivo || direcao == 0) return;

        int novaFaixa = Mathf.Clamp(faixaAtual + (direcao > 0 ? 1 : -1), 0, quantidadeFaixas - 1);
        if (novaFaixa == faixaAtual) return;

        faixaAtual = novaFaixa;
        alvoX = PosicaoXDaFaixa(faixaAtual);
        AoMudarDeFaixa?.Invoke(faixaAtual);
    }

    /// <summary>Posição X do centro de uma faixa (0 = mais à esquerda).</summary>
    public float PosicaoXDaFaixa(int indice)
    {
        return centroDaPistaX + (indice - (quantidadeFaixas - 1) * 0.5f) * larguraFaixa;
    }

    // ---------------------------------------------------------- Entrada
    // Funciona tanto com o Input Manager antigo quanto com o novo Input System.

    private static bool EsquerdaPressionada()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    private static bool DireitaPressionada()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    private static float EixoHorizontal()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        float valor = 0f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) valor -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) valor += 1f;
        return valor;
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return 0f;
        float valor = 0f;
        if (k.leftArrowKey.isPressed || k.aKey.isPressed) valor -= 1f;
        if (k.rightArrowKey.isPressed || k.dKey.isPressed) valor += 1f;
        return valor;
#else
        return 0f;
#endif
    }

    /// <summary>Lê o primeiro toque (ou o botão esquerdo do mouse). Retorna false se nada estiver pressionado.</summary>
    private static bool LerToque(out float posicaoX, out bool comecouAgora)
    {
        posicaoX = 0f;
        comecouAgora = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            posicaoX = t.position.x;
            comecouAgora = t.phase == TouchPhase.Began;
            return true;
        }
        if (Input.GetMouseButton(0))
        {
            posicaoX = Input.mousePosition.x;
            comecouAgora = Input.GetMouseButtonDown(0);
            return true;
        }
#elif ENABLE_INPUT_SYSTEM
        var tela = Touchscreen.current;
        if (tela != null && tela.primaryTouch.press.isPressed)
        {
            posicaoX = tela.primaryTouch.position.ReadValue().x;
            comecouAgora = tela.primaryTouch.press.wasPressedThisFrame;
            return true;
        }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            posicaoX = mouse.position.ReadValue().x;
            comecouAgora = mouse.leftButton.wasPressedThisFrame;
            return true;
        }
#endif
        return false;
    }

    // ----------------------------------------------------------- Gizmos

    private void OnDrawGizmos()
    {
        // Desenha as faixas na Scene para facilitar o alinhamento com a rodovia.
        float y = Application.isPlaying ? posicaoInicial.y : transform.position.y;
        if (usarFaixas)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < quantidadeFaixas; i++)
            {
                float x = PosicaoXDaFaixa(i);
                Gizmos.DrawLine(new Vector3(x, y - 5f), new Vector3(x, y + 5f));
            }
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(limiteEsquerdo, y - 5f), new Vector3(limiteEsquerdo, y + 5f));
            Gizmos.DrawLine(new Vector3(limiteDireito, y - 5f), new Vector3(limiteDireito, y + 5f));
        }
    }
}
