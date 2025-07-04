using UnityEngine;

public class StatueManager : MonoBehaviour
{
    public StatuePlacement[] statues;

    void Update()
    {
        if (AllStatuesLocked())
        {
            Debug.Log("Puzzle resolvido!");
            // Coloque aqui o que acontecerá quando o puzzle for resolvido
            // Ex: abrir uma porta, tocar som, mostrar painel etc.
        }
    }

    bool AllStatuesLocked()
    {
        foreach (var statue in statues)
        {
            if (!statue.IsLocked())
                return false;
        }
        return true;
    }
}
