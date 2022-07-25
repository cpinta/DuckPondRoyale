using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class PlayerController : Hittable
{
    public string nickname;

    #region INITIALIZE OBJECTS
    [Header("Components")]
    public DuckSettings duckSettings;
    public Transform tr;
    public Rigidbody2D rb;
    public CapsuleCollider2D cl;

    AnimatorHolder anims;
    public Animator wingAnim;
    public Animator headAnim;
    public Animator bodyAnim;
    public Animator feetAnim;

    PhotonView pView;
    #endregion

    #region VARIABLES
    [Header("Movement")]
    public int direction;
    public float speed;
    public float maxSpeed;
    public float jumpSpeed;
    public int flapCount;
    public float flapHeight;
    public int swimCount;
    public float swimSpeed;
    public float friction;

    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Animation")]
    public float walkingAnimSpeed;

    [Header("Cooldowns")]
    public float flapCooldown;
    public float peckCooldown;
    public float crouchPeckCooldown;
    public float honkCooldown;
    public float crouchHonkCooldown;
    public float swimCooldown;
    public float landingCooldown;
    public float postSwimInvincibility;
    


    [Header("Stats")]
    float damage;

    float swimInvincibility = GlobalVariables.getSwimInvincibility();

    public Vector2 movementVector;
    private bool inputJump, inputPeck;
    int currentFlaps;
    bool canControl, isDead;
    bool canPeck, canFlap, canHonk, canSwim;
    float anim_xvel; //xvelocity that is sent to the animations
    float anim_yvel;

    //Dictionary<string, Timer> Timers = new Dictionary<string, Timer>();

    bool jumping, crouching, turning, swimming, isFalling, onGround, inHitstun;
    bool pressingHonk;
    int againstWall;

    float attack;
    float defense;
    #endregion


    Timer flapTimer = new Timer();
    Timer peckTimer = new Timer();
    Timer honkTimer = new Timer();
    Timer swimTimer = new Timer();
    Timer landingTimer = new Timer();
    Timer hitstunTimer = new Timer();
    Timer invincibleTimer = new Timer();
    Timer cantControlTimer = new Timer();


    // START: is called before the first frame update
    void Start()
    {
        tr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody2D>();
        cl = GetComponent<CapsuleCollider2D>();

        pView = GetComponent<PhotonView>();

        rb.simulated = true;
        canControl = true;
        isDead = false;
        onGround = false;
        canPeck = true;
        inHitstun = false;
        swimming = false;
        canSwim = true;
       
        damage = 0;
        speed = duckSettings.Speed;
        maxSpeed = duckSettings.MaxSpeed;
        jumpSpeed = duckSettings.JumpHeight;
        swimSpeed = duckSettings.SwimSpeed;
        flapCount = duckSettings.FlapCount;
        flapHeight = duckSettings.FlapHeight;
        friction = duckSettings.Friction;

        attack = duckSettings.Attack;
        defense = duckSettings.Defense;

        flapCooldown = duckSettings.FlapCooldown;
        peckCooldown = duckSettings.PeckCooldown;
        honkCooldown = duckSettings.HonkCooldown;
        swimCooldown = duckSettings.SwimCooldown;
        landingCooldown = duckSettings.LandingCooldown;

        anims = new AnimatorHolder(wingAnim, headAnim, bodyAnim, feetAnim);
    }

    private void Update()
    {
        if (pView.IsMine)
        {
            StateAssignmentCode();
            AnimationVariables();
        }
    }


    // FIXED UPDATE: updates in delta time
    private void FixedUpdate()
    {
        if (pView.IsMine)
        {
            MovementCode();
            
        }
    }

    //whenever a state variable is set, it is set in here
    void StateAssignmentCode()
    {
        if(onGround && movementVector.y < 0)
        {
            crouching = true;
        }
        else
        {
            crouching = false;
        }
        if (rb.velocity.y < 0)
        {
            isFalling = true;
        }
        else
        {
            isFalling = false;
        }
        if (onGround && jumping)
        {
            jumping = false;
        }
        if (onGround)
        {
            currentFlaps = flapCount;
            swimming = false;
            canSwim = true;
            if (swimTimer.isInProgress())
            {
                swimTimer.setTimer(0);
            }
        }
        swimming = swimTimer.isInProgress();
    }

    void MovementCode()
    {
        //Basic sideways movement
        if(onGround && (movementVector.x > 0 && rb.velocity.x < 0 || movementVector.x < 0 && rb.velocity.x > 0))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        if (onGround)
        {
            if(Mathf.Abs(rb.velocity.x) < maxSpeed)
            {
                rb.velocity += Vector2.right * speed * movementVector.x * Time.deltaTime;
            }
        }
        else
        {
            rb.velocity += Vector2.right * maxSpeed * movementVector.x * Time.deltaTime;
        }

        if(Mathf.Abs(rb.velocity.x) < 0.1)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }


        //if jump button pressed longer, player jumps higher
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !inputJump)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
        //jump
        if (inputJump && onGround)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        }


        if(movementVector.x > 0)
        {
            direction = 1;
        }
        else if(movementVector.x < 0)
        {
            direction = -1;
        }

    }

    public void AnimationVariables()
    {
        //--------ANIMATION STUFF------------//

        if (rb.velocity.x > 0.01 || rb.velocity.x < -0.01)
            anim_xvel = rb.velocity.x;
        else
            anim_xvel = 0;

        if (rb.velocity.y > 0.01 || rb.velocity.y < -0.01)
            anim_yvel = rb.velocity.y;
        else
            anim_yvel = 0;

        anim_xvel = anim_xvel * walkingAnimSpeed;


        //SET ANIMATION PARAMETERS FOR 
        anims.SetFloatForAll("yVelocity", anim_yvel);
        anims.SetBoolForAll("isCrouching", crouching);

        //SET ANIMATION PARAMETERS FOR FEET
        if (onGround)
        {
            anims.SetFloatForAll("Speed", Mathf.Abs(anim_xvel));
        }
        else
        {
            anims.SetFloatForAll("Speed", 0);
        }

                      


        //FLIP CHARACTER THE DIRECTION THEY MOVE
        if (direction > 0)
        {
            if (turning && !jumping)
                tr.rotation = Quaternion.Euler(tr.rotation.x, 0, tr.rotation.z);
            else
                tr.rotation = Quaternion.Euler(tr.rotation.x, 180, tr.rotation.z);
        }
        else if (direction < 0)
        {
            if (turning && !jumping)
                tr.rotation = Quaternion.Euler(tr.rotation.x, 180, tr.rotation.z);
            else
                tr.rotation = Quaternion.Euler(tr.rotation.x, 0, tr.rotation.z);
        }


    }

    #region ONGROUND METHODS
    public bool getOnGround()
    {
        return onGround;
    }
    public void setOnGround(bool newValue)
    {
        onGround = newValue;
    }
    #endregion

    #region INPUT METHODS
    private void Direction(InputAction.CallbackContext context)
    {
        if (!cantControlTimer.isInProgress())
        {
            movementVector = context.ReadValue<Vector2>();
        }
        
    }
    private void Jump(InputAction.CallbackContext context)
    {
        inputJump = context.performed;
        //Debug.Log("trying to jump"+onGround);
        if (context.performed && onGround)
        {
            jumping = true;
            //Debug.Log("jumping");

        }
        else if (context.performed && !onGround && currentFlaps > 0)
        {
            Flap();
        }
    }

    private void Peck(InputAction.CallbackContext context)
    {
        if (!peckTimer.isInProgress() && pView.IsMine)
        {
            inputPeck = context.performed;
            anims.SetTriggerForAll("Peck");
            if (crouching) { peckTimer.setTimer(crouchPeckCooldown); }
            else { peckTimer.setTimer(peckCooldown); }
        }
    }

    private void Flap()
    {
        if (!flapTimer.isInProgress() && pView.IsMine)
        {
            currentFlaps--;
            anims.SetTriggerForAll("Flap");
            rb.velocity = new Vector2(rb.velocity.x, flapHeight);
            flapTimer.setTimer(flapCooldown);
        }
    }
    private void Honk()
    {
        if (!honkTimer.isInProgress() && pView.IsMine)
        {
            currentFlaps--;
            anims.SetTriggerForAll("Honk");
            if (crouching) { honkTimer.setTimer(crouchHonkCooldown); }
            else { honkTimer.setTimer(honkCooldown); }
        }
    }
    private void Swim()
    {
        if (canSwim && pView.IsMine)
        {
            canSwim = false;
            anims.SetTriggerForAll("Swim");
            rb.velocity = movementVector * swimSpeed;
            swimTimer.setTimer(swimCooldown);
            invincibleTimer.setTimer(postSwimInvincibility);
            cantControlTimer.setTimer(swimCooldown);
        }
    }
    #endregion
    [PunRPC]
    public override void GetHit(float damage, float knockback, float hitstun, Vector2 direction, knockbackType type)
    {
        if (pView.IsMine)
        {
            if (!invincibleTimer.isInProgress())
            {
                this.damage += damage;
                if (hitstun > 0)
                {
                    hitstunTimer.setTimer(hitstun);
                }

                //rb.velocity = direction.normalized * knockback * (this.damage / 5);
                rb.velocity = direction.normalized * knockback;
                Debug.Log(this.gameObject.name + " Player is hit: " 
                          + "Direction: " + direction.normalized 
                          + " Kb:" + knockback 
                          + " dmg/5: " + this.damage / 5 + " = " + rb.velocity);
                invincibleTimer.setTimer(0.01f);
            }
        }
    }


    private void OnTriggerEnter2d(Collider2D collider)
    {
        if (collider.CompareTag("Death"))
        {
            canControl = false;
            isDead = true;
            rb.velocity = Vector2.up * 100;
        }
    }
}