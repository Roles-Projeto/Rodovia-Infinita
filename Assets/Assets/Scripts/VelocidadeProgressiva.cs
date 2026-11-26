using UnityEngine;

/// <summary>
/// Integrante 5: aumenta a velocidade da estrada com o tempo de sobrevivência.
/// Vai no GameObject "GameManager". Arraste o "EstradaBackground" para o campo Estrada.
/// Não altera o script do Integrante 2: só muda o valor de "velocidade" do BackgroundController.
/// </summary>
public class VelocidadeProgressiva : MonoBehaviour
{
    public static VelocidadeProgressiva Instance { get; private set; }

    [Header("Arraste o EstradaBackground da Hierarchy")]
    [SerializeField] private BackgroundController estrada;

    [Header("Ajustes")]
    [Tooltip("Quanto a velocidade aumenta a cada segundo.")]
    [SerializeField] private float aumentoPorSegundo = 0.05f;

    [Tooltip("Velocidade máxima da estrada.")]
    [SerializeField] private float velocidadeMaxima = 3f;

    private float velocidadeInicial;

    /// <summary>1 no começo e cresce com o tempo. Obstáculos e pontuação podem usar.</summary>
    public float Multiplicador
    {
        get
        {
            if (estrada == null || velocidadeInicial <= 0f) return 1f;
            return estrada.velocidade / velocidadeInicial;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (estrada == null)
        {
            Debug.LogWarning("VelocidadeProgressiva: arraste o EstradaBackground para o campo Estrada.");
            enabled = false;
            return;
        }

        velocidadeInicial = estrada.velocidade;
    }

    private void OnEnable()
    {
        GameManager.OnJogoReiniciado += Reiniciar;
    }

    private void OnDisable()
    {
        GameManager.OnJogoReiniciado -= Reiniciar;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.JogoEncerrado) return;

        estrada.velocidade = Mathf.Min(
            estrada.velocidade + aumentoPorSegundo * Time.deltaTime,
            velocidadeMaxima);
    }

    private void Reiniciar()
    {
        if (estrada != null)
            estrada.velocidade = velocidadeInicial;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
