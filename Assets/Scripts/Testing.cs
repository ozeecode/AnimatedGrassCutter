using UnityEngine;

public class Testing : MonoBehaviour
{


    public float speed = .1f;
    public Rigidbody rb;
    Vector3 movement;
    public ParticleSystem particle;
    public Joystick joystick;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Grass"))
        {
            particle.transform.position = other.transform.position;
            particle.Play();
            Destroy(other.gameObject);
        }
    }
    private void Update()
    {
#if UNITY_EDITOR
        movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
#else
        movement = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
#endif
        movement.Normalize();
    }
    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + movement * speed);
    }
}
