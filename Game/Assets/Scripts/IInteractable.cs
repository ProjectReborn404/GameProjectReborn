using UnityEngine;

public interface IInteractable
{
    // Se pasa el interactor por si la implementación lo necesita.
    void Interact(GameObject interactor);
}
