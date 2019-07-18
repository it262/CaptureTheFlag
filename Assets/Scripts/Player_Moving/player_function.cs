﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class player_function : MonoBehaviour
{
    /// <summary>
    ///*playerの機能
    ///・移動と回転ができる
    ///・どの方向がを向いているのかが分かる
    ///・自分のターンの間は攻撃できる
    ///・Goalしたかどうかが分かる
    ///・障害物に潰されたら死亡する
    /// </summary>

    GameManager gm;
    SocketObject so;

    public GameObject name_Indicater;

    public float move_speed;
    public float rotate_speed;
    public float attack_range;
    //
    bool is_my_turn = false;
    bool is_goal = false;
    bool is_dead = false;
    [SerializeField]bool is_enemy = true;
    //virtual
    Vector3 send_position;
    Vector3 send_rotation;
    string send_direction;
    // Start is called before the first frame update

    bool start = false;

    void Start()
    {
        gm = GameManager.Instance;
        so = SocketObject.Instance;

        gm._GameState
            .DistinctUntilChanged()
            .Where(x => x == GameState.GameStart)
            .Subscribe(_ => start = true);
    }

    // Update is called once per frame
    void Update()
    {
        name_Indicater.transform.LookAt(Camera.main.transform);
        name_Indicater.transform.eulerAngles += new Vector3(0, 180, 0);
        if (!start)
        {
            Debug.Log("待機中...");
            send_position = transform.position;
            send_rotation = transform.eulerAngles;
            return;
        }
        if (is_enemy)
        {
            return;
        }
        //*位置と向きの情報
        Vector3 velocity = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            velocity.z += 1;
        }

        if (Input.GetKey(KeyCode.A))
        {
            Vector3 def = transform.rotation.eulerAngles;
            def.y -= rotate_speed;
            send_rotation = def;
        }

        if (Input.GetKey(KeyCode.S))
        {
            velocity.z -= 1;
        }

        if (Input.GetKey(KeyCode.D))
        {
            Vector3 def = transform.rotation.eulerAngles;
            def.y += rotate_speed;
            send_rotation = def;
        }

        velocity = velocity.normalized * move_speed * Time.deltaTime;

        if (velocity.magnitude > 0)
        {
            Vector3 def = transform.position;
            send_position = def + gameObject.transform.rotation * velocity;
        }

        //*位置と向きを送る
        if (transform.position != send_position || transform.eulerAngles != send_rotation)
        {
            Debug.Log("SendPosition");
            transform.position = send_position;
            transform.eulerAngles = send_rotation;
            var data = new Dictionary<string, string>();
            data["TYPE"] = "Transform";
            //send_position
            data["posX"] = send_position.x.ToString();
            data["posY"] = send_position.y.ToString();
            data["posZ"] = send_position.z.ToString();
            //send_rotation
            data["rotX"] = send_rotation.x.ToString();
            data["rotY"] = send_rotation.y.ToString();
            data["rotZ"] = send_rotation.z.ToString();
            so.EmitMessage("ToOwnRoom", data);
        }

        //自分のターンの間は攻撃出来る
        if (is_my_turn)
        {
            Attack();
        }

    }

    //Rayで
    void Attack()
    {
        //どの方向を向いているかが分かる
        if (45 <= transform.eulerAngles.y && transform.eulerAngles.y < 135)
        {
            send_direction = "Right";
        }
        else if (135 <= transform.eulerAngles.y && transform.eulerAngles.y < 225)
        {
            send_direction = "Down";
        }
        else if (225 <= transform.eulerAngles.y && transform.eulerAngles.y < 315)
        {
            send_direction = "Left";
        }
        else
        {
            send_direction = "Up";
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, attack_range))
            {
                if (hit.collider.gameObject.CompareTag("Obstacle"))
                {
                    obstacle_function hit_component = hit.collider.gameObject.GetComponent<obstacle_function>();

                    int send_masu_x = hit_component.get_masu_x();
                    int send_masu_y = hit_component.get_masu_y();
                    //*殴った相手と自分の向きの情報を送る
                    //send_masu_x
                    //send_masu_y
                    //send_direction

                    var data = new Dictionary<string, string>();
                    data["TYPE"] = "Hit";
                    data["masuX"] = send_masu_x.ToString();
                    data["masuY"] = send_masu_y.ToString();
                    data["direction"] = send_direction;
                    so.EmitMessage("ToOwnRoom", data);

                    this.set_my_turn(false);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!is_enemy)
        {
            //Goalしたかどうかが分かる
            //*自分がGoalしたという情報
            if (collision.gameObject.name == "Goal")
            {
                is_goal = true;
                var data = new Dictionary<string, string>();
                data["TYPE"] = "Goal";
                so.EmitMessage("ToOwnRoom", data);
            }

            //障害物に潰されたら死亡する
            //*自分が死亡したという情報
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                if (collision.gameObject.GetComponent<obstacle_function>().get_is_moving() && !is_my_turn)
                {
                    Debug.Log("衝突！！！！！！！！！！");
                    //is_dead = true;
                    //Destroy(gameObject);
                    var data = new Dictionary<string, string>();
                    data["TYPE"] = "Dead";
                    so.EmitMessage("ToOwnRoom", data);
                }
            }
        }
    }

    public void set_my_turn(bool is_my_turn)
    {
        this.is_my_turn = is_my_turn;
    }

    public void set_pos_rot(Vector3 pos, Vector3 rot)
    {
        transform.position = pos;
        transform.eulerAngles = rot;
    }

    public void set_is_enemy(bool is_enemy)
    {
        this.is_enemy = is_enemy;
    }

    public void set_is_dead(bool is_dead)
    {
        //もしかしたら上のコードでdead判定に成功しているかも知れないので
        if (is_dead)
        {
            this.is_dead = is_dead;
            Destroy(gameObject);

        }
    }
}