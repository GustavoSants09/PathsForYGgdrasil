using Photon.Realtime;

/// <summary>
/// Interface que define objetos interativos no mundo
/// Qualquer objeto que implementa esta interface pode ser ativado pelo jogador
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Retorna o texto do prompt de interação
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// Verifica se o objeto pode ser interagido no momento
    /// </summary>
    bool CanInteract();

    /// <summary>
    /// Executa a interação com o objeto
    /// </summary>
    /// <param name="player">Jogador que está interagindo</param>
    void Interact(Player player);
}