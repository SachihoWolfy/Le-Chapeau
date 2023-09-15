using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [HideInInspector]
    public int id;
    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;
    [HideInInspector]
    public float curHatTime;
    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;
    public PhotonView photonView;
    // Start is called before the first frame update
    void Start()
    {

    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(curHatTime);
        }
        else if (stream.IsReading)
        {
            curHatTime = (float)stream.ReceiveNext();
        }
    }


    // Update is called once per frame
    void Update()
    {
        Move();
        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
        // the host will check if the player has won
        if (PhotonNetwork.IsMasterClient)
        {
            if (curHatTime >= GameManager.instance.timeToWin && !GameManager.instance.gameEnded)
            {
                GameManager.instance.gameEnded = true;
                GameManager.instance.photonView.RPC("WinGame", RpcTarget.All, id);
            }
        }

        // track the amount of time we're wearing the hat
        if (hatObject.activeInHierarchy)
            curHatTime += Time.deltaTime;
    }
    void Move()
    {
        if (photonView.IsMine)
        {
            //float x = Input.GetAxis("Horizontal") * moveSpeed;
            float rot = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical") * moveSpeed;
            float rotStateX = 0;
            float rotStateZ = 0;
            this.rig.rotation = this.transform.rotation;
            switch (rig.rotation.eulerAngles.y % 360)
            {
                case 0:
                    rotStateX = 0;
                    rotStateZ = 1;
                    break;
                case 90:
                    rotStateX = 1;
                    rotStateZ = 0;
                    break;
                case 180:
                    rotStateX = 0;
                    rotStateZ = -1;
                    break;
                case 270:
                    rotStateX = -1;
                    rotStateZ = 0;
                    break;

                default:
                    break;
            }
            this.rig.isKinematic = false;
            this.rig.velocity = new Vector3(z * rotStateX, rig.velocity.y, z * rotStateZ);
            if (Input.GetKeyDown("a"))
            {
                this.transform.Rotate(0, -90, 0);
            }
            if (Input.GetKeyDown("d"))
            {
                this.transform.Rotate(0, 90, 0);
            }
        }
    }
    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, 0.7f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    // called when the player object is instantiated
    [PunRPC]
    public void Initialize(Player player)
    {
        photonPlayer = player;
        id = player.ActorNumber;
        GameManager.instance.players[id - 1] = this;
        // give the first player the hat
        if(id == 1)
            GameManager.instance.GiveHat(id, true);

        // if this isn't our local player, disable physics as that's
        // controlled by the user and synced to all other clients
        if (!photonView.IsMine)
            rig.isKinematic = true;
    }
    // sets the player's hat active or not
    public void SetHat(bool hasHat)
    {
        hatObject.SetActive(hasHat);
    }
    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
            return;
        // did we hit another player?
        if (collision.gameObject.CompareTag("Player"))
        {
            // do they have the hat?
            if (GameManager.instance.GetPlayer(collision.gameObject).id == GameManager.instance.playerWithHat)
 {
                // can we get the hat?
                if (GameManager.instance.CanGetHat())
                {
                    // give us the hat
                    GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All, id, false);
                }
            }
        }
    }



}
