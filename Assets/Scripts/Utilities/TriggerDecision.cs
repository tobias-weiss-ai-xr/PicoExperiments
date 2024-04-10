using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class TriggerDecision : MonoBehaviour
{
    private GameObject[] products;
    private GameObject text1, text2;
    private Collider cartBottomCollider;
    bool purchaseDone;

    // Log a decision via collider intersection
    private void Awake()
    {
        // Needs to be done in awake because cart and products are disabled in start
        purchaseDone = false;

        text1 = GameObject.Find("text1");
        text2 = GameObject.Find("text2");
        text2.SetActive(false);

        cartBottomCollider = GameObject.Find("Cart").GetComponentInChildren<BoxCollider>();

        products = GameObject.FindGameObjectsWithTag("ProductBox");
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

        string filePath = Application.persistentDataPath + "/purchase-decision.txt";
        string dt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string msg = $"{dt} Model purchased: {product.name}";

        Debug.Log(msg);
        using (StreamWriter writer = File.AppendText(filePath))
        {
            writer.WriteLine(msg);
        }

        text2.SetActive(true);
        text1.SetActive(false);
        purchaseDone = true;
    }
}
