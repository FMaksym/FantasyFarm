using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class PlayerBildPurchase : MonoBehaviour
{
    [SerializeField] private float _radius;

    [Inject] private PlayerTouchMovement _playerMovement;
    [Inject] private Inventory _playerInventory;
    [Inject] private NavMeshManager _navMesh;

    public delegate void BuyBuildEventHandler();
    public static event BuyBuildEventHandler BuyBuild;

    private void FixedUpdate()
    {
        FindBuildPrice();
    }

    private void FindBuildPrice()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radius);
        foreach (var collider in colliders)
        {
            BuildPrice buildPrice = collider.GetComponent<BuildPrice>();
            if (buildPrice != null)
            {
                SpendResources(buildPrice);
            }
        }
    }

    private void SpendResources(BuildPrice buildPrice)
    {
        Dictionary<string, int> newPrice = new Dictionary<string, int>(buildPrice.Price);
        foreach (KeyValuePair<string, int> resource in buildPrice.Price)
        {
            int playerResource = 0;
            if (_playerInventory.ContainsKey(resource.Key))
            {
                playerResource = _playerInventory.GetItem(resource.Key);
                int amountToSpend = Mathf.Min(playerResource, resource.Value);
                _playerInventory.RemoveItem(resource.Key, amountToSpend);
                newPrice[resource.Key] -= amountToSpend;
            }
            DataManager.instance.GameDataChanged();
        }

        buildPrice.Price = newPrice;
        buildPrice.UpdatePrice(newPrice);

        if (buildPrice.Price.Values.OfType<int>().All(value => value == 0))
        {
            buildPrice.BildObject.SetActive(true);
            buildPrice.PriceObject.SetActive(false);
            if (buildPrice.NextBuildObject != null)
            {
                buildPrice.NextBuildObject.SetActive(true);
                buildPrice.NextBuildObject.GetComponent<BuildForPurchaseData>().isActive = true;
                buildPrice.buildForPurchaseData.isPurchased = true;
                buildPrice.buildForPurchaseData.mainObjectActive = true;
                buildPrice.buildForPurchaseData.priceObjectActive = false;
            }

            BuyBuild?.Invoke();
            DataManager.instance.GameDataChanged();
            StartCoroutine(PurchaseBild());
            StartCoroutine(WaitAndMove(1));
        }
    }

    IEnumerator PurchaseBild()
    {
        _playerMovement.Moved(false);
        yield return new WaitForSeconds(2f);
        _navMesh.BakeNavMesh();
    }

    IEnumerator WaitAndMove(float time)
    {

        yield return new WaitForSeconds(time);
        _playerMovement.Moved(true);
    }
}
