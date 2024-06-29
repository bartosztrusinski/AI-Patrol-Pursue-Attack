using UnityEngine;
using UnityEngine.AI;

public class State
{
  public enum STATE
  {
    IDLE,
    PATROL,
    PURSUE,
    ATTACK,
    SLEEP,
    RUNAWAY
  };

  public enum EVENT
  {
    ENTER,
    UPDATE,
    EXIT
  };

  public STATE name;
  protected EVENT stage;
  protected GameObject npc;
  protected Transform player;
  protected State nextState;
  protected NavMeshAgent agent;

  private readonly float visDist = 10f;
  private readonly float visAngle = 30f;
  private readonly float shootDist = 7f;

  public State(GameObject _npc, NavMeshAgent _agent, Transform _player)
  {
    npc = _npc;
    agent = _agent;
    player = _player;
    stage = EVENT.ENTER;
  }

  public virtual void Enter() { stage = EVENT.UPDATE; }
  public virtual void Update() { stage = EVENT.UPDATE; }
  public virtual void Exit() { stage = EVENT.EXIT; }

  public State Process()
  {
    switch (stage)
    {
      case EVENT.ENTER:
        Enter();
        break;
      case EVENT.UPDATE:
        Update();
        break;
      case EVENT.EXIT:
        Exit();
        return nextState;
    }

    return this;

    // if (stage == EVENT.ENTER)
    // {
    //   Enter();
    // }

    // if (stage == EVENT.UPDATE)
    // {
    //   Update();
    // }

    // if (stage == EVENT.EXIT)
    // {
    //   Exit();
    //   return nextState;
    // }

    // return this;
  }

  public bool CanSeePlayer()
  {
    Vector3 direction = player.position - npc.transform.position;
    float angle = Vector3.Angle(direction, npc.transform.forward);

    return direction.magnitude < visDist && angle < visAngle;
  }

  public bool IsPlayerBehind()
  {
    Vector3 direction = npc.transform.position - player.position;
    float angle = Vector3.Angle(direction, npc.transform.forward);

    return direction.magnitude < 2f && angle < 30f;
  }

  public bool CanAttackPlayer()
  {
    Vector3 direction = player.position - npc.transform.position;

    return direction.magnitude < shootDist;
  }
}

public class Idle : State
{
  public Idle(GameObject _npc, NavMeshAgent _agent, Transform _player)
      : base(_npc, _agent, _player)
  {
    name = STATE.IDLE;
  }

  public override void Enter()
  {
    base.Enter();
  }

  public override void Update()
  {
    if (CanSeePlayer())
    {
      nextState = new Pursue(npc, agent, player);
      stage = EVENT.EXIT;
    }
    else if (Random.Range(0, 100) < 10)
    {
      nextState = new Patrol(npc, agent, player);
      stage = EVENT.EXIT;
    }
  }

  public override void Exit()
  {
    base.Exit();
  }
}

public class Patrol : State
{
  private int currentIndex = -1;

  public Patrol(GameObject _npc, NavMeshAgent _agent, Transform _player)
      : base(_npc, _agent, _player)
  {
    name = STATE.PATROL;
    agent.speed = 2f;
    agent.isStopped = false;
  }

  public override void Enter()
  {
    float lastDistance = Mathf.Infinity;

    for (int i = 0; i < GameEnvironment.Singleton.Checkpoints.Count; ++i)
    {
      GameObject thisWP = GameEnvironment.Singleton.Checkpoints[i];
      float distance = Vector3.Distance(npc.transform.position, thisWP.transform.position);

      if (distance < lastDistance)
      {
        currentIndex = i - 1;
        lastDistance = distance;
      }
    }

    base.Enter();
  }

  public override void Update()
  {
    if (agent.remainingDistance < 1)
    {
      currentIndex = (currentIndex + 1) % GameEnvironment.Singleton.Checkpoints.Count;
      agent.SetDestination(GameEnvironment.Singleton.Checkpoints[currentIndex].transform.position);
    }

    if (CanSeePlayer())
    {
      nextState = new Pursue(npc, agent, player);
      stage = EVENT.EXIT;
    }
    else if (IsPlayerBehind())
    {
      nextState = new RunAway(npc, agent, player);
      stage = EVENT.EXIT;
    }
  }

  public override void Exit()
  {
    base.Exit();
  }
}

public class Pursue : State
{
  public Pursue(GameObject _npc, NavMeshAgent _agent, Transform _player)
      : base(_npc, _agent, _player)
  {
    name = STATE.PURSUE;
    agent.speed = 5f;
    agent.isStopped = false;
  }

  public override void Enter()
  {
    base.Enter();
  }

  public override void Update()
  {
    agent.SetDestination(player.position);

    if (!agent.hasPath)
    {
      return;
    }

    if (CanAttackPlayer())
    {
      nextState = new Attack(npc, agent, player);
      stage = EVENT.EXIT;
    }
    else if (!CanSeePlayer())
    {
      nextState = new Patrol(npc, agent, player);
      stage = EVENT.EXIT;
    }
  }

  public override void Exit()
  {
    base.Exit();
  }
}

public class Attack : State
{
  private readonly float rotationSpeed = 2f;
  private readonly AudioSource shoot;

  public Attack(GameObject _npc, NavMeshAgent _agent, Transform _player)
      : base(_npc, _agent, _player)
  {
    name = STATE.ATTACK;
    shoot = _npc.GetComponent<AudioSource>();
  }

  public override void Enter()
  {
    agent.isStopped = true;
    shoot.Play();
    base.Enter();
  }

  public override void Update()
  {
    Vector3 direction = player.position - npc.transform.position;
    direction.y = 0f;

    npc.transform.rotation =
      Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);

    if (!CanAttackPlayer())
    {
      nextState = new Idle(npc, agent, player);
      shoot.Stop();
      stage = EVENT.EXIT;
    }
  }

  public override void Exit()
  {
    base.Exit();
  }
}

public class RunAway : State
{
  private readonly GameObject safeLocation;

  public RunAway(GameObject _npc, NavMeshAgent _agent, Transform _player)
      : base(_npc, _agent, _player)
  {
    name = STATE.RUNAWAY;
    safeLocation = GameObject.FindGameObjectWithTag("Safe");
  }

  public override void Enter()
  {
    agent.isStopped = false;
    agent.speed = 6;
    agent.SetDestination(safeLocation.transform.position);
    base.Enter();
  }

  public override void Update()
  {
    if (agent.remainingDistance < 1f)
    {
      nextState = new Idle(npc, agent, player);
      stage = EVENT.EXIT;
    }
  }

  public override void Exit()
  {
    base.Exit();
  }
}