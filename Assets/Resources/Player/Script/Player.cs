using UnityEngine;

public class Player : MonoBehaviour {
    public float speed = 4.0f;
    public float jumpForce = 2.0f;
    public int health = 6;
    public int ammo = 10;
    public int score = 0;
    private float timeGame = 0;


    private bool onGround = false;
    private Rigidbody2D plRigidBody;
    private Animator plAnimator;
    private SpriteRenderer plSprite;

    private void Awake ()
    {
        plRigidBody = GetComponent <Rigidbody2D> ();
        plAnimator = GetComponent<Animator>();
        plSprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        timeGame += Time.deltaTime;
        Controll();
    }

    private void FixedUpdate()
    {
        CheckGround();
        
    }

    private void Controll ()
    {
        if (Input.GetButton("Horizontal"))
        {
            plAnimator.SetInteger("state", 1);
            Move();
        }
        else
            plAnimator.SetInteger("state", 0);
        if (Input.GetButton("Jump") && onGround)
            Jump();
        if (!onGround)
            plAnimator.SetInteger("state", 2);       
    }

    private void CheckGround()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector3(transform.position.x, transform.position.y - 1.4f, transform.position.z), 0.03f);
        for (int i = 0; i < colliders.Length; i++)
            if (!colliders[i].isTrigger && colliders[i].gameObject.tag != "Player")
            {
                onGround = true;
                return;
            }
        onGround = false;
    }

    private void Move()
    {
        Vector3 tempVec = Vector3.right * Input.GetAxis("Horizontal");
        transform.position = Vector3.MoveTowards(transform.position, transform.position + tempVec, speed * Time.deltaTime);
        if(tempVec.x < 0)
            plSprite.flipX = true;
        else
            plSprite.flipX = false;
    }

    private void Jump()
    {
        plRigidBody.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
    }


}




















