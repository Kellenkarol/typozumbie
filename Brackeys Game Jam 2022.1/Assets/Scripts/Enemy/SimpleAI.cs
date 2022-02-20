using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Reflection;

public class SimpleAI : MonoBehaviour
{
    public Transform player;
    public LayerMask layerOfObstacles, layerOfFloor;
    public float followRange;

    Vector3 target, oldPositionOfThePlayer, newPositionOfThePlayer;
    NavMeshAgent agent;
    NavMeshObstacle myObstacle;

    Vector3[] cornersOfPath;

    bool isOnEnd, pathFound, stuck, followingPlayer, lastTargetWasThePlayer, lostThePlayer, tryingFindPlayer;
    int currentIndexOfPathCorner;
    float speed;


    private List<Vector2> poss = new List<Vector2>();

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.blue;
        foreach(Vector2 pos in poss)
        {
            // print(pos);
            Gizmos.DrawSphere(pos, 0.25f);
        }
    }



    // Start is called before the first frame update
    void Start()
    {
        agent                   = GetComponent<NavMeshAgent>();
        // agent.avoidancePriority = GetRandomPriority();
        // transform.position
        // print(agent.path.corners.Length);
        agent.updateRotation    = false;
        agent.updateUpAxis      = false;
        speed                   = agent.speed;


        myObstacle              = GetComponent<NavMeshObstacle>();

        target = GetNewRandomTargetPosition();
        agent.SetDestination(target);

        StartCoroutine("ChangeDestinationWhenStuck");
    }


    void Update()
    {  
        DrawLine(transform.position, target);
        Debug.DrawLine((Vector2) player.position - Vector2.right * 1, (Vector2) player.position - Vector2.right * 8, Color.red);


        // follow player --------------------------------------------------------------------

            followingPlayer = !HasObstacleBetweenThePlayerAndMe() && PlayerIsOnRange();

            if(followingPlayer)
            {
                // StopAllCoroutines();
                StopCoroutine("TryFindThePlayer");
                print("Achei muahahahahaha");
                lastTargetWasThePlayer  = true;
                lostThePlayer           = false;
                target                  = player.position;
                agent.SetDestination(target);
                oldPositionOfThePlayer = player.position;
            }
            else if(lastTargetWasThePlayer && !lostThePlayer)
            {
                print("Perdi o safado");
                newPositionOfThePlayer  = player.position;
                lostThePlayer           = true;
                StopCoroutine("TryFindThePlayer");
                // StopAllCoroutines();
                List<Vector2> closePositions = GetListOfPositionsNearOfThePlayer(5);
                closePositions = SortByClosest(closePositions);
                StartCoroutine(TryFindThePlayer(closePositions));
                // target                  = GetNewRandomPositionNearOfThePlayer();
                // agent.SetDestination(target);
            }

        // ----------------------------------------------------------------------------------


        if(((IsOnTheEnd() && !tryingFindPlayer) || stuck))
        {
            stuck           = false;
            target          = GetNewRandomTargetPosition();
            agent.SetDestination(target);
        }

        LookAt(agent.steeringTarget - transform.position);

        return; // ----------------------------------------------------------------------------------------------------------


        agent.SetDestination(player.position);


        if(pathFound)
        {
            isOnEnd                     = ((Vector2)(target-transform.position)).magnitude <= 0.1f;

            if(!isOnEnd)
            {
                MoveThroughPath(); // movimenta através do caminho
                LookAt(agent.steeringTarget-transform.position); // mantem a rotação na direção do movimento
                return;
            }

            // ClearLog();
            // print("isOnEnd");

            target = GetNewRandomTargetPosition();
            SetAgentNewDestination(target);
            currentIndexOfPathCorner = 0;
        }

    }


    IEnumerator TryFindThePlayer(List<Vector2> positions)
    {   

        print("Mas eu vou achar ele, nem que seja a última coisa que eu faça");
        tryingFindPlayer = true;

        poss = positions;

        foreach(Vector2 position in positions)
        {
            target = position;
            Debug.DrawLine(transform.position, target, Color.red);
            agent.SetDestination(target);

            print("Waiting");
            yield return new WaitUntil(IsOnTheEnd);

            // while(!IsOnTheEnd() && !stuck)
            // {
            //     print("Waiting");
            //     //wait
            //     yield return null;
            // }

            // yield return null;
        }
        print("Desisto");

        tryingFindPlayer = false;
    }



    bool IsOnTheEnd()
    {
        return ((Vector2)(target-transform.position)).magnitude <= 0.5f;
    }


    int GetRandomPriority()
    {
        return Random.Range(0, 100);
    }


    void DrawLine(Vector2 start, Vector2 end)
    {
        Debug.DrawLine(start, end, Color.blue);
    }


    IEnumerator ChangeDestinationWhenStuck()
    {
        while(true)
        {
            Vector2 myOldPosition = transform.position;
            yield return new WaitForSeconds(2f);
            Vector2 myNewPosition = transform.position;
            // print((myOldPosition-myNewPosition).magnitude);
            if((myOldPosition-myNewPosition).magnitude <= 0.5f)
            {
                print("Held, i'm stuck");
                stuck = true;
            }

        }
    }




    void SetAgentNewDestination(Vector2 target)
    {
        // print("Definindo nova posição");
        pathFound           = false;
        myObstacle.enabled  = false;
        agent.enabled       = true;

        if(agent.hasPath) 
        {
            // print("Resetando caminho antigo");
            agent.ResetPath();
        }

        agent.SetDestination(target);
        StopAllCoroutines();
        StartCoroutine(WaitUntilFindPath());
    }


    IEnumerator WaitUntilFindPath()
    {
        while(!agent.hasPath)
        {
            // print("Esperando encontrar caminho");
            yield return null;
        }

        SetCorners(); // atualiza as posições do caminho
        
        myObstacle.enabled  = true;
        agent.enabled       = false;
        pathFound           = true;
        // print("caminho encontrado");
    }


    void SetAgentDestinationAndCorners(Vector2 target)
    {
        // myObstacle.enabled          = false;
        agent.enabled               = true;
        agent.SetDestination(target);
        SetCorners();
        agent.enabled               = false;
        // myObstacle.enabled          = true;

    }


    void SetCorners()
    {
        cornersOfPath = agent.path.corners;
    }


    void MoveThroughPath()
    {
        // print(cornersOfPath.Length+"          "+currentIndexOfPathCorner);

        transform.position = Vector2.MoveTowards(transform.position, cornersOfPath[currentIndexOfPathCorner], speed*Time.deltaTime);

        bool moveToNextCorner = ((Vector2)(cornersOfPath[currentIndexOfPathCorner] - transform.position)).magnitude <= 0.001f;
        
        if(moveToNextCorner)
        {
            // print("[!] moveToNextCorner");
            if(currentIndexOfPathCorner < cornersOfPath.Length-1)
            {
                // print("[!] currentIndexOfPathCorner++");
                currentIndexOfPathCorner++;
            }
            // else
            // {
            //     print("[!] ResetPath");
            //     agent.ResetPath();
            // }
        }
    }

    
    List<Vector2> GetListOfPositionsNearOfThePlayer(int count = 2, bool StartWithPlayerPosition = true, float minRange = 5, float maxRange = 15, float maxCountOfExellentPositions = 1)
    {
        List<Vector2> positions = new List<Vector2>();

        if(StartWithPlayerPosition)
        {
            count--;
            positions.Add(GetNearstPositionOnNavMesh(player.position));
        }

        maxCountOfExellentPositions = maxCountOfExellentPositions > count ? count : maxCountOfExellentPositions;

        for(int c=0; c<maxCountOfExellentPositions; c++)
            positions.Add(GetNewRandomPositionNearOfThePlayer(minRange, maxRange, true));
        
        for(int c=0; c<count-maxCountOfExellentPositions; c++)
            positions.Add(GetNewRandomPositionNearOfThePlayer(minRange, maxRange, false));

        print("positions.Count: "+positions.Count);

        return positions;
    }



    // usar quando o inimigo está seguindo o player mas o perde de vista
    // basicamente o que a função faz é restornar uma posição proxima ao player
    // para que assim o inimigo tenha uma pista da sua possível localização
    Vector2 GetNewRandomPositionNearOfThePlayer(float minRange = 5, float maxRange = 15, bool useLineCast=false)
    {

        // new method ---------------------

            // if(!useLineCast)
            // {

            //     Vector2 randomDirection;
            //     Vector2 randomDirectionWithRange;

            //     for(int c=0; c < 50; c++)
            //     {

            //         randomDirection             = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            //         randomDirection            /= Mathf.Sqrt(randomDirection.x*randomDirection.x + randomDirection.y*randomDirection.y);
            //         randomDirectionWithRange    = randomDirection * Random.Range(0, maxRange);

            //         NavMeshHit hit;

            //         if (NavMesh.SamplePosition((Vector2) player.position + randomDirectionWithRange, out hit, 1000, NavMesh.AllAreas))
            //             return hit.position;

            //     }

            //     print("Adicionei a posição do player 1");
              // // return player.position;
                // return GetNearstPositionOnNavMesh(player.position);

            // }


        // old method ---------------------
            for(int c=0; c < 50; c++)
            {
                // Vector2 faceOfPlayer = player.transform.up;
                Vector2 randomDirection = player.transform.right * Random.Range(-1.0f, 1.0f);
                randomDirection += (Vector2) player.transform.up/4;
                // Vector2 randomDirection = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                randomDirection /= Mathf.Sqrt(randomDirection.x*randomDirection.x + randomDirection.y*randomDirection.y);
                RaycastHit2D _hit = Physics2D.Linecast(player.position, (Vector2) player.position+randomDirection*maxRange, layerOfObstacles);

                NavMeshHit hit;
                Vector2 positionNearOfThePlayer = (Vector2) player.position + randomDirection * _hit.distance;
                NavMesh.SamplePosition(positionNearOfThePlayer, out hit, 1000, NavMesh.AllAreas);
                float randomDistance = Random.Range(0, (player.position-hit.position).magnitude);

                if(randomDistance > minRange)
                {
                    Debug.DrawLine(player.position, (Vector2) player.position + randomDirection * (randomDistance), Color.white, 10);
                    return (Vector2) player.position + randomDirection * (randomDistance);
                }
            }

            print("Adicionei a posição do player 2");
            return GetNearstPositionOnNavMesh(player.position);
            
    }


    List<Vector2> SortByClosest(List<Vector2> positions)
    {
        // return positions;

        List<Vector2> positionsSorted = new List<Vector2>();
        int count = positions.Count;
        Vector2 lastPosition = positions[0];
        Vector2 nearstPosition = positions[0];
        positionsSorted.Add(lastPosition);
        positions.Remove(lastPosition);


        while(positionsSorted.Count != count)
        {
            float minDistance = Mathf.Infinity;

            foreach(Vector2 oldPos in positions)
            {
                if((lastPosition-oldPos).magnitude < minDistance)
                {
                    minDistance     = (lastPosition-oldPos).magnitude;
                    nearstPosition  = oldPos;
                }
            }
            lastPosition = nearstPosition;
            positionsSorted.Add(lastPosition);
            positions.Remove(lastPosition);
        }

        // print("positionsSorted.Count: "+positionsSorted.Count);

        // for(int c=0; c<positionsSorted.Count; c++)
        // {
        //     print(positions[c]+"     ->    "+positionsSorted[c]);
        // }

        return positionsSorted;
    }


    Vector2 GetNearstPositionOnNavMesh(Vector2 position)
    {
        NavMeshHit hit;
        NavMesh.SamplePosition(position, out hit, 1000, NavMesh.AllAreas);
        return hit.position;
    }


    Vector3 GetNewRandomTargetPosition()
    {
        while(true)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 100;
            randomDirection += transform.position;

            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomDirection, out hit, 1000, NavMesh.AllAreas))
                return hit.position;
        }
    }

    void LookAt(Vector2 direction)
    {
        Quaternion toRotation = Quaternion.LookRotation(Vector3.forward, direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 1000*Time.deltaTime);
    }


    bool PlayerIsOnRange()
    {
        return ((Vector2)(player.position-transform.position)).magnitude <= followRange;
    }

    bool HasObstacleBetweenThePlayerAndMe()
    {
        return Physics2D.Linecast(transform.position, player.position, layerOfObstacles).distance != 0;
    }

    public void ClearLog()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }


    void OnTriggerStay2D(Collider2D col)
    {
        if(col.tag != player.tag)
            return;

        agent.SetDestination(col.transform.position);
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if(col.tag != player.tag)
            return;
            
    }

}
