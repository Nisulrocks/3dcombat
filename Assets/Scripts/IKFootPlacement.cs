using UnityEngine;

public class IKFootPlacement : MonoBehaviour
{

    Animator animator;

    [Range(0, 1f)]
    public float DistanceToGround;

    public LayerMask layerMask;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if(animator){

            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);

            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);

            //left foot
            RaycastHit hit;
            Ray ray = new Ray(animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
            if(Physics.Raycast(ray, out hit, DistanceToGround + 1f, layerMask)){

                if (hit.transform.tag == "Ground" || hit.transform.tag == "Obstacle"){
                    Vector3 footPosition = hit.point;
                    footPosition.y += DistanceToGround;
                    animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                    animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal));
                }

            }

            //right foot 
            ray = new Ray(animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
            if(Physics.Raycast(ray, out hit, DistanceToGround + 1f, layerMask)){

                if (hit.transform.tag == "Ground" || hit.transform.tag == "Obstacle"){
                    Vector3 footPosition = hit.point;
                    footPosition.y += DistanceToGround;
                    animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                    animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hit.normal));
                }

            }


        }
    }

}
