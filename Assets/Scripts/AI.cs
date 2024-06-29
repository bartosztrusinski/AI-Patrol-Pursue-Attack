using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour
{
  NavMeshAgent agent;
  State currentState;

  public Transform player;

  void Start()
  {
    agent = GetComponent<NavMeshAgent>();
    currentState = new Idle(gameObject, agent, player);
  }


  void Update()
  {
    currentState = currentState.Process();
  }
}
