using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum knockbackType
{
    Relative,                //The direction of knockback pushes away depending on the hitbox owner's direction  
    Fixed,                   //The direction of knockback pushes away in the same direction no matter the direction of the hitbox owner
    Centered                //The direction of knockback pushes away from the center of the hitbox
}
public enum hitboxShape
{
    Box,
    Circle
}
public class Hitbox : MonoBehaviour
{
    //[DisplayWithoutEdit()]
    public PlayerController owner;
    public HitboxSettings settings;
    Attack attack;

    [SerializeField] bool updateValues;
    [SerializeField] bool active;
    bool oldactive;
    public bool showGizmo;
    //[SerializeField] bool active;

    [Header("Stats")]
    [SerializeField] public int priority;
    [SerializeField] float damage;
    [SerializeField] float knockback;
    [SerializeField] float hitstun;
    [SerializeField] Vector2 angle;
    [SerializeField] knockbackType type;

    [Header("Size")]
    [SerializeField] hitboxShape hitboxShapes;
    [SerializeField] CapsuleDirection2D capsuleDirection;
    [SerializeField] Vector2 center;
    [SerializeField] Vector2 size;
    [SerializeField] float circleRadius;
    [SerializeField] float hitboxAngle;

    Vector2 offset = new Vector2(0,0);
    Vector2 centerTransform;
    int direction;

    public List<Hittable> currentHit;

    List<int> instanceIDs = new List<int>();


    private void Start()
    {
        hitboxShapes = settings.HitboxShapes;
        capsuleDirection = settings.CapsuleDirection;
        center = settings.Center;
        size = settings.Size;
        angle = settings.Angle;
        circleRadius = settings.CircleRadius;
        hitboxAngle = settings.HitboxAngle;
        knockback = settings.Knockback;
        damage = settings.Damage;
        hitstun = settings.Hitstun;
        attack = this.gameObject.GetComponent<Attack>();

        centerTransform = center;

        owner = this.transform.root.gameObject.GetComponent<PlayerController>();
        direction = owner.direction;
    }

    private void Update()
    {
        direction = -owner.direction;
        centerTransform = new Vector2(this.transform.position.x + (center.x * direction), this.transform.position.y + center.y);
        if (active)
        {
            showGizmo = true;
            Debug.Log("hitbox active:" + active);
            if (!oldactive)
            {
                instanceIDs.Clear();
            }
            List<Collider2D> colliders = new List<Collider2D>();

            List<Hittable> currentHit = renderHitbox(showGizmo);
            foreach (Hittable hittable in currentHit)
            {
                if (!instanceIDs.Contains(hittable.GetInstanceID()))
                {
                    instanceIDs.Add(hittable.GetInstanceID());
                    Hit(hittable);
                }
            }
            
        }
        else
        {
            showGizmo = false;
        }
        oldactive = active;
    }

    private void OnValidate()
    {
        if (updateValues)
        {
            if (settings == null) { return; }


            hitboxShapes = settings.HitboxShapes;
            capsuleDirection = settings.CapsuleDirection;
            center = settings.Center;
            size = settings.Size;
            angle = settings.Angle;
            circleRadius = settings.CircleRadius;
            hitboxAngle = settings.HitboxAngle;
            centerTransform = center;

            damage = settings.Damage;
            knockback = settings.Knockback;
            hitstun = settings.Hitstun;
        }
        centerTransform = offset + center;
    }
    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            if(hitboxShapes == hitboxShape.Box)
            {
                //Gizmos.matrix = this.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(centerTransform, size);
                //this.transform.rotation = Quaternion.Euler(0,0,hitboxAngle);
            }
            if(hitboxShapes == hitboxShape.Circle)
            {
                Gizmos.DrawWireSphere(centerTransform, circleRadius);
            }
            Gizmos.color = Color.red;
            drawHitbox();
        }
    }
    public void drawHitbox()
    {
        List<Collider2D> colliders = new List<Collider2D>();
        switch (hitboxShapes)
        {
            case hitboxShape.Box:
                colliders = Physics2D.OverlapBoxAll(centerTransform, size, hitboxAngle).ToList();
                break;
            case hitboxShape.Circle:
                colliders = Physics2D.OverlapCircleAll(centerTransform, circleRadius).ToList();
                break;
        }
        //Debug.Log(colliders.ToArray().ToString());
    }

    public List<Hittable> renderHitbox(bool showGizmos)
    {
        direction = -owner.direction;
        centerTransform = new Vector2(this.transform.position.x + (center.x * direction), this.transform.position.y + center.y);
        List<Collider2D> colliders = new List<Collider2D>();
        showGizmo = showGizmos;
        switch (hitboxShapes)
        {
            case hitboxShape.Box:
                colliders = Physics2D.OverlapBoxAll(centerTransform, size, hitboxAngle).ToList();
                break;
            case hitboxShape.Circle:
                colliders = Physics2D.OverlapCircleAll(centerTransform, circleRadius).ToList();
                break;
        }

        List<Hittable> thoseHit = new List<Hittable>();
        foreach (Collider2D collider in colliders)
        {
            GameObject rootObj = collider.gameObject.transform.root.gameObject;
            if (collider.gameObject.tag == "Hurtbox" && 
                rootObj.GetInstanceID() != this.transform.root.gameObject.GetInstanceID() && 
                rootObj.GetComponent<Hittable>() != null)
            {
                thoseHit.Add(collider.gameObject.transform.root.gameObject.GetComponent<Hittable>());
            }
        }

        //showGizmo = false;
        return thoseHit;
    }


    public void Hit(Hittable hittable)
    {
        Debug.Log("HIT");
        float direction;
        Vector2 newAngle = new Vector2();

        switch (type)
        {
            case knockbackType.Relative:

                direction = this.gameObject.transform.root.GetComponent<PlayerController>().direction;
                Debug.Log(direction);
                newAngle = new Vector2(angle.x * direction, angle.y);
                break;
            case knockbackType.Centered:
                direction = this.gameObject.transform.root.GetComponent<PlayerController>().direction;
                Vector2 hypotenuse = this.gameObject.transform.root.position - hittable.transform.position;
                Vector2 horizontal = new Vector2(direction, 0);
                float angleDegree = Vector2.Angle(from: horizontal, to: hypotenuse);
                newAngle = (Vector2)(Quaternion.Euler(0, 0, angleDegree) * Vector2.right);

                break;
            default:
                newAngle = angle;
                break;
        }

        Debug.Log("Sending[KB:"+knockback+", Angle:"+angle+"] to enemy");


        if (PhotonNetwork.IsConnected)
        {

            PhotonView pView = hittable.gameObject.GetComponent<PhotonView>();          //pView = the opposing players view
            PhotonView ownerView = this.owner.GetComponent<PhotonView>();
            pView.RPC("GetHit", RpcTarget.All, damage, knockback, hitstun, newAngle, type); //call GetHit on the opposing player
            ownerView.RPC("Hit", RpcTarget.All, hittable.damage, damage, knockback); //call Hit on the hitbox owner
            //ownerView.gameObject.GetComponent<PlayerController>().Hit(hittable.damage, damage, knockback);
        }
        else
        {
            hittable.GetHit(damage, knockback, hitstun, newAngle, type);
            owner.Hit(hittable.damage, damage, knockback);
        }




        //hittable.GetHit(damage, knockback, hitstun, newAngle, type);
    }

        //private List<Collider2D> removeDuplicateColliders(List<Collider2D> opposingColliders)
        //{
        //    List<int> instanceIDs = new List<int>();
        //    foreach (Collider2D collider in opposingColliders)
        //    {
        //        if (!instanceIDs.Contains(collider.gameObject.transform.root.gameObject.GetInstanceID()))
        //        {
        //            instanceIDs.Add(collider.gameObject.transform.root.gameObject.GetInstanceID());
        //        }
        //        else
        //        {
        //            opposingColliders.Remove(collider);
        //        }
        //    }
        //    return opposingColliders;
        //}
}
