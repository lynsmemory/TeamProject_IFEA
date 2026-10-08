using UnityEngine;

public class StudioDoor : MonoBehaviour
{
    public string label = "Door";
    public float openAngle = -90;
    public float degreesPerSecond = 150;
    public Transform blocker;
    public CharacterController player;
    public bool IsOpen { get; private set; }
    bool moving;
    float angle;
    public void Toggle() { IsOpen = !IsOpen; moving = true; }
    void Update()
    {
        if (!moving) return;
        float next = Mathf.MoveTowards(angle, IsOpen ? openAngle : 0, degreesPerSecond * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0,next,0);
        Physics.SyncTransforms();
        // Stop the leaf at the player rather than pushing them through a wall.
        if (player && blocker)
        {
            Vector3 center = player.transform.TransformPoint(player.center);
            Vector3 half = blocker.GetComponent<BoxCollider>().size * .5f;
            Vector3 local = blocker.InverseTransformPoint(center);
            if (Mathf.Abs(local.x) < half.x + player.radius && Mathf.Abs(local.z) < half.z + player.radius && Mathf.Abs(local.y) < half.y + player.height*.5f)
            { transform.localRotation = Quaternion.Euler(0,angle,0); Physics.SyncTransforms(); return; }
        }
        angle = next;
        moving = !Mathf.Approximately(angle,IsOpen ? openAngle : 0);
    }
}
