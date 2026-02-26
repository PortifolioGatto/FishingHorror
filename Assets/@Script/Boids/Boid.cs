//using System.Collections.Generic;
//using UnityEngine;

//public class Boid : MonoBehaviour
//{
//    public int familyID = 0;
//    [Space]

//    public float baseSpeed = 5f;
//    private float speed = 5f;
//    public float minSpeed = 2;
//    public float maxSpeed = 2;

//    public float turnSpeed = 5f;

//    public float avoidanceWeight = 1f;
//    public float cohesionWeight = 1f;
//    public float alignmentWeight = 1f;
//    public float mass = 1f;
//    public float maxForce = 1f;
//    public float radius = 1f;
//    public float boundaryForce = 10f;

//    public Vector3 acceleration;
//    public Vector3 velocity;

//    private List<Boid> neighbors;

//    private BoidsManager boidsManager;

//    public void Initialize(BoidsManager manager)
//    {
//        neighbors = new List<Boid>();
//        boidsManager = manager;

//        speed = baseSpeed / Mathf.Sqrt(radius);
//        velocity = Random.insideUnitSphere.normalized * speed;
//    }

//    public void GetNeighbors()
//    {
//        boidsManager.GetNeighbors(this, neighbors);
//    }

//    public void HandleBoundary()
//    {
//        float boundaryThreshold = boidsManager.simulationRadius * 0.8f;

//        Vector3 center = boidsManager.transform.position;

//        Vector3 distanceToCenter = center - transform.position;

//        float distanceFromCenter = distanceToCenter.magnitude;

//        if (distanceFromCenter > boundaryThreshold)
//        {
//            float t = (distanceFromCenter - boundaryThreshold) / (boidsManager.simulationRadius - boundaryThreshold);

//            Vector3 force = distanceToCenter.normalized * boundaryForce * t;
//            acceleration += force;
//        }
//    }

//    public void HandleAlignment()
//    {
//        if (neighbors == null || neighbors.Count == 0) return;

//        Vector3 averageVelocity = Vector3.zero;
//        int count = 0;

//        for (int i = 0; i < neighbors.Count; i++)
//        {
//            Boid other = neighbors[i];

//            if (other == this) continue;
//            if(other.familyID != familyID) continue;

//            averageVelocity += neighbors[i].velocity;
//            count++;
//        }

//        if(count == 0) return;

//        averageVelocity /= count;

//        acceleration += (averageVelocity - velocity) * alignmentWeight;
//    }
//    public void HandleCohesion()
//    {
//        if(neighbors == null || neighbors.Count == 0) return;

//        Vector3 averagePos = Vector3.zero;
//        int count = 0;

//        for (int i = 0; i < neighbors.Count; i++)
//        {
//            Boid other = neighbors[i];

//            if (other == this) continue;
//            if(other.familyID != familyID) continue;

//            averagePos += neighbors[i].transform.position;
//            count++;
//        }

//        if(count == 0) return;

//        averagePos /= count;

//        Vector3 desired = (averagePos - transform.position).normalized * speed;
//        Vector3 steer = desired - velocity;

//        acceleration += steer * cohesionWeight;
//    }
//    public void HandleAvoidance()
//    {
//        if (neighbors == null || neighbors.Count == 0) return;

//        Vector3 force = Vector3.zero;

//        for (int i = 0; i < neighbors.Count; i++)
//        {
//            Boid other = neighbors[i];

//            Vector3 diff = transform.position - other.transform.position;
//            float dist = diff.magnitude;

//            float minDist = radius + other.radius;

//            if (dist > 0 && dist < minDist)
//            {
//                float strength = (minDist - dist) / minDist;
//                force += diff.normalized * strength;
//            }
//        }

//        acceleration += force * avoidanceWeight;
//    }


//    public void HandleLocomotion()
//    {
//        HandleRotation();
//        HandleMovement();
//    }
//    public void HandleRotation()
//    {
//        if (velocity.magnitude > 0.1f)
//        {
//            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
//            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
//        }
//    }
//    public void HandleMovement()
//    {
//        acceleration = Vector3.ClampMagnitude(acceleration, maxForce);

//        velocity += (acceleration / mass) * Time.deltaTime;
        
//        if (velocity.magnitude < minSpeed)
//        {
//            velocity = velocity.normalized * minSpeed;
//        }
//        else if (velocity.magnitude > maxSpeed)
//        {
//            velocity = velocity.normalized * maxSpeed;
//        }

//        velocity = Vector3.ClampMagnitude(velocity, speed);

//        transform.position += velocity * Time.deltaTime;

//        acceleration = Vector3.zero;
//    }
//}