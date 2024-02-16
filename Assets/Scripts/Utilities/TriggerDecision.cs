using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TriggerDecision : MonoBehaviour
{
    private GameObject[] products;
    private GameObject purchaseConfirmation;
    private Collider cartBottomCollider;
    bool purchaseDone;

    // Log a decision via collider intersection
    private void Awake()
    {
        // Needs to be done in awake because cart and products are disabled in start
        purchaseDone = false;

        purchaseConfirmation = GameObject.Find("Bought item panel");
        purchaseConfirmation.SetActive(false);

        cartBottomCollider = GameObject.Find("Cart").GetComponent<BoxCollider>();

        products = GameObject.FindGameObjectsWithTag("Product");
    }
    private void Update()
    {
        if (!purchaseDone)
        {
            foreach (GameObject product in products)
            {
                if (product.GetComponent<Collider>().bounds.Intersects(cartBottomCollider.bounds))
                {
                    Log(product);
                }
            }
        }

    }
    private void Log(GameObject product)
    {
        string msg = $"Model purchased: {product.name}";
        Debug.Log(msg);
        purchaseConfirmation.GetComponentInChildren<TextMeshProUGUI>().text = msg;
        purchaseConfirmation.SetActive(true);
        purchaseDone = true;
    }
}
