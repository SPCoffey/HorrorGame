﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Player : MonoBehaviour {

    public GameObject Menu;
    public GameObject Vitals;
    public GameObject Chat;
    public GameObject player;

    [SerializeField]private float runSpeed = 8f;                                       // The speed at which we want the character to move
    [SerializeField]private float strafeSpeed = 4f;                                    // The speed at which we want the character to be able to strafe
    [SerializeField]private float jumpPower = 5f;                                      // The power behind the characters jump. increase for higher jumps
#if !MOBILE_INPUT
    [SerializeField]private bool walkByDefault = true;									// controls how the walk/run modifier key behaves.
    [SerializeField]private float walkSpeed = 3f;                                      // The speed at which we want the character to move
#endif
    private float lastSynchronizationTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition = Vector3.zero;
    private Vector3 syncEndPosition = Vector3.zero;
    private Quaternion syncStartRotation = Quaternion.identity;
    private Quaternion syncEndRotation = Quaternion.identity;
    private Vector3 mousePosPrev;
    public bool grounded { get; private set; }
    private IComparer rayHitComparer;
    private const float jumpRayLength = 0.7f;
    private const float playerReach = 1f;
    private Vector2 input;
    private CapsuleCollider capsule;
    private bool sprinting;

    public GameObject currentWeapon = null;
    public GameObject sheathedWeapon = null;
    public GameObject weaponLoc;
    public GameObject sheathedLoc;
    public float throwForce = 1500;
    private bool charging = false;
    private float percentCharge;
    private const float goalCharge = 0.15f;
    private float startCharge;
    private bool chatting = false;

    [SerializeField]private AdvancedSettings advanced = new AdvancedSettings();
    [System.Serializable]
    public class AdvancedSettings                                                       // The advanced settings
    {
        public float gravityMultiplier = 1f;                                            // Changes the way gravity effect the player ( realistic gravity can look bad for jumping in game )
        public PhysicMaterial zeroFrictionMaterial;                                     // Material used for zero friction simulation
        public PhysicMaterial highFrictionMaterial;                                     // Material used for high friction ( can stop character sliding down slopes )
        public float groundStickyEffect = 5f;											// power of 'stick to ground' effect - prevents bumping down slopes.
    }

    void Awake()
    {
        if (networkView.isMine)
        {
            // Set up a reference to the capsule collider.
            capsule = collider as CapsuleCollider;
            grounded = true;
            Screen.lockCursor = true;
            rayHitComparer = new RayHitComparer();
        }
    }

    void Update()
    {
        if (networkView.isMine)
        {
            MenuInput();
            if (!Menu.activeSelf && !chatting)
            {
                KeyInput();
            }
        }
    }

    void FixedUpdate()
    {
        if (networkView.isMine)
        {
            InputMovement();
        }
        else
        {
            SyncedMovement();
        }
    }

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        Vector3 syncPosition = Vector3.zero;
        Vector3 syncVelocity = Vector3.zero;
        Quaternion syncRotation = Quaternion.identity;
        Vector3 syncAngularVelocity = Vector3.zero;
        if (stream.isWriting)
        {
            syncPosition = rigidbody.position;
            stream.Serialize(ref syncPosition);

            syncVelocity = rigidbody.velocity;
            stream.Serialize(ref syncVelocity);

            syncRotation = rigidbody.rotation;
            stream.Serialize(ref syncRotation);

            syncAngularVelocity = rigidbody.angularVelocity;
            stream.Serialize(ref syncAngularVelocity);
        }
        else
        {
            stream.Serialize(ref syncPosition);
            stream.Serialize(ref syncVelocity);
            stream.Serialize(ref syncRotation);
            stream.Serialize(ref syncAngularVelocity);

            syncTime = 0f;
            syncDelay = Time.time - lastSynchronizationTime;
            lastSynchronizationTime = Time.time;

            syncEndPosition = syncPosition + syncVelocity * syncDelay;
            syncStartPosition = rigidbody.position;
            syncEndRotation = syncRotation * Quaternion.Euler(syncAngularVelocity * syncDelay * Mathf.Rad2Deg);
            syncStartRotation = rigidbody.rotation;
        }
    }

    void InputMovement()
    {
        float speed = runSpeed;

        // Read input
#if CROSS_PLATFORM_INPUT
        float h = CrossPlatformInput.GetAxis("Horizontal");
        float v = CrossPlatformInput.GetAxis("Vertical");
        bool jump = CrossPlatformInput.GetButton("Jump") && transform.GetComponent<Vitals>().CanJump() && grounded;
#else
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		bool jump = Input.GetButton("Jump") && transform.GetComponent<Vitals>().CanJump() && !jumping;
#endif
        if (chatting)
        {
            h = 0;
            v = 0;
            jump = false;
        }
#if !MOBILE_INPUT
        if (jump)
        {
            transform.GetComponent<Vitals>().UseStamina(transform.GetComponent<Vitals>().jumpStamina);
        }

        // On standalone builds, walk/run speed is modified by a key press.
        // We select appropriate speed based on whether we're walking by default, and whether the walk/run toggle button is pressed:
        if (sprinting)
        {
            transform.GetComponent<Vitals>().UseStamina(Time.deltaTime);
        }
        bool walkOrRun = Input.GetKey(KeyCode.LeftShift) && transform.GetComponent<Vitals>().CanRun();
        sprinting = walkOrRun;
        speed = walkByDefault ? (walkOrRun ? runSpeed : walkSpeed) : (walkOrRun ? walkSpeed : runSpeed);

        // On mobile, it's controlled in analogue fashion by the v input value, and therefore needs no special handling.


#endif

        input = new Vector2(h, v);

        // normalize input if it exceeds 1 in combined length:
        if (input.sqrMagnitude > 1) input.Normalize();

        // Get a vector which is desired move as a world-relative direction, including speeds
        Vector3 desiredMove = transform.forward * input.y * speed + transform.right * input.x * strafeSpeed;

        // preserving current y velocity (for falling, gravity)
        float yv = rigidbody.velocity.y;

        // add jump power
        if (grounded && jump)
        {
            yv += jumpPower;
            grounded = false;
        }

        // Set the rigidbody's velocity according to the ground angle and desired move
        rigidbody.velocity = desiredMove + Vector3.up * yv;

        // Use low/high friction depending on whether we're moving or not
        if (desiredMove.magnitude > 0 || !grounded)
        {
            collider.material = advanced.zeroFrictionMaterial;
        }
        else
        {
            collider.material = advanced.highFrictionMaterial;
        }


        // Ground Check:

        // Create a ray that points down from the centre of the character.
        Ray ray = new Ray(transform.position, -transform.up);

        // Raycast slightly further than the capsule (as determined by jumpRayLength)
        RaycastHit[] hits = Physics.RaycastAll(ray, capsule.height * jumpRayLength);
        System.Array.Sort(hits, rayHitComparer);


        if (grounded || rigidbody.velocity.y < jumpPower * .5f)
        {
            // Default value if nothing is detected:
            grounded = false;
            // Check every collider hit by the ray
            for (int i = 0; i < hits.Length; i++)
            {
                // Check it's not a trigger
                if (!hits[i].collider.isTrigger)
                {
                    // The character is grounded, and we store the ground angle (calculated from the normal)
                    grounded = true;

                    // stick to surface - helps character stick to ground - specially when running down slopes
                    //if (rigidbody.velocity.y <= 0) {
                    rigidbody.position = Vector3.MoveTowards(rigidbody.position, hits[i].point + Vector3.up * capsule.height * .5f, Time.deltaTime * advanced.groundStickyEffect);
                    //}
                    rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);
                    break;
                }
            }
        }

        Debug.DrawRay(ray.origin, ray.direction * capsule.height * jumpRayLength, grounded ? Color.green : Color.red);


        // add extra gravity
        rigidbody.AddForce(Physics.gravity * (advanced.gravityMultiplier - 1));
    }

    // Smoothly moves networked bodies.
    private void SyncedMovement()
    {
        syncTime += Time.deltaTime;
        rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
        rigidbody.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
    }

    private void MenuInput()
    {
        // Menu Options
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
        if (Input.GetKeyUp(KeyCode.Return) && !Menu.activeSelf)
        {
            GetComponent<ChatScript>().SendChatMessage();
            ToggleChat();
        }
    }

    private void KeyInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && (currentWeapon == null || sheathedWeapon == null))
        {
            PickupItem();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwapWeapons();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            DropWeapon();
        }
        if (Input.GetMouseButtonDown(0))
        {
            SwingWeapon();
        }
        if (Input.GetMouseButtonDown(1))
        {
            ChargeWeapon();
        }
        if (Input.GetMouseButtonUp(1))
        {
            ThrowWeapon();
        }
        if (charging && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default"))
        {
            float time = Time.time - startCharge;
            percentCharge = time / goalCharge;
            currentWeapon.transform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.back * 0.06f, percentCharge);
            if (percentCharge >= 1.0f)
            {
                ThrowWeapon();
            }
        }
        else
        {
            charging = false;
        }
    }

    private void PickupItem()
    {
        Ray ray = new Ray(transform.FindChild("Player Camera").position, transform.FindChild("Player Camera").forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, capsule.height * playerReach);
        System.Array.Sort(hits, rayHitComparer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.gameObject.tag == "Item" && !hits[i].transform.GetComponent<Weapon>().isEquipped)
            {
                networkView.RPC("SyncPickup", RPCMode.OthersBuffered, hits[i].transform.networkView.viewID);
                hits[i].transform.GetComponent<Weapon>().isEquipped = true;
                hits[i].transform.GetComponent<Weapon>().isStuck = false;
                hits[i].transform.GetComponent<Weapon>().equippedTo = gameObject;
                if (currentWeapon == null)
                {
                    SetCurrentWeapon(hits[i].collider.gameObject);
                }
                else
                {
                    SetSheathedWeapon(hits[i].collider.gameObject);
                }
                return;
            }
        }
    }

    public void SetCurrentWeapon(GameObject weapon)
    {
        currentWeapon = weapon;
        if (weapon != null)
        {
            weapon.collider.enabled = false;
            weapon.transform.SetParent(weaponLoc.transform);
            weapon.GetComponent<Weapon>().LerpTo(Vector3.zero, Quaternion.identity, 0.3f);
            weapon.rigidbody.isKinematic = true;
        }
    }

    public void SetSheathedWeapon(GameObject weapon)
    {
        sheathedWeapon = weapon;
        if (weapon != null)
        {
            weapon.collider.enabled = false;
            weapon.transform.SetParent(sheathedLoc.transform);
            weapon.GetComponent<Weapon>().LerpTo(Vector3.zero, Quaternion.identity, 0.3f);
            weapon.rigidbody.isKinematic = true;
        }
    }

    public void SwapWeapons()
    {
        if (sheathedWeapon != null && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default"))
        {
            networkView.RPC("SyncSwap", RPCMode.OthersBuffered);
            GameObject tempWeapon = currentWeapon;
            SetCurrentWeapon(sheathedWeapon);
            SetSheathedWeapon(tempWeapon);
        }
    }

    public void DropWeapon()
    {
        networkView.RPC("SyncDrop", RPCMode.OthersBuffered);
        if (currentWeapon != null)
        {
            currentWeapon.GetComponent<Weapon>().isEquipped = false;
            currentWeapon.transform.parent = null;
            currentWeapon.collider.enabled = true;
            currentWeapon.rigidbody.isKinematic = false;
            currentWeapon = null;
            if (sheathedWeapon != null)
            {
                SetCurrentWeapon(sheathedWeapon);
                sheathedWeapon = null;
            }
        }
    }

    public void ChargeWeapon()
    {
        if (currentWeapon != null && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default"))
        {
            charging = true;
            startCharge = Time.time;
        }
    }

    public void ThrowWeapon()
    {
        networkView.RPC("SyncThrow", RPCMode.OthersBuffered, percentCharge);
        if (currentWeapon != null && charging && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default"))
        {
            charging = false;
            if (percentCharge <= 0.15f)
            {
                percentCharge = 0.15f;
            }
            currentWeapon.GetComponent<Weapon>().isEquipped = false;
            currentWeapon.GetComponent<Weapon>().EndLerp();
            currentWeapon.transform.parent = null;
            currentWeapon.collider.enabled = true;
            currentWeapon.rigidbody.isKinematic = false;
            currentWeapon.rigidbody.AddRelativeForce(Vector3.forward * throwForce * percentCharge);
            currentWeapon.rigidbody.maxAngularVelocity = 35;
            currentWeapon.rigidbody.AddRelativeTorque(40 * percentCharge, 0, 0, ForceMode.VelocityChange);
            currentWeapon = null;
            if (sheathedWeapon != null)
            {
                SetCurrentWeapon(sheathedWeapon);
                sheathedWeapon = null;
            }
        }
    }

    public void SwingWeapon()
    {
        networkView.RPC("SyncSwing", RPCMode.OthersBuffered);
        if (currentWeapon != null && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default") && currentWeapon.transform.localPosition == Vector3.zero)
        {
            charging = false;
            weaponLoc.GetComponent<Animator>().SetTrigger("Attack");
        }
    }

    public void ToggleMenu()
    {
        Menu.SetActive(!Menu.activeSelf);
        Chat.SetActive(!Menu.activeSelf);
        Vitals.SetActive(!Menu.activeSelf);
        Screen.showCursor = Menu.activeSelf;
        foreach (MouseLook mouseLook in GetComponentsInChildren<MouseLook>())
        {
            mouseLook.enabled = !Menu.activeSelf;
        }
        chatting = false;
        GetComponent<ChatScript>().SetInactive();
    }

    public void ToggleChat()
    {
        chatting = !chatting;
        GetComponent<ChatScript>().ToggleActive();
        foreach (MouseLook mouseLook in GetComponentsInChildren<MouseLook>())
        {
            mouseLook.enabled = !chatting;
        }
    }

    // Sets up the player view based on if client or server.
    void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        if (networkView.isMine)
        {
            Debug.Log("Instantiating local player view.");
            Camera.SetupCurrent(GetComponentInChildren<Camera>());
            GetComponentInChildren<Camera>().enabled = true;
            GetComponent<MouseLook>().enabled = true;
            GetComponentInChildren<MouseLook>().enabled = true;
            GetComponentInChildren<Camera>().GetComponent<MouseLook>().enabled = true;
        }
        else
        {
            Debug.Log("Instantiating remote player view.");
            GetComponentInChildren<Camera>().enabled = false;
            GetComponent<MouseLook>().enabled = false;
            GetComponentInChildren<MouseLook>().enabled = false;
            GetComponentInChildren<Camera>().GetComponent<MouseLook>().enabled = false;
        }
    }

    [RPC] void SyncPickup(NetworkViewID viewID)
    {
        GameObject weapon = NetworkView.Find(viewID).gameObject;
        weapon.transform.GetComponent<Weapon>().isEquipped = true;
        weapon.transform.GetComponent<Weapon>().isStuck = false;
        weapon.transform.GetComponent<Weapon>().equippedTo = gameObject;
        if (currentWeapon == null)
        {
            SetCurrentWeapon(weapon.collider.gameObject);
        }
        else
        {
            SetSheathedWeapon(weapon.collider.gameObject);
        }
    }

    [RPC] void SyncSwap()
    {
        if (sheathedWeapon != null && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default"))
        {
            GameObject tempWeapon = currentWeapon;
            SetCurrentWeapon(sheathedWeapon);
            SetSheathedWeapon(tempWeapon);
        }
    }

    [RPC] void SyncDrop()
    {
        if (currentWeapon != null && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default"))
        {
            currentWeapon.GetComponent<Weapon>().isEquipped = false;
            currentWeapon.transform.parent = null;
            currentWeapon.collider.enabled = true;
            currentWeapon.rigidbody.isKinematic = false;
            currentWeapon = null;
            if (sheathedWeapon != null)
            {
                SetCurrentWeapon(sheathedWeapon);
                sheathedWeapon = null;
            }
        }
    }

    [RPC] void SyncThrow(float percent)
    {
        if (currentWeapon != null && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default"))
        {
            charging = false;
            if (percent <= 0.15f)
            {
                percent = 0.15f;
            }
            currentWeapon.GetComponent<Weapon>().isEquipped = false;
            currentWeapon.GetComponent<Weapon>().EndLerp();
            currentWeapon.transform.parent = null;
            currentWeapon.collider.enabled = true;
            currentWeapon.rigidbody.isKinematic = false;
            currentWeapon.rigidbody.AddRelativeForce(Vector3.forward * throwForce * percent);
            currentWeapon.rigidbody.maxAngularVelocity = 30;
            currentWeapon.rigidbody.AddRelativeTorque(40 * percent, 0, 0, ForceMode.VelocityChange);
            currentWeapon = null;
            if (sheathedWeapon != null)
            {
                SetCurrentWeapon(sheathedWeapon);
                sheathedWeapon = null;
            }
        }
    }

    [RPC] void SyncSwing()
    {
        if (currentWeapon != null && weaponLoc.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Default") && currentWeapon.transform.localPosition == Vector3.zero)
        {
            charging = false;
            weaponLoc.GetComponent<Animator>().SetTrigger("Attack");
        }
    }

    // Used for comparing distances
    class RayHitComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((RaycastHit)x).distance.CompareTo(((RaycastHit)y).distance);
        }
    }
}