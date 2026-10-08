using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class StudioWalkthrough : MonoBehaviour
{
    public Camera view;
    public GameObject[] interiorCeilings;
    public float speed = 2.2f;
    public float sensitivity = .09f;
    CharacterController controller;
    float pitch, vertical;
    StudioDoor focused;
    int ignoreMouseFrames;
    void Awake() { controller = GetComponent<CharacterController>(); }
    void Start() { foreach(var ceiling in interiorCeilings) if(ceiling) ceiling.SetActive(true); Lock(true); }
    void OnDisable() { Lock(false); }
    void OnApplicationFocus(bool focus) { if (!focus) Lock(false); }
    void Lock(bool value) { Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !value; if(value) ignoreMouseFrames=2; }
    void Update()
    {
        var keys = Keyboard.current; var mouse = Mouse.current;
        if (keys == null || mouse == null) return;
        if (keys.escapeKey.wasPressedThisFrame) Lock(false);
        if (Cursor.lockState != CursorLockMode.Locked) { if(mouse.leftButton.wasPressedThisFrame) Lock(true); return; }
        Vector2 delta = mouse.delta.ReadValue() * sensitivity;
        if(ignoreMouseFrames>0) { delta=Vector2.zero; ignoreMouseFrames--; }
        transform.Rotate(0, delta.x, 0);
        pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
        view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        Vector2 move = new Vector2((keys.dKey.isPressed?1:0)-(keys.aKey.isPressed?1:0), (keys.wKey.isPressed?1:0)-(keys.sKey.isPressed?1:0));
        move = Vector2.ClampMagnitude(move,1);
        if(controller.isGrounded && vertical < 0) vertical = -2;
        vertical += Physics.gravity.y * Time.deltaTime;
        controller.Move(((transform.right * move.x + transform.forward * move.y) * speed + Vector3.up * vertical) * Time.deltaTime);
        focused = null;
        if (Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 2.4f, ~0, QueryTriggerInteraction.Ignore)) focused = hit.collider.GetComponentInParent<StudioDoor>();
        if (focused && keys.eKey.wasPressedThisFrame) focused.Toggle();
    }
    void OnGUI()
    {
        GUI.Label(new Rect(Screen.width/2f-5,Screen.height/2f-12,20,25),"+");
        GUI.Box(new Rect(12,12,355,30),"WASD: Move   Mouse: Look   E: Door   Esc: Cursor");
        if (focused) GUI.Box(new Rect(Screen.width/2f-100,Screen.height/2f+30,200,30),"E: " + (focused.IsOpen ? "Close " : "Open ") + focused.label);
    }
}


