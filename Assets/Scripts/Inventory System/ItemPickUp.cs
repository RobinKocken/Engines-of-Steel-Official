using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickUp : MonoBehaviour, IInteractable
{
    public Item item;
    public int currentAmount;
    public Item item2;
    public int currentAmount2;

    public bool destroyThisObject;

    public void Interact(GameManager gameManager)
    {
        PickUp(gameManager);
    }

    void PickUp(GameManager gameManager)
    {
        gameManager.inventoryManager.AddItem(item, currentAmount, -1);

        if(item2 != null)
            gameManager.inventoryManager.AddItem(item, currentAmount2, -1);

        item = null;
        currentAmount = 0;

        item2 = null;
        currentAmount2 = 0;

        if(destroyThisObject)
            Destroy(gameObject);
    }

    public void DestroyItem()
    {
        Destroy(gameObject);
    }
}
