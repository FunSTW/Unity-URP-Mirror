using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunS.Demo
{
    public class Jumping : MonoBehaviour
    {
        float temp;
        Vector3 origin;
        public bool random = true;
        public float height = 1.0f;
        public float speed = 1.0f;

        public AnimationCurve curve;

        private void Start()
        {
            origin = transform.position;
            height = random ? Random.value * height : height;
            speed = random ? Random.value * speed : speed;
        }

        void Update()
        {
            temp = (Time.time * speed) % 1;
            transform.position = origin + curve.Evaluate(temp) * height * Vector3.up;
        }
    }
}