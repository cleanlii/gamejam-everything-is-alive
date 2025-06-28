using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionAnimationScaleFlip : MonoBehaviour
{
  [SerializeField] Transform logo;
  public void AAA_FLip()
  {
    logo.rotation = new Quaternion(0, 180, 0, 0);
    transform.rotation = new Quaternion(0, 180, 0, 0);
  }
}
