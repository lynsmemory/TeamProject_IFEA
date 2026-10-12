using UnityEngine;

// 이전 파일 참조를 유지하는 예비 컴포넌트이다. 실제 이동은 PlayerInteraction이 담당한다.
public class FirstPersonController : MonoBehaviour
{
    // 실수로 연결되어도 이동 입력이 중복되지 않도록 경고하고 자신을 비활성화한다.
    private void Awake()
    {
        Debug.LogWarning("Movement uses PlayerInteraction. Remove FirstPersonController.", this);
        enabled = false;
    }
}
