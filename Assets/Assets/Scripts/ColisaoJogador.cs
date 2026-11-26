using UnityEngine;

/// <summary>
/// Integrante 4: detecta a batida do carro em um obstáculo.
/// Vai no GameObject "player_car" (que precisa ter Collider2D e Rigidbody2D).
/// Funciona tanto com obstáculo "Is Trigger" quanto com colisão normal.
/// </summary>
public class ColisaoJogador : MonoBehaviour
{
    [SerializeField] private string tagObstaculo = "Obstaculo";

    private void Start()
    {
        // Avisa o GameManager qual é o carro do jogador.
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarCarro(GetComponent<PlayerCarController>());
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        Verificar(outro.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        Verificar(colisao.gameObject);
    }

    private void Verificar(GameObject outro)
    {
        if (!outro.CompareTag(tagObstaculo)) return;

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
        else
            Debug.LogWarning("GameManager não encontrado na cena.");
    }
}
