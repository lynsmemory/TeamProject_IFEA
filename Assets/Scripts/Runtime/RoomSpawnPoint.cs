using UnityEngine;

// 방 Prefab 안에서 Player 발 위치와 바라볼 방향을 표시하는 컴포넌트이다.
public class RoomSpawnPoint : MonoBehaviour
{
    // RoomController가 Player를 배치할 월드 좌표를 얻도록 반환한다.
    public Vector3 GetPosition() { return transform.position; }
    // RoomController가 시작 시 바라볼 수평 방향을 얻도록 반환한다.
    public Quaternion GetRotation() { return Quaternion.Euler(0f, transform.eulerAngles.y, 0f); }
}
