using System;
using UnityEngine;

/// <summary>
/// Integrante 4: controla o estado do jogo (rodando / Game Over).
/// Fica em um GameObject vazio chamado "GameManager" na cena.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Outros integrantes podem assinar estes eventos (pontuação, obstáculos, UI).
    public static event Action OnGameOver;
    public static event Action OnJogoReiniciado;

    [Header("Opcional: painel de Game Over (Integrante 6 pode ligar depois)")]
    [SerializeField] private GameObject painelGameOver;

    private PlayerCarController carro;

    public bool JogoEncerrado { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (painelGameOver != null)
            painelGameOver.SetActive(false);
    }

    /// <summary>O carro se registra sozinho (feito pelo ColisaoJogador).</summary>
    public void RegistrarCarro(PlayerCarController novoCarro)
    {
        carro = novoCarro;
    }

    /// <summary>Chamado quando o carro bate em um obstáculo.</summary>
    public void GameOver()
    {
        if (JogoEncerrado) return;
        JogoEncerrado = true;

        if (carro != null)
            carro.SetControleAtivo(false);

        if (painelGameOver != null)
            painelGameOver.SetActive(true);

        Debug.Log("GAME OVER");
        OnGameOver?.Invoke();

        // Congela o jogo (estrada, obstáculos, pontuação).
        Time.timeScale = 0f;
    }

    /// <summary>Usado pelo botão "Reiniciar" (Integrante 6).</summary>
    public void ReiniciarJogo()
    {
        Time.timeScale = 1f;
        JogoEncerrado = false;

        if (painelGameOver != null)
            painelGameOver.SetActive(false);

        if (carro != null)
        {
            carro.ResetarCarro();
            carro.SetControleAtivo(true);
        }

        OnJogoReiniciado?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }
}
