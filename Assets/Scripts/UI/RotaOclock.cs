using UnityEngine;

public class RotaOclock : MonoBehaviour
{
    public float rotationSpeed = 2f;

    void Update()
    {
        // Xoay quanh trục Z với giá trị âm để đi theo chiều kim đồng hồ
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}
