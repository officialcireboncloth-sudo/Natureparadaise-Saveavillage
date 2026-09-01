using UnityEngine;

/// <summary>Menyimpan gold/points global dan menyediakan operasi tambah serta belanja yang tervalidasi.</summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public int points;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>Menambahkan gold/points ke saldo global.</summary>
    public void AddPoints(int amount)
    {
        points += amount;
        Debug.Log($"[SCORE] +{amount}. Total = {points}");
    }

    /// <summary>Mengurangi saldo hanya jika jumlah valid dan mencukupi.</summary>
    public bool TrySpendPoints(int amount)
    {
        if (amount <= 0)
            return false;

        if (points < amount)
        {
            Debug.Log($"[SCORE] Gold tidak cukup. Butuh {amount}, punya {points}.");
            return false;
        }

        points -= amount;

        Debug.Log($"[SCORE] -{amount}. Total = {points}");

        return true;
    }
}
