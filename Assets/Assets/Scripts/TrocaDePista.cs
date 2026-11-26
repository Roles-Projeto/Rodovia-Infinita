using UnityEngine;

/// <summary>
/// Integrante 5: troca de pista automática + parede invisível no canteiro central.
///
/// - O jogador NÃO consegue atravessar o canteiro central por conta própria.
/// - A cada marco de pontos (500, 1000, 1500...) o jogo move o carro para a outra pista
///   (esquerda -> direita -> esquerda...), de forma suave, com leve inclinação.
///
/// Vai no GameObject "player_car" (ao lado do PlayerCarController e do ColisaoJogador).
/// A parede é feita por código (e não por Collider) porque o collider do carro é Trigger,
/// usado para detectar obstáculos, e um Trigger não bloqueia movimento.
/// </summary>
public class TrocaDePista : MonoBehaviour
{
    [Header("Estrada")]
    [Tooltip("X do meio da estrada (o canteiro central). Normalmente 0.")]
    [SerializeField] private float centroDaEstradaX = 0f;

    [Tooltip("Parede invisível: o centro do carro não passa de (centro ± este valor). Aumente se o carro invadir o canteiro.")]
    [SerializeField] private float limiteDoCanteiro = 1.7f;

    [SerializeField] private bool bloquearCanteiro = true;

    [Header("Troca automática")]
    [Tooltip("Duração da troca de pista, em segundos. Maior = mais suave.")]
    [SerializeField] private float duracao = 1.4f;

    [Tooltip("Inclinação máxima do carro durante a troca, em graus.")]
    [SerializeField] private float inclinacaoMaxima = 12f;

    private PlayerCarController carro;
    private bool trocando;
    private float tempo;
    private float xInicial;
    private float xFinal;
    private float yFixo;

    private void Awake()
    {
        carro = GetComponent<PlayerCarController>();
    }

    private void OnEnable()
    {
        Pontuacao.OnMarco += IniciarTroca;
        GameManager.OnGameOver += Cancelar;
        GameManager.OnJogoReiniciado += Cancelar;
    }

    private void OnDisable()
    {
        Pontuacao.OnMarco -= IniciarTroca;
        GameManager.OnGameOver -= Cancelar;
        GameManager.OnJogoReiniciado -= Cancelar;
    }

    private void IniciarTroca(int marco)
    {
        if (trocando) return;
        if (GameManager.Instance != null && GameManager.Instance.JogoEncerrado) return;

        trocando = true;
        tempo = 0f;
        xInicial = transform.position.x;
        yFixo = transform.position.y;

        // Espelha a posição do carro em relação ao canteiro central.
        // Assim ele mantém o mesmo "lugar" dentro da nova pista.
        xFinal = 2f * centroDaEstradaX - xInicial;

        // Se o carro estava exatamente no meio, vai para a direita.
        if (Mathf.Abs(xFinal - xInicial) < 0.5f)
            xFinal = centroDaEstradaX + 2f;

        // Durante a troca o jogador não controla o carro.
        if (carro != null)
            carro.SetControleAtivo(false);
    }

    // LateUpdate roda depois do Update do PlayerCarController,
    // então a posição e a inclinação daqui são as que valem na tela.
    private void LateUpdate()
    {
        if (trocando)
        {
            AnimarTroca();
            return;
        }

        if (bloquearCanteiro)
            LimitarAoCanteiro();
    }

    private void AnimarTroca()
    {
        tempo += Time.deltaTime;
        float k = Mathf.Clamp01(tempo / Mathf.Max(0.01f, duracao));

        // SmoothStep: começa devagar, acelera no meio e desacelera no fim.
        float suave = k * k * (3f - 2f * k);

        Vector3 posicao = transform.position;
        posicao.x = Mathf.Lerp(xInicial, xFinal, suave);
        posicao.y = yFixo;
        transform.position = posicao;

        // Inclina para o lado que está virando e volta a ficar reto no fim.
        float sentido = Mathf.Sign(xFinal - xInicial);
        float angulo = -sentido * inclinacaoMaxima * Mathf.Sin(Mathf.PI * k);
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        if (k >= 1f)
            Terminar();
    }

    // Parede invisível: se o carro entrar na zona do canteiro, é empurrado de volta
    // para o lado da pista em que está.
    private void LimitarAoCanteiro()
    {
        Vector3 posicao = transform.position;
        float distancia = posicao.x - centroDaEstradaX;

        if (Mathf.Abs(distancia) >= limiteDoCanteiro) return;

        posicao.x = centroDaEstradaX + (distancia < 0f ? -limiteDoCanteiro : limiteDoCanteiro);
        transform.position = posicao;
    }

    private void Terminar()
    {
        trocando = false;
        transform.rotation = Quaternion.identity;

        bool acabou = GameManager.Instance != null && GameManager.Instance.JogoEncerrado;
        if (carro != null && !acabou)
            carro.SetControleAtivo(true);
    }

    // Game Over ou reinício no meio da troca: para tudo e deixa o GameManager cuidar do resto.
    private void Cancelar()
    {
        if (!trocando) return;

        trocando = false;
        transform.rotation = Quaternion.identity;
    }
}